# vLLM Docker Installation (Llama 70B FP4)

This guide installs the vLLM OpenAI-compatible server with Docker and exposes it on port `8001`.

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
