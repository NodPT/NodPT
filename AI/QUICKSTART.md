# TensorRT-LLM Quick Start Guide

## Prerequisites
- NVIDIA GPU (Compute Capability 7.0+)
- Docker & Docker Compose
- NVIDIA Container Toolkit

## Installation Steps

### 1. Start the Service
```bash
cd AI
docker compose up -d
```

### 2. Verify Service is Running
```bash
curl http://localhost:8850/health
```

### 3. Add Your Models
```bash
# Create a model directory
mkdir -p models/my-model

# Copy your model files
cp -r /path/to/your/model/* models/my-model/

# Restart service to load new model
docker compose restart
```

### 4. Test the API
```bash
# List available models
curl http://localhost:8850/models

# Generate text
curl -X POST http://localhost:8850/generate \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Hello, how are you?",
    "max_tokens": 50
  }'
```

## Recommended Models for NVFP4

- **GPT-OSS-20B**: Optimized for FP4 quantization
- **Llama 2/3 7B-70B**: Meta's open models
- **Mistral-7B**: Efficient instruction-following model

## Troubleshooting

### GPU Not Detected
```bash
# Test NVIDIA runtime
docker run --rm --gpus all nvidia/cuda:12.1.0-base-ubuntu22.04 nvidia-smi
```

### View Logs
```bash
docker compose logs -f
```

### Stop Service
```bash
docker compose down
```

## Next Steps

See [README.md](README.md) for complete documentation including:
- Detailed API documentation
- Model conversion instructions
- Performance optimization tips
- Advanced configuration options
