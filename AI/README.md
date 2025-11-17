# NodPT AI Service - TensorRT-LLM

This directory contains the AI service for NodPT, powered by NVIDIA TensorRT-LLM for high-performance LLM inference with OpenAI-compatible API.

## Overview

The AI service provides:
- **TensorRT-LLM**: NVIDIA's optimized inference engine for large language models
- **OpenAI-Compatible API**: REST API endpoints compatible with OpenAI's format
- **Custom Model Support**: Ability to use your own fine-tuned models
- **GPU Acceleration**: Leverages NVIDIA GPUs for fast inference

## Prerequisites

### Hardware Requirements
- NVIDIA GPU with compute capability 8.0 or higher (RTX 3000 series, A100, H100, etc.)
- At least 16GB GPU memory (32GB+ recommended for larger models)
- 50GB+ disk space for models and engines

### Software Requirements
- Docker with NVIDIA Container Runtime
- NVIDIA GPU drivers (version 525.60.13 or later)
- Docker Compose

### Install NVIDIA Container Runtime

```bash
# Add NVIDIA package repositories
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
curl -s -L https://nvidia.github.io/nvidia-docker/gpgkey | sudo apt-key add -
curl -s -L https://nvidia.github.io/nvidia-docker/$distribution/nvidia-docker.list | sudo tee /etc/apt/sources.list.d/nvidia-docker.list

# Install nvidia-docker2
sudo apt-get update
sudo apt-get install -y nvidia-docker2

# Restart Docker daemon
sudo systemctl restart docker

# Test GPU access
docker run --rm --gpus all nvidia/cuda:12.1.0-base-ubuntu22.04 nvidia-smi
```

## Quick Start

### 1. Build the Docker Image

```bash
cd AI
docker compose build
```

**Note**: Initial build takes 30-60 minutes as it compiles TensorRT-LLM from source.

### 2. Start the Service

```bash
docker compose up -d
```

The service will start and listen on port 8000.

### 3. Check Service Health

```bash
curl http://localhost:8000/health
```

Expected response:
```json
{
  "status": "healthy",
  "model_loaded": false
}
```

## Using Custom Models

### Directory Structure

The service uses two directories for model management:

- **`./models`**: Place your model files here (HuggingFace format, GGUF, etc.)
- **`./engines`**: Compiled TensorRT engines are cached here for faster startup

```
AI/
├── models/              # Your custom models go here
│   ├── gpt-oss-20b/    # Default model directory
│   ├── my-model-1/     # Your custom model
│   └── my-model-2/     # Another custom model
├── engines/            # Auto-generated TensorRT engines (cached)
│   ├── gpt-oss-20b/
│   ├── my-model-1/
│   └── my-model-2/
```

### Adding a Model

#### Option 1: Download Pre-trained Model

```bash
# Create models directory
mkdir -p models/gpt-oss-20b

# Download GPT-OSS-20B (example using git-lfs)
cd models
git lfs install
git clone https://huggingface.co/nvidia/gpt-oss-20b
```

#### Option 2: Use Your Fine-tuned Model

```bash
# Copy your model files to the models directory
cp -r /path/to/your/fine-tuned-model ./models/my-custom-model

# Ensure it contains required files:
# - config.json
# - pytorch_model.bin (or model.safetensors)
# - tokenizer.json
# - tokenizer_config.json
```

### Model Format Requirements

Your model directory should contain:
- `config.json` - Model configuration
- Model weights in one of these formats:
  - PyTorch: `pytorch_model.bin` or `model.safetensors`
  - Multiple shards: `pytorch_model-00001-of-00002.bin`, etc.
- Tokenizer files:
  - `tokenizer.json`
  - `tokenizer_config.json`
  - `vocab.json` (if applicable)

### Using a Specific Model

Set the `DEFAULT_MODEL` environment variable to use a specific model:

```bash
# In docker-compose.yml
environment:
  - DEFAULT_MODEL=my-custom-model
```

Or pass it when starting:

```bash
docker compose run -e DEFAULT_MODEL=my-custom-model nodpt-ai
```

## API Usage

The service provides OpenAI-compatible endpoints:

### List Available Models

```bash
curl http://localhost:8000/v1/models
```

### Text Completion

```bash
curl http://localhost:8000/v1/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-oss-20b",
    "prompt": "Once upon a time",
    "max_tokens": 100,
    "temperature": 0.7
  }'
```

### Chat Completion

```bash
curl http://localhost:8000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-oss-20b",
    "messages": [
      {"role": "system", "content": "You are a helpful assistant."},
      {"role": "user", "content": "What is TensorRT-LLM?"}
    ],
    "max_tokens": 200,
    "temperature": 0.7
  }'
```

### Using with OpenAI Python Client

```python
from openai import OpenAI

# Point to local TensorRT-LLM service
client = OpenAI(
    base_url="http://localhost:8000/v1",
    api_key="not-needed"  # API key not required for local service
)

# Create chat completion
response = client.chat.completions.create(
    model="gpt-oss-20b",
    messages=[
        {"role": "user", "content": "Hello, how are you?"}
    ]
)

print(response.choices[0].message.content)
```

## Advanced Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `MODEL_DIR` | `/models` | Directory containing model files |
| `ENGINE_DIR` | `/engines` | Directory for TensorRT engine cache |
| `DEFAULT_MODEL` | `gpt-oss-20b` | Default model to load |
| `NVIDIA_VISIBLE_DEVICES` | `all` | GPUs to use (e.g., `0,1` for GPU 0 and 1) |

### Building TensorRT Engines

TensorRT engines are automatically built on first use. To pre-build an engine:

```bash
# Access container shell
docker compose exec nodpt-ai bash

# Navigate to TensorRT-LLM examples
cd /workspace/TensorRT-LLM/examples/gpt

# Build engine for your model
python convert_checkpoint.py \
    --model_dir /models/my-custom-model \
    --output_dir /engines/my-custom-model/checkpoint \
    --dtype float16

trtllm-build \
    --checkpoint_dir /engines/my-custom-model/checkpoint \
    --output_dir /engines/my-custom-model \
    --gemm_plugin float16 \
    --max_batch_size 8 \
    --max_input_len 2048 \
    --max_output_len 512
```

### Performance Tuning

For optimal performance:

1. **Use FP16 precision**: Balances speed and accuracy
2. **Adjust batch size**: Set based on GPU memory
3. **Pre-compile engines**: Build engines before deployment
4. **Monitor GPU usage**: Use `nvidia-smi` to track utilization

```bash
# Monitor GPU in real-time
watch -n 1 nvidia-smi
```

## Troubleshooting

### Container Fails to Start

**Issue**: GPU not accessible
```bash
# Verify NVIDIA runtime
docker run --rm --gpus all nvidia/cuda:12.1.0-base-ubuntu22.04 nvidia-smi

# Check Docker daemon config
cat /etc/docker/daemon.json
# Should contain: {"default-runtime": "nvidia"}
```

**Issue**: Out of memory
- Reduce batch size in engine build
- Use smaller model or quantization
- Ensure no other GPU processes running

### Model Not Loading

**Issue**: Model files missing
```bash
# Check model directory structure
docker compose exec nodpt-ai ls -la /models/gpt-oss-20b/

# Should see: config.json, pytorch_model.bin, tokenizer files
```

**Issue**: Incompatible model format
- Ensure model is in HuggingFace transformers format
- Check TensorRT-LLM compatibility for your model architecture

### API Errors

**Issue**: Connection refused
```bash
# Check if container is running
docker compose ps

# Check logs
docker compose logs nodpt-ai

# Check port binding
netstat -tulpn | grep 8000
```

## Logs and Monitoring

### View Logs

```bash
# Follow logs
docker compose logs -f nodpt-ai

# View last 100 lines
docker compose logs --tail=100 nodpt-ai
```

### Access Container Shell

```bash
# Interactive shell
docker compose exec nodpt-ai bash

# Run commands
docker compose exec nodpt-ai python --version
docker compose exec nodpt-ai nvidia-smi
```

## Stopping the Service

```bash
# Stop service
docker compose down

# Stop and remove volumes (WARNING: deletes cached engines)
docker compose down -v
```

## Recommended Models

### GPT-OSS-20B (Default)
- **Size**: 20B parameters
- **Memory**: ~40GB GPU RAM
- **Use Case**: General purpose, good balance

### Smaller Alternatives
- **GPT-J-6B**: 6B parameters, ~12GB GPU RAM
- **GPT-2-XL**: 1.5B parameters, ~6GB GPU RAM

### Fine-tuning Your Own

To use a fine-tuned model:
1. Train using HuggingFace transformers
2. Save model in HuggingFace format
3. Copy to `./models/your-model-name/`
4. Set `DEFAULT_MODEL=your-model-name`
5. Restart service

## Support and Resources

- **TensorRT-LLM GitHub**: https://github.com/NVIDIA/TensorRT-LLM
- **NVIDIA DGX Spark Playbooks**: https://github.com/NVIDIA/dgx-spark-playbooks
- **TensorRT-LLM Documentation**: https://nvidia.github.io/TensorRT-LLM/
- **OpenAI API Reference**: https://platform.openai.com/docs/api-reference

## License

This service uses NVIDIA TensorRT-LLM, which is licensed under the Apache 2.0 License.
