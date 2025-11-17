#!/bin/bash
# Example API calls using curl
# Demonstrates OpenAI-compatible endpoints

BASE_URL="http://localhost:8000"

echo "================================================"
echo "NodPT AI Service - API Examples (curl)"
echo "================================================"
echo ""

# Check health
echo "1. Health Check"
echo "----------------"
echo "$ curl ${BASE_URL}/health"
curl -s ${BASE_URL}/health | json_pp 2>/dev/null || curl -s ${BASE_URL}/health
echo -e "\n"

# List models
echo "2. List Available Models"
echo "------------------------"
echo "$ curl ${BASE_URL}/v1/models"
curl -s ${BASE_URL}/v1/models | json_pp 2>/dev/null || curl -s ${BASE_URL}/v1/models
echo -e "\n"

# Text completion
echo "3. Text Completion"
echo "------------------"
echo "$ curl ${BASE_URL}/v1/completions -H 'Content-Type: application/json' -d '{...}'"
curl -s ${BASE_URL}/v1/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-oss-20b",
    "prompt": "Once upon a time in a faraway land",
    "max_tokens": 50,
    "temperature": 0.7
  }' | json_pp 2>/dev/null || curl -s ${BASE_URL}/v1/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-oss-20b",
    "prompt": "Once upon a time in a faraway land",
    "max_tokens": 50,
    "temperature": 0.7
  }'
echo -e "\n"

# Chat completion
echo "4. Chat Completion"
echo "------------------"
echo "$ curl ${BASE_URL}/v1/chat/completions -H 'Content-Type: application/json' -d '{...}'"
curl -s ${BASE_URL}/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-oss-20b",
    "messages": [
      {"role": "system", "content": "You are a helpful assistant."},
      {"role": "user", "content": "What is the capital of France?"}
    ],
    "max_tokens": 100,
    "temperature": 0.7
  }' | json_pp 2>/dev/null || curl -s ${BASE_URL}/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-oss-20b",
    "messages": [
      {"role": "system", "content": "You are a helpful assistant."},
      {"role": "user", "content": "What is the capital of France?"}
    ],
    "max_tokens": 100,
    "temperature": 0.7
  }'
echo -e "\n"

echo "================================================"
echo "✅ All API examples completed"
echo "================================================"
