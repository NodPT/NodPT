# TensorRT-LLM Dockerfile for NodPT
# Based on NVIDIA TensorRT-LLM guidelines
# https://github.com/NVIDIA/TensorRT-LLM
# Configured for GPT-OSS-20B model with OpenAI-compatible API

FROM nvcr.io/nvidia/pytorch:23.10-py3

# Set environment variables
ENV DEBIAN_FRONTEND=noninteractive \
    PYTHONUNBUFFERED=1 \
    MODEL_DIR=/models \
    ENGINE_DIR=/engines \
    TRT_LLM_HOME=/workspace/TensorRT-LLM

# Install system dependencies
RUN apt-get update && apt-get install -y \
    git \
    git-lfs \
    wget \
    curl \
    vim \
    build-essential \
    cmake \
    openmpi-bin \
    libopenmpi-dev \
    && rm -rf /var/lib/apt/lists/*

# Upgrade pip and install basic tools
RUN pip install --upgrade pip setuptools wheel

# Clone TensorRT-LLM repository
WORKDIR /workspace
RUN git clone https://github.com/NVIDIA/TensorRT-LLM.git
WORKDIR /workspace/TensorRT-LLM

# Install TensorRT-LLM Python dependencies
RUN pip install -r requirements.txt

# Build and install TensorRT-LLM
RUN python3 ./scripts/build_wheel.py --clean --trt_root /usr/local/tensorrt && \
    pip install ./build/tensorrt_llm*.whl

# Install dependencies for GPT models and OpenAI-compatible server
RUN pip install \
    transformers>=4.36.0 \
    sentencepiece \
    protobuf \
    accelerate \
    datasets \
    huggingface_hub

# Create directories for models and engines
RUN mkdir -p ${MODEL_DIR} ${ENGINE_DIR}

# Copy entrypoint script for service management
COPY src/entrypoint.sh /entrypoint.sh
COPY src/serve_openai.py /workspace/serve_openai.py
RUN chmod +x /entrypoint.sh

# Expose port for OpenAI-compatible API
EXPOSE 8000

# Set working directory
WORKDIR /workspace

# Default entrypoint
ENTRYPOINT ["/entrypoint.sh"]
CMD ["serve"]
