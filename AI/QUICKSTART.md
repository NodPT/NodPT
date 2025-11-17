# Quick Start Guide - NodPT AI Service

## 1. Prerequisites Check

```bash
# Check NVIDIA GPU
nvidia-smi

# Check Docker with GPU support
docker run --rm --gpus all nvidia/cuda:12.1.0-base-ubuntu22.04 nvidia-smi
```

## 2. Build & Start

```bash
# Navigate to AI directory
cd AI

# Build Docker image (first time: 30-60 minutes)
docker compose build

# Start service
docker compose up -d

# Check logs
docker compose logs -f
```

## 3. Verify Service

```bash
# Health check
curl http://localhost:8000/health

# List models
curl http://localhost:8000/v1/models
```

## 4. Test API

```bash
# Simple completion test
curl http://localhost:8000/v1/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-oss-20b",
    "prompt": "Hello, how are you?",
    "max_tokens": 50
  }'

# Or use example scripts
./examples/api_examples.sh
python examples/usage_example.py
```

## 5. Add Your Model

```bash
# Create model directory
mkdir -p models/my-model

# Copy your model files
cp -r /path/to/your/model/* models/my-model/

# Restart with your model
docker compose down
docker compose run -e DEFAULT_MODEL=my-model nodpt-ai
```

## Common Commands

```bash
# View logs
docker compose logs -f nodpt-ai

# Stop service
docker compose down

# Rebuild image
docker compose build --no-cache

# Access container shell
docker compose exec nodpt-ai bash

# Check GPU usage
nvidia-smi -l 1
```

## Troubleshooting

**Issue: GPU not found**
```bash
# Check NVIDIA runtime
cat /etc/docker/daemon.json
# Should contain: {"default-runtime": "nvidia"}

# Restart Docker
sudo systemctl restart docker
```

**Issue: Out of memory**
- Use a smaller model
- Close other GPU applications
- Check: `nvidia-smi` for GPU memory usage

**Issue: Container won't start**
```bash
# Check logs
docker compose logs nodpt-ai

# Try interactive mode
docker compose run nodpt-ai bash
```

## Next Steps

- Read full documentation: [README.md](README.md)
- Try examples: [examples/](examples/)
- Configure custom models
- Integrate with your application

## Support

- TensorRT-LLM Docs: https://github.com/NVIDIA/TensorRT-LLM
- NVIDIA Forums: https://forums.developer.nvidia.com/
- OpenAI API Reference: https://platform.openai.com/docs/api-reference
