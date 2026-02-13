# vLLM Setup and Sample Generation

This guide covers:
1. Installing the vLLM OpenAI-compatible server with Docker (port 8001)
2. Generating coding and book writing samples with randomized topics

## Prerequisites

- Docker
- NVIDIA GPU with CUDA support
- Hugging Face access token (if the model requires gated access)

## Run vLLM with Docker

```bash
docker pull vllm/vllm-openai:latest

docker run --gpus all --ipc=host --shm-size 16g \
  -p 8001:8000 \
  -v vllm_cache:/root/.cache/huggingface \
  -e HF_HOME=/root/.cache/huggingface \
  -e HUGGING_FACE_HUB_TOKEN=YOUR_TOKEN \
  vllm/vllm-openai:latest \
  --model meta-llama/Llama-3.1-70B-Instruct \
  --quantization fp4 \
  --gpu-memory-utilization 0.65 \
  --port 8000
```

## Verify the Service

```bash
curl http://localhost:8001/v1/models
```

## Notes

- Host port `8001` maps to the container's port `8000`.
- `--gpu-memory-utilization 0.65` limits GPU memory usage to 65%.
- `--quantization fp4` enables 4-bit floating point weights for the Llama 70B model, trading a small amount of quality for lower memory usage.
- Keep `HUGGING_FACE_HUB_TOKEN` in your shell environment or secret manager and never commit real tokens to version control.

## Generate Training Samples

Once vLLM is running on port 8001, use the sample generation script to create training data.

### Install Dependencies

```bash
cd AI/src
pip install -r requirements.txt
```

### Generate Samples

The script `generate_vllm_samples.py` generates two types of samples with randomized topics:

**Coding Samples** - Programming tasks covering:
- Multiple languages (Python, JavaScript, TypeScript, Go, Java, C#, Rust, etc.)
- Various domains (web, mobile, backend, DevOps, ML, security, etc.)
- Realistic code implementations with best practices

**Book Writing Samples** - Creative writing with:
- Multiple genres (sci-fi, fantasy, mystery, thriller, romance, etc.)
- Randomized themes (AI, time travel, survival, etc.)
- Varied settings (futuristic cities, medieval kingdoms, etc.)

### Usage Examples

```bash
cd AI/src

# Generate 10 coding samples
python generate_vllm_samples.py --type coding --count 10

# Generate 20 book writing samples
python generate_vllm_samples.py --type book --count 20

# Generate both types (50 samples each)
python generate_vllm_samples.py --type all --count 50

# Custom configuration
python generate_vllm_samples.py --type all --count 30 \
    --endpoint http://localhost:8001 \
    --model meta-llama/Llama-3.1-70B-Instruct \
    --concurrency 16 \
    --batch-size 10 \
    --temperature 0.9
```

### Output

Samples are saved in JSONL format to `AI/src/vllm-samples/`:
- `coding.jsonl` - Coding task samples
- `book.jsonl` - Book writing samples

Each line contains a JSON object with:
```json
{"prompt": "Task or creative prompt", "response": "Detailed implementation or story"}
```

### Options

- `--type`: Sample type (`coding`, `book`, or `all`)
- `--count`: Number of samples per type
- `--endpoint`: vLLM server URL (default: `http://localhost:8001`)
- `--model`: Model name (default: `meta-llama/Llama-3.1-70B-Instruct`)
- `--concurrency`: Concurrent requests (default: 8)
- `--batch-size`: Samples per API call (default: 5)
- `--temperature`: Sampling temperature (default: 0.8)

The script automatically:
- Randomizes topics for diversity
- Avoids duplicate prompts
- Validates generated samples
- Appends to existing files without overwriting
