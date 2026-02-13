# vLLM Sample Generation - Quick Start Guide

This guide shows how to generate coding and book writing samples using the vLLM endpoint.

## Prerequisites

1. **vLLM Server Running on Port 8001**
   ```bash
   # Follow AI/VLLM.md for setup instructions
   docker run --gpus all -p 8001:8000 vllm/vllm-openai:latest \
     --model meta-llama/Llama-3.1-70B-Instruct
   ```

2. **Python Dependencies**
   ```bash
   cd AI/src
   pip install -r requirements.txt
   ```

## Usage Examples

### Generate Coding Samples

```bash
# Generate 10 coding samples
python generate_vllm_samples.py --type coding --count 10

# Generate 50 coding samples with custom settings
python generate_vllm_samples.py --type coding --count 50 \
    --concurrency 16 \
    --batch-size 10 \
    --temperature 0.9
```

### Generate Book Writing Samples

```bash
# Generate 10 book writing samples
python generate_vllm_samples.py --type book --count 10

# Generate 50 book writing samples
python generate_vllm_samples.py --type book --count 50
```

### Generate Both Types

```bash
# Generate 20 samples of each type
python generate_vllm_samples.py --type all --count 20
```

## Sample Output

Samples are saved to `AI/src/vllm-samples/`:

- **coding.jsonl** - Programming task samples
- **book.jsonl** - Creative writing samples

### Coding Sample Format
```json
{
  "prompt": "Build a REST API in Python using Flask",
  "response": "Here's a complete REST API implementation with authentication..."
}
```

### Book Writing Sample Format
```json
{
  "prompt": "Write a sci-fi story about AI set in a futuristic megacity",
  "response": "In the year 2157, the neon-lit towers of Neo-Shanghai..."
}
```

## Topic Randomization

### Coding Topics (30+)
- Web development, mobile apps, backend APIs
- DevOps, ML, data processing, microservices
- Security, testing, CI/CD, cloud infrastructure
- Multiple languages: Python, JS, TypeScript, Go, Java, C#, Rust, etc.

### Book Writing Topics (60+ combinations)
- **Genres**: sci-fi, fantasy, mystery, thriller, romance, horror, etc.
- **Themes**: AI, time travel, survival, space exploration, etc.
- **Settings**: futuristic cities, medieval kingdoms, space stations, etc.

## Testing

Run the test suite to validate the script:

```bash
python test_vllm_samples.py
```

Use the mock server for local testing without vLLM:

```bash
# Terminal 1: Start mock server
python mock_vllm_server.py

# Terminal 2: Generate samples (they'll use mock data)
python generate_vllm_samples.py --type coding --count 2
```

## Command-Line Options

| Option | Default | Description |
|--------|---------|-------------|
| `--type` | (required) | Sample type: `coding`, `book`, or `all` |
| `--count` | (required) | Number of samples per type |
| `--endpoint` | `http://localhost:8001` | vLLM server URL |
| `--model` | `meta-llama/Llama-3.1-70B-Instruct` | Model name |
| `--concurrency` | `8` | Max concurrent requests |
| `--batch-size` | `5` | Samples per API call |
| `--temperature` | `0.8` | Sampling temperature |

## Features

✓ **Randomized Topics** - Automatic topic variation for diversity
✓ **Duplicate Detection** - Avoids regenerating existing prompts
✓ **Validation** - Ensures samples meet quality standards
✓ **Concurrent Generation** - Efficient batch processing
✓ **Append Mode** - Safely adds to existing datasets
✓ **Progress Tracking** - Real-time generation status

## Troubleshooting

**Connection Error**
```bash
# Verify vLLM is running
curl http://localhost:8001/v1/models
```

**No Samples Generated**
- Check vLLM server logs
- Try reducing `--concurrency` to 1
- Increase `--temperature` for more variety

**Invalid Samples**
- Use a larger model (70B+ recommended)
- Adjust `--temperature` (0.7-0.9 range)
- Check that model supports instruction following

## Next Steps

After generating samples:

1. **Review samples** - Check quality and diversity
2. **Fine-tune models** - Use samples for training (see AI/src/fine-tuning/)
3. **Generate more** - Run script again to append additional samples
4. **Export results** - Use JSONL files for downstream tasks
