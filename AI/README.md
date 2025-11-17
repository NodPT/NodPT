# NodPT TensorRT-LLM AI Service

This service provides high-performance LLM inference using NVIDIA TensorRT-LLM with FP4 quantization support.

## Features

- **TensorRT-LLM**: Optimized inference using NVIDIA TensorRT-LLM
- **FP4 Quantization**: Support for NVFP4 precision for efficient inference
- **Custom Models**: Easy integration of fine-tuned models
- **GPU Acceleration**: Full NVIDIA GPU support
- **REST API**: FastAPI-based REST API for inference requests
- **Security**: Uses patched versions of dependencies (torch>=2.6.0, transformers>=4.48.0) to address known vulnerabilities

## Prerequisites

- Docker and Docker Compose installed
- NVIDIA GPU with CUDA support (compute capability 7.0+)
- NVIDIA Container Toolkit installed
- At least 16GB GPU memory recommended

### Installing NVIDIA Container Toolkit

```bash
# Add NVIDIA package repositories
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
curl -s -L https://nvidia.github.io/nvidia-docker/gpgkey | sudo apt-key add -
curl -s -L https://nvidia.github.io/nvidia-docker/$distribution/nvidia-docker.list | \
  sudo tee /etc/apt/sources.list.d/nvidia-docker.list

# Install nvidia-container-toolkit
sudo apt-get update
sudo apt-get install -y nvidia-container-toolkit

# Restart Docker daemon
sudo systemctl restart docker
```

## Directory Structure

```
AI/
├── Dockerfile              # TensorRT-LLM Docker image definition
├── docker-compose.yml      # Docker Compose configuration
├── README.md              # This file
├── src/
│   └── main.py            # FastAPI application
├── models/                # Fine-tuned models directory (created on first run)
└── logs/                  # Application logs (created on first run)
```

## Quick Start

### 1. Build the Docker Image

```bash
cd AI
docker compose build
```

This will create a Docker image with:
- NVIDIA CUDA 12.1.0
- TensorRT-LLM 0.10.0
- Python 3.10
- FastAPI server

### 2. Start the Service

```bash
docker compose up -d
```

The service will be available at `http://localhost:8850`

### 3. Check Service Health

```bash
curl http://localhost:8850/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2025-11-17T01:23:10.000Z",
  "model_dir": "/models/finetuned",
  "base_model_dir": "/models/base"
}
```

## Using Custom Fine-Tuned Models

### Model Directory Structure

The service mounts the `./models` directory from your host to `/models/finetuned` inside the container. Place your fine-tuned models here:

```
AI/models/
├── my-custom-gpt-model/
│   ├── config.json
│   ├── pytorch_model.bin
│   ├── tokenizer_config.json
│   ├── tokenizer.json
│   └── vocab.txt
└── another-model/
    └── ...
```

### Adding a New Model

1. **Download or prepare your fine-tuned model:**
   ```bash
   cd AI/models
   mkdir my-model
   # Copy your model files to my-model/
   ```

2. **Restart the service to detect the new model:**
   ```bash
   docker compose restart
   ```

3. **Verify the model is available:**
   ```bash
   curl http://localhost:8850/models
   ```

### Recommended Models for NVFP4

The following models are recommended for use with NVFP4 quantization:

- **GPT-OSS-20B**: Open-source GPT model optimized for FP4
- **Llama 2/3**: Meta's Llama models with FP4 support
- **Mistral-7B**: Efficient model with good FP4 performance
- **GPT-NeoX-20B**: EleutherAI's 20B parameter model

### Model Conversion for TensorRT-LLM

To convert HuggingFace models to TensorRT-LLM format with FP4 quantization:

```bash
# Enter the container
docker exec -it nodpt-ai bash

# Convert model (example for GPT-OSS-20B)
python3 -c "
from tensorrt_llm import LLM
from tensorrt_llm.quantization import QuantMode

# Load and convert model
llm = LLM(
    model='/models/finetuned/gpt-oss-20b',
    quantization=QuantMode.from_description(use_fp4_for_weights=True),
    max_batch_size=8,
    max_input_len=2048,
    max_output_len=512
)
"
```

## API Endpoints

### 1. Root Endpoint
```bash
curl http://localhost:8850/
```

### 2. Health Check
```bash
curl http://localhost:8850/health
```

### 3. List Available Models
```bash
curl http://localhost:8850/models
```

### 4. Generate Text
```bash
curl -X POST http://localhost:8850/generate \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Once upon a time",
    "max_tokens": 100,
    "temperature": 0.7,
    "model_name": "my-custom-gpt-model"
  }'
```

### 5. Service Information
```bash
curl http://localhost:8850/info
```

## Configuration

### Environment Variables

You can customize the service by modifying the `docker-compose.yml` file:

- `MODEL_DIR`: Directory for fine-tuned models (default: `/models/finetuned`)
- `BASE_MODEL_DIR`: Directory for base models (default: `/models/base`)
- `NVIDIA_VISIBLE_DEVICES`: GPU selection (default: `all`)

### Volume Mounts

The following directories are mounted:

- `./models` → `/models/finetuned`: Fine-tuned models
- `./logs` → `/app/logs`: Application logs

## Monitoring and Logs

### View Real-time Logs
```bash
docker compose logs -f
```

### Access Log Files
Logs are stored in `./logs/api.log` on your host machine.

### Check Container Status
```bash
docker compose ps
```

## Troubleshooting

### GPU Not Detected

```bash
# Verify NVIDIA runtime
docker run --rm --gpus all nvidia/cuda:12.1.0-base-ubuntu22.04 nvidia-smi

# Check container GPU access
docker exec nodpt-ai nvidia-smi
```

### Out of Memory Errors

- Reduce batch size in model configuration
- Use smaller models or more aggressive quantization
- Increase GPU memory allocation

### Model Loading Issues

- Verify model files are complete
- Check model format compatibility
- Review logs: `docker compose logs`

## Performance Optimization

### FP4 Quantization Benefits

- **4x memory reduction**: FP4 uses 4 bits per weight vs 16 bits for FP16
- **2-3x inference speedup**: Faster computation with minimal accuracy loss
- **Larger batch sizes**: More efficient GPU utilization

### Best Practices

1. **Model Selection**: Choose models explicitly supporting FP4
2. **Batch Processing**: Use larger batch sizes for throughput
3. **KV Cache**: Enable key-value cache for multi-turn conversations
4. **GPU Selection**: Use GPUs with Tensor Cores for optimal performance

## Stopping the Service

```bash
docker compose down
```

To also remove volumes:
```bash
docker compose down -v
```

## Additional Resources

- [TensorRT-LLM Documentation](https://github.com/NVIDIA/TensorRT-LLM)
- [NVIDIA GPU Cloud](https://ngc.nvidia.com/)
- [Model Optimization Guide](https://docs.nvidia.com/deeplearning/tensorrt/developer-guide/)
- [FP4 Quantization Paper](https://arxiv.org/abs/2309.14717)

## Support

For issues and questions:
- Check the logs: `docker compose logs`
- Review NVIDIA TensorRT-LLM documentation
- Open an issue in the repository

## License

This service is part of the NodPT project. See the main repository LICENSE file for details.
