# NodPT AI Service

AI inference service using Ollama for large language model processing. This service provides AI capabilities to the NodPT platform through the Executor service.

## 🛠️ Technology Stack

- **Ollama**: Open-source LLM inference engine
- **Docker**: Containerized deployment
- **NVIDIA GPU**: Required for optimal performance
- **CUDA**: GPU acceleration support

### Supported Models

- **LLaMA 2**: General-purpose language model
- **Code LLaMA**: Specialized for code generation and analysis
- **Mistral**: Fast and efficient model
- **Custom Models**: Any Ollama-compatible model

## 🏗️ Architecture

### Service Flow

```
Executor Service
    │
    │ HTTP POST
    ▼
Ollama API (Port 11434)
    │
    ├─→ Model: trt-llm-manager
    ├─→ Model: trt-llm-inspector
    └─→ Model: trt-llm-agent
    │
    ▼
GPU Processing (CUDA)
    │
    ▼
AI Response
```

### Project Structure

```
AI/
├── docker-compose.yml    # Docker Compose configuration
└── README.md            # This file
```

## 🚀 Getting Started

### Prerequisites

- **NVIDIA GPU**: CUDA-compatible GPU (RTX 2060 or better recommended)
- **NVIDIA Docker Runtime**: GPU support in Docker
- **Docker & Docker Compose**: Container runtime
- **At least 8GB VRAM**: For running medium-sized models
- **At least 16GB RAM**: System memory

### Verify GPU Support

```bash
# Check NVIDIA driver
nvidia-smi

# Verify Docker GPU support
docker run --rm --gpus all nvidia/cuda:12.0-base nvidia-smi
```

### Install NVIDIA Container Toolkit

If not already installed:

```bash
# Ubuntu/Debian
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
curl -s -L https://nvidia.github.io/nvidia-docker/gpgkey | sudo apt-key add -
curl -s -L https://nvidia.github.io/nvidia-docker/$distribution/nvidia-docker.list | \
  sudo tee /etc/apt/sources.list.d/nvidia-docker.list

sudo apt-get update
sudo apt-get install -y nvidia-container-toolkit
sudo systemctl restart docker
```

## 🐳 Docker Deployment

### Build and Run

```bash
# Navigate to AI directory
cd AI

# Start Ollama service
docker-compose up -d

# View logs
docker-compose logs -f

# Stop service
docker-compose down
```

The service will be accessible at `http://localhost:11434`

### Docker Compose Configuration

```yaml
services:
  ollama:
    image: ollama/ollama:latest
    container_name: ollama
    restart: unless-stopped
    
    # Enable NVIDIA GPU
    deploy:
      resources:
        reservations:
          devices:
            - capabilities: [ gpu ]
    
    # OR for older Docker versions:
    # runtime: nvidia

    # enable all GPUs
    gpus: all
    
    # Environment variables for remote access
    environment:
      - OLLAMA_HOST=0.0.0.0:11434    # Listen on all interfaces for Docker network access
      - OLLAMA_ORIGINS=*              # Allow CORS from any origin
    
    volumes:
      - ollama_data:/root/.ollama
    
    ports:
      - "11434:11434"

volumes:
  ollama_data:
```

### Remote Access Configuration

The Ollama service is configured to accept connections from other Docker containers:

- **OLLAMA_HOST=0.0.0.0:11434**: Binds Ollama to all network interfaces, allowing access from other containers on the same Docker network
- **OLLAMA_ORIGINS=\***: Disables CORS restrictions for web-based access

This configuration enables the Executor service to communicate with Ollama using the hostname `ollama:11434` within the `backend_network`.

### Persistent Storage

Model data is stored in Docker volume `ollama_data` to persist across container restarts.

## 📦 Model Management

### Pull Models

```bash
# Access Ollama container
docker exec -it ollama bash

# Pull LLaMA 2 (7B)
ollama pull llama2

# Pull Code LLaMA
ollama pull codellama

# Pull Mistral
ollama pull mistral

# List downloaded models
ollama list
```

### Recommended Models for NodPT

| Role | Model | Size | Purpose |
|------|-------|------|---------|
| Manager | `llama2:13b` | 7.3GB | High-level planning and orchestration |
| Inspector | `codellama:13b` | 7.3GB | Code review and analysis |
| Worker | `mistral:7b` | 4.1GB | Fast task execution |

### Custom Model Configuration

For production, you can use custom fine-tuned models:

```bash
# Create Modelfile
FROM llama2
SYSTEM "You are a workflow planning assistant."

# Create custom model
ollama create trt-llm-manager -f Modelfile
```

## 🔌 API Usage

### Ollama API Endpoints

#### List Models

```bash
curl http://localhost:11434/api/tags
```

#### Generate Completion

```bash
curl -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama2",
    "prompt": "Explain AI workflow orchestration",
    "stream": false
  }'
```

#### Chat Completion (OpenAI-compatible)

```bash
curl -X POST http://localhost:11434/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama2",
    "messages": [
      {
        "role": "user",
        "content": "Explain AI workflow orchestration"
      }
    ],
    "max_tokens": 128
  }'
```

### Integration with Executor

The Executor service uses the OpenAI-compatible endpoint:

```csharp
// Executor LlmChatService configuration
LLM_ENDPOINT=http://ollama:11434/v1/chat/completions

// Request format
{
  "model": "llama2",
  "messages": [{"role": "user", "content": "message"}],
  "max_tokens": 128
}
```

## ⚙️ Configuration

### Environment Variables

```env
# Ollama Configuration
OLLAMA_HOST=0.0.0.0:11434
OLLAMA_MODELS=/root/.ollama/models
OLLAMA_NUM_PARALLEL=4
OLLAMA_MAX_LOADED_MODELS=3

# GPU Configuration
CUDA_VISIBLE_DEVICES=0
```

### Performance Tuning

#### GPU Memory Management

```bash
# Set maximum GPU memory usage (80% of available)
docker run --gpus all \
  -e CUDA_VISIBLE_DEVICES=0 \
  -e NVIDIA_VISIBLE_DEVICES=0 \
  ollama/ollama:latest
```

#### Concurrent Requests

Adjust based on GPU memory:

```env
# Number of parallel requests
OLLAMA_NUM_PARALLEL=4

# Max models loaded in memory
OLLAMA_MAX_LOADED_MODELS=3
```

## 📊 Monitoring

### Health Check

```bash
# Check service status
curl http://localhost:11434/api/tags

# Check GPU utilization
nvidia-smi
```

### Performance Metrics

Monitor these metrics:

- **GPU Utilization**: Should be 70-95% during inference
- **GPU Memory**: Watch for OOM errors
- **Response Time**: Typical 1-5 seconds per request
- **Throughput**: Requests per second

### Logs

```bash
# View Ollama logs
docker-compose logs -f ollama

# Monitor in real-time
docker logs -f ollama
```

## 🔧 Troubleshooting

### Common Issues

#### GPU Not Detected

```bash
# Verify GPU in container
docker exec ollama nvidia-smi

# Check Docker GPU support
docker run --rm --gpus all ubuntu nvidia-smi
```

**Solution**: Install NVIDIA Container Toolkit and restart Docker

#### Out of Memory Errors

**Symptoms**: Container crashes, CUDA OOM errors

**Solutions**:
- Use smaller models (7B instead of 13B)
- Reduce `OLLAMA_NUM_PARALLEL`
- Reduce `OLLAMA_MAX_LOADED_MODELS`
- Increase GPU memory or use model quantization

#### Slow Inference

**Solutions**:
- Ensure GPU is being used (check `nvidia-smi`)
- Use quantized models (Q4, Q5)
- Reduce max_tokens in requests
- Upgrade GPU if necessary

#### Model Download Fails

```bash
# Retry with verbose logging
docker exec -it ollama ollama pull llama2 --verbose

# Check disk space
df -h
```

#### Connection Refused

```bash
# Verify service is running
docker ps | grep ollama

# Check port binding
netstat -tlnp | grep 11434

# Test from inside network
docker exec ollama curl http://localhost:11434/api/tags
```

## 🚀 Production Deployment

### Multi-GPU Setup

```yaml
services:
  ollama:
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: all
              capabilities: [gpu]
```

### Load Balancing

For high-traffic deployments:

```yaml
services:
  ollama-1:
    image: ollama/ollama:latest
    # GPU 0
    environment:
      - CUDA_VISIBLE_DEVICES=0
  
  ollama-2:
    image: ollama/ollama:latest
    # GPU 1
    environment:
      - CUDA_VISIBLE_DEVICES=1
```

### Model Caching

Pre-pull models in Dockerfile:

```dockerfile
FROM ollama/ollama:latest

RUN ollama serve & \
    sleep 5 && \
    ollama pull llama2 && \
    ollama pull codellama && \
    ollama pull mistral
```

## 🔒 Security

### Network Security

```yaml
# Restrict to backend network only
networks:
  backend_network:
    internal: true
```

### Rate Limiting

Implement in reverse proxy (Nginx):

```nginx
limit_req_zone $binary_remote_addr zone=ollama:10m rate=10r/s;

location /v1/chat/completions {
    limit_req zone=ollama burst=20;
    proxy_pass http://ollama:11434;
}
```

## 📈 Scaling

### Horizontal Scaling

Deploy multiple Ollama instances with load balancer:

```
Load Balancer
    │
    ├─→ Ollama Instance 1 (GPU 0)
    ├─→ Ollama Instance 2 (GPU 1)
    └─→ Ollama Instance 3 (GPU 2)
```

## 🤝 Contributing

### Adding New Models

1. Pull the model: `ollama pull model-name`
2. Test with Executor: Update `LLM_ENDPOINT` configuration
3. Document model purpose and requirements
4. Update this README

### Testing

```bash
# Test model inference
docker exec ollama ollama run llama2 "Test prompt"

# Test API endpoint
curl -X POST http://localhost:11434/api/generate \
  -d '{"model":"llama2","prompt":"test"}'
```

## 📚 Resources

- [Ollama Documentation](https://ollama.ai/docs)
- [Ollama GitHub](https://github.com/ollama/ollama)
- [Model Library](https://ollama.ai/library)
- [NVIDIA Container Toolkit](https://github.com/NVIDIA/nvidia-docker)

## 📞 Support

For issues and questions:
- Check Ollama logs: `docker-compose logs ollama`
- Verify GPU: `nvidia-smi`
- Test API: `curl http://localhost:11434/api/tags`
- Open an issue on GitHub
- Contact the development team
