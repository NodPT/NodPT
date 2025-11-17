# Use NVIDIA TensorRT-LLM base image with CUDA support
# This image includes TensorRT, CUDA, and Python environment
FROM nvidia/cuda:12.1.0-devel-ubuntu22.04

# Set environment variables
ENV DEBIAN_FRONTEND=noninteractive
ENV CUDA_MODULE_LOADING=LAZY
ENV PATH=/usr/local/cuda/bin:${PATH}
ENV LD_LIBRARY_PATH=/usr/local/cuda/lib64:${LD_LIBRARY_PATH}

# Set working directory
WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y \
    python3.10 \
    python3-pip \
    python3-dev \
    git \
    wget \
    curl \
    vim \
    libopenmpi-dev \
    openmpi-bin \
    && rm -rf /var/lib/apt/lists/*

# Upgrade pip and install build tools
RUN python3 -m pip install --upgrade pip setuptools wheel

# Install TensorRT-LLM and its dependencies
# Using specific version that supports FP4 quantization
RUN pip install --no-cache-dir \
    tensorrt_llm==0.10.0 \
    transformers>=4.38.0 \
    torch>=2.1.0 \
    numpy \
    huggingface-hub \
    accelerate \
    sentencepiece \
    protobuf

# Install additional dependencies for model serving
RUN pip install --no-cache-dir \
    fastapi \
    uvicorn[standard] \
    pydantic \
    python-multipart

# Create directories for models
RUN mkdir -p /models/base \
    && mkdir -p /models/finetuned \
    && mkdir -p /app/logs

# Copy application source files
COPY src/ ./

# Set default model directory
ENV MODEL_DIR=/models/finetuned
ENV BASE_MODEL_DIR=/models/base

# Expose port for API
EXPOSE 8850

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8850/health || exit 1

# Default command to run the service
CMD ["python3", "-m", "uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8850"]
