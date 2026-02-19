
# Example for version 1.3.7
wget https://github.com/rustdesk/rustdesk/releases/download/1.4.5/rustdesk-1.4.5-aarch64.deb
sudo apt update
sudo apt install ./rustdesk-1.4.5-aarch64.deb


export LATEST_VLLM_VERSION=nvcr.io/nvidia/vllm:26.01-py3

# example
# export LATEST_VLLM_VERSION=26.01-py3

docker pull nvcr.io/nvidia/vllm:${LATEST_VLLM_VERSION}

docker run -it --gpus all -p 8001:8001 \
nvcr.io/nvidia/vllm:${LATEST_VLLM_VERSION} \
vllm serve "Qwen/Qwen2.5-Math-1.5B-Instruct"

100.82.84.53

curl http://localhost:8001/v1/chat/completions \
-H "Content-Type: application/json" \
-d '{
    "model": "Qwen/Qwen2.5-1.5B-Instruct",
    "messages": [{"role": "user", "content": "12*17"}],
    "max_tokens": 500
}'

docker run -it --gpus all -p 8001:8000 \
nvcr.io/nvidia/vllm:${LATEST_VLLM_VERSION} \
vllm serve Qwen/Qwen2.5-1.5B-Instruct \
  --gpu-memory-utilization 0.60 \
  --max-model-len 8192 \
  --dtype auto \
  --enable-chunked-prefill \
  --max-num-batched-tokens 16384

docker stop ecce9729a8e2

export LLM_MODEL=RedHatAI/Llama-3.3-70B-Instruct-NVFP4
export LATEST_VLLM_VERSION=26.01-py3

docker run -it --gpus all -p 8001:8000 \
nvcr.io/nvidia/vllm:${LATEST_VLLM_VERSION} \
vllm serve ${LLM_MODEL} \
  --gpu-memory-utilization 0.65 \
  --max-model-len 16384 \
  --dtype auto \
  --trust-remote-code \
  --enable-chunked-prefill \
  --max-num-batched-tokens 32768


curl http://localhost:8001/v1/chat/completions \
-H "Content-Type: application/json" \
-d '{
    "model": "${LLM_MODEL}",
    "messages": [{"role": "user", "content": "12*17"}],
    "max_tokens": 500
}'

