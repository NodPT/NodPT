#!/bin/bash
set -e

# TensorRT-LLM Entrypoint Script
# Manages model serving with OpenAI-compatible API

MODEL_DIR=${MODEL_DIR:-/models}
ENGINE_DIR=${ENGINE_DIR:-/engines}
DEFAULT_MODEL=${DEFAULT_MODEL:-gpt-oss-20b}

echo "========================================="
echo "TensorRT-LLM Service Starting"
echo "========================================="
echo "Model Directory: ${MODEL_DIR}"
echo "Engine Directory: ${ENGINE_DIR}"
echo "Default Model: ${DEFAULT_MODEL}"
echo "========================================="

# Check if models directory exists
if [ ! -d "${MODEL_DIR}" ]; then
    echo "Creating models directory at ${MODEL_DIR}"
    mkdir -p ${MODEL_DIR}
fi

# Check if engines directory exists
if [ ! -d "${ENGINE_DIR}" ]; then
    echo "Creating engines directory at ${ENGINE_DIR}"
    mkdir -p ${ENGINE_DIR}
fi

# List available models
echo "Checking for available models in ${MODEL_DIR}..."
if [ "$(ls -A ${MODEL_DIR})" ]; then
    echo "Found models:"
    ls -lh ${MODEL_DIR}
else
    echo "No custom models found. You can add models to ${MODEL_DIR}"
fi

# Function to serve the model
serve_model() {
    echo "Starting OpenAI-compatible API server..."
    
    # Check if there's a compiled engine
    if [ -d "${ENGINE_DIR}/${DEFAULT_MODEL}" ]; then
        echo "Using pre-compiled engine: ${ENGINE_DIR}/${DEFAULT_MODEL}"
        python3 /workspace/serve_openai.py \
            --engine_dir ${ENGINE_DIR}/${DEFAULT_MODEL} \
            --host 0.0.0.0 \
            --port 8000
    elif [ -d "${MODEL_DIR}/${DEFAULT_MODEL}" ]; then
        echo "Using model from: ${MODEL_DIR}/${DEFAULT_MODEL}"
        echo "Note: Engine will be built on first use"
        python3 /workspace/serve_openai.py \
            --model_dir ${MODEL_DIR}/${DEFAULT_MODEL} \
            --engine_dir ${ENGINE_DIR}/${DEFAULT_MODEL} \
            --host 0.0.0.0 \
            --port 8000
    else
        echo "WARNING: No model or engine found for ${DEFAULT_MODEL}"
        echo "Please place your model in ${MODEL_DIR}/${DEFAULT_MODEL}"
        echo "Or specify a different model with DEFAULT_MODEL environment variable"
        echo ""
        echo "Starting server in standby mode..."
        # Keep container running for manual intervention
        tail -f /dev/null
    fi
}

# Handle different commands
case "$1" in
    serve)
        serve_model
        ;;
    bash)
        /bin/bash
        ;;
    *)
        # Execute any other command
        exec "$@"
        ;;
esac
