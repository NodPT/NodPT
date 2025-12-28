#!/bin/bash

# Test script for Ollama connectivity from Executor container
# This script sends a simple "Hello" message to the Ollama service
# to verify network connectivity and API accessibility

echo "======================================"
echo "Ollama Connectivity Test"
echo "======================================"
echo ""
echo "Testing connection to: http://ollama:11434/api/generate"
echo "Model: llama3:8b"
echo "Message: Hello"
echo ""

# Perform the curl request and capture both response and exit code
if response=$(curl -s -X POST http://ollama:11434/api/generate \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama3:8b",
    "prompt": "Hello",
    "stream": false
  }' 2>&1); then
    echo "✓ Successfully connected to Ollama service"
    echo ""
    echo "Response:"
    echo "$response" | jq . 2>/dev/null || echo "$response"
    echo ""
    echo "======================================"
    echo "Test completed successfully!"
    echo "======================================"
    exit 0
else
    echo "✗ Failed to connect to Ollama service"
    echo ""
    echo "Error: Unable to reach http://ollama:11434"
    echo "Please ensure:"
    echo "  1. Ollama container is running"
    echo "  2. Both containers are on the same backend_network"
    echo "  3. Ollama is listening on port 11434"
    echo ""
    echo "======================================"
    echo "Test failed!"
    echo "======================================"
    exit 1
fi
