# NodPT Fine-Tuning

Fine-tune a language model on NodPT node-type tasks using [Unsloth](https://github.com/unslothai/unsloth) with **FP4 (4-bit)** precision, then export and run locally with [Ollama](https://ollama.ai/).

## Overview

NodPT uses a four-level node hierarchy to decompose project requests:

| Node Type  | Input          | Output                              |
|------------|----------------|-------------------------------------|
| Director   | Project request | `content` + `managers[]` (name/job) |
| Manager    | Job from Director | `content` + `supervisors[]` (name/job) |
| Supervisor | Job from Manager | `content` + `agents[]` (name/job) |
| Agent      | Job from Supervisor | `content` + `files[]` (filename/content) |

Each node type has a JSON Schema in `AI/src/<NodeType>/format.json` that defines its structured output format. The fine-tuning data teaches the model to produce valid JSON that matches these schemas.

## Folder Structure

```
AI/src/fine-tuning/
├── README.md              # This file
├── Modelfile              # Ollama Modelfile template
├── requirements.txt       # Python dependencies
├── data-samples/          # Training data (JSONL)
│   ├── director.jsonl
│   ├── manager.jsonl
│   ├── supervisor.jsonl
│   └── agent.jsonl
├── scripts/
│   ├── generate_samples.py  # Generate samples via TensorRT-LLM (70B+ model)
│   ├── finetune.py          # Fine-tuning script (Unsloth + FP4)
│   └── export_gguf.py       # Export to GGUF for Ollama
└── export/                  # Exported GGUF model files (gitignored)
```

## Prerequisites

- **Python** 3.10+
- **CUDA** 11.8+ with a compatible NVIDIA GPU (≥ 16 GB VRAM recommended)
- **Ollama** installed locally (for deployment)
- **Git** and **pip**

## 1. Environment Setup

```bash
cd AI/src/fine-tuning

# Create a virtual environment (recommended)
python -m venv .venv
source .venv/bin/activate   # Linux/macOS
# .venv\Scripts\activate    # Windows

# Install dependencies
pip install -r requirements.txt
```

> **Note:** Unsloth requires an NVIDIA GPU with CUDA support. See [Unsloth installation](https://github.com/unslothai/unsloth#installation) and [NVIDIA's Unsloth guide](https://build.nvidia.com/spark/unsloth/instructions) for environment-specific instructions.

## 2. Generate Training Data (TensorRT-LLM)

Use a large model (70B+) served by TensorRT-LLM to generate high-quality training samples. TensorRT-LLM exposes an OpenAI-compatible API and supports **continuous batching**, so concurrent requests are efficiently handled server-side.

### Recommended Models

| Model | Parameters | Notes |
|-------|-----------|-------|
| `meta-llama/Llama-3.1-70B-Instruct` | 70B | Recommended default, strong instruction following |
| `meta-llama/Llama-3.3-70B-Instruct` | 70B | Latest Llama, improved quality |
| `Qwen/Qwen2.5-72B-Instruct` | 72B | Alternative, good at structured JSON output |

### Quick Start

```bash
# Generate 50 director samples
python scripts/generate_samples.py --node-type director --count 50

# Generate 100 samples for ALL node types
python scripts/generate_samples.py --node-type all --count 100

# Custom endpoint and higher concurrency (128GB server)
python scripts/generate_samples.py --node-type all --count 200 \
    --endpoint http://my-server:8000 \
    --model meta-llama/Llama-3.1-70B-Instruct \
    --concurrency 16 --batch-size 10
```

### Options

| Option          | Default                                    | Description |
|-----------------|--------------------------------------------|-------------|
| `--node-type`   | (required)                                 | `director`, `manager`, `supervisor`, `agent`, or `all` |
| `--count`       | (required)                                 | Number of samples to generate per node type |
| `--endpoint`    | `http://localhost:8000`                    | TensorRT-LLM server URL |
| `--model`       | `meta-llama/Llama-3.1-70B-Instruct`       | Model name served by TensorRT-LLM |
| `--concurrency` | `8`                                        | Max concurrent API requests |
| `--batch-size`  | `5`                                        | Samples to request per API call |
| `--temperature` | `0.8`                                      | Sampling temperature |
| `--overwrite`   | `false`                                    | Clear existing samples before generating |

### How It Works

1. Builds structured prompts that instruct the large model to produce JSONL training data.
2. Fires concurrent requests to TensorRT-LLM (utilising continuous batching).
3. Parses and **validates** each generated sample against the node type's `format.json` schema.
4. Discards invalid samples and retries until the requested count is reached.
5. Appends valid samples to `data-samples/<node-type>.jsonl` (deduplicating by input).

### TensorRT-LLM Setup

Start TensorRT-LLM with an OpenAI-compatible API server:

```bash
# Example using the TensorRT-LLM OpenAI server
python -m tensorrt_llm.serve \
    --model meta-llama/Llama-3.1-70B-Instruct \
    --host 0.0.0.0 --port 8000
```

> **Concurrency note:** TensorRT-LLM uses continuous batching, so it natively handles concurrent requests. With 128 GB system RAM, you can comfortably run 8–16 concurrent requests against a 70B model. Increase `--concurrency` based on your GPU VRAM and system memory.

## 3. Data Format

### Data Format

Training data uses **JSONL** (one JSON object per line) with three fields:

| Field         | Type   | Description |
|---------------|--------|-------------|
| `instruction` | string | System instruction describing the node type role. |
| `input`       | string | The user's project request or job description. |
| `output`      | string | Expected JSON response matching the node type's `format.json` schema. |

### Example (Director)

```json
{
  "instruction": "You are a Director AI. Analyze the following project request and break it down into manager assignments. For each manager, provide a clear name and job description.",
  "input": "Build a simple todo application with user authentication, a REST API backend, and a web frontend.",
  "output": "{\"content\": \"I'll organize this project into three areas...\", \"managers\": [{\"name\": \"Backend API Manager\", \"job\": \"Design and implement the REST API...\"}]}"
}
```

### Output Schema by Node Type

**Director** — must produce `{ "content": string, "managers": [{ "name": string, "job": string }] }`

**Manager** — must produce `{ "content": string, "supervisors": [{ "name": string, "job": string }] }`

**Supervisor** — must produce `{ "content": string, "agents": [{ "name": string, "job": string }] }`

**Agent** — must produce `{ "content": string, "files": [{ "filename": string, "content": string }] }`

### Adding More Data

Add lines to the corresponding `.jsonl` file in `data-samples/`. Ensure each line is valid JSON and the `output` field is valid JSON matching the node type schema.

## 4. Fine-Tuning

Run the fine-tuning script from the `fine-tuning` directory:

```bash
# Fine-tune on ALL node types (recommended)
python scripts/finetune.py --node-type all

# Fine-tune on a single node type
python scripts/finetune.py --node-type director

# Customise base model and epochs
python scripts/finetune.py --node-type all \
  --base-model unsloth/Llama-3.1-8B-Instruct-bnb-4bit \
  --epochs 5
```

### Options

| Option          | Default                                        | Description |
|-----------------|-------------------------------------------------|-------------|
| `--node-type`   | `all`                                           | `director`, `manager`, `supervisor`, `agent`, or `all` |
| `--base-model`  | `unsloth/Llama-3.1-8B-Instruct-bnb-4bit`       | Unsloth-compatible model name |
| `--epochs`      | `3`                                             | Number of training epochs |

### What Happens

1. Loads the base model in FP4 (4-bit) quantisation via Unsloth.
2. Applies LoRA adapters (rank 16) to attention and MLP layers.
3. Loads JSONL data from `data-samples/` and converts to Alpaca-style prompts.
4. Trains with `SFTTrainer` (AdamW 8-bit, bf16, gradient checkpointing).
5. Saves checkpoints after each epoch and final weights to `output/<node-type>/final/`.

## 5. Model Export (GGUF)

Export the fine-tuned model to GGUF format for Ollama:

```bash
python scripts/export_gguf.py --model-dir output/all/final

# Choose a quantisation method (default: q4_k_m)
python scripts/export_gguf.py --model-dir output/all/final --quantization q4_k_m
```

### Quantisation Options

| Method   | Size   | Quality | Description |
|----------|--------|---------|-------------|
| `q4_k_m` | Small  | Good    | 4-bit quantisation, recommended default |
| `q5_k_m` | Medium | Better  | 5-bit quantisation |
| `q8_0`   | Large  | High    | 8-bit quantisation |
| `f16`    | Largest | Highest | 16-bit float, no quantisation |

Exported files are written to `export/`.

## 6. Run with Ollama

### Step 1 — Verify the GGUF file

After export, check that a `.gguf` file exists in `export/`:

```bash
ls -lh export/*.gguf
```

### Step 2 — Update the Modelfile

The included `Modelfile` has a `FROM` line pointing to the default export path. Update it if your filename differs:

```dockerfile
FROM ./export/unsloth.Q4_K_M.gguf
```

### Step 3 — Create the Ollama model

```bash
ollama create nodpt -f Modelfile
```

### Step 4 — Run the model

```bash
ollama run nodpt
```

### Step 5 — Use with NodPT

Once the model is loaded in Ollama, use it with the existing `run.py` CLI:

```bash
cd AI/src
python run.py director --prompt sample.txt --model nodpt
python run.py agent --prompt-text "Write a REST endpoint for user signup" --model nodpt
```

## Expected Outputs and Limitations

### Expected Outputs

- The fine-tuned model produces JSON matching the node type schema when prompted correctly.
- With the provided Modelfile, Ollama will format responses using the Alpaca template.
- Using `run.py` with the `--model nodpt` flag sends requests to the locally hosted model.

### Limitations

- **Small dataset**: The provided samples are starter examples. For production quality, add 50–200+ examples per node type.
- **GPU required**: Fine-tuning requires an NVIDIA GPU with ≥ 16 GB VRAM. Inference via Ollama can run on CPU but is slower.
- **Base model choice**: Results depend heavily on the base model. The default `Llama-3.1-8B-Instruct-bnb-4bit` provides a good balance of size and quality.
- **FP4 precision**: 4-bit quantisation reduces memory usage at a small cost to precision. For higher accuracy, use a larger model or higher quantisation during export.
- **Prompt format sensitivity**: The model is trained on Alpaca-style prompts. Ensure the Modelfile template and prompts match the training format.

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `CUDA out of memory` | Reduce `per_device_train_batch_size` or `max_seq_length` in `finetune.py` |
| `No module named 'unsloth'` | Ensure you installed dependencies: `pip install -r requirements.txt` |
| GGUF file too large | Use a smaller quantisation (e.g. `q4_k_m` instead of `f16`) |
| Ollama model produces bad JSON | Add more training data and re-train with more epochs |
| `ollama create` fails | Ensure the `FROM` path in `Modelfile` points to an existing `.gguf` file |

## References

- [Unsloth GitHub](https://github.com/unslothai/unsloth)
- [NVIDIA Unsloth Guide](https://build.nvidia.com/spark/unsloth/instructions)
- [Ollama Documentation](https://ollama.ai/)
- [GGUF Format](https://github.com/ggerganov/ggml/blob/master/docs/gguf.md)
- [NodPT AI Testing Instructions](../instruction.md)
