#!/bin/bash

# Executor Test Runner Script
# This script helps run the Executor integration tests with proper configuration

set -e

echo "======================================"
echo "Executor Integration Tests"
echo "======================================"
echo ""

# Check if Redis is accessible
echo "Checking Redis connection..."
if command -v redis-cli &> /dev/null; then
    REDIS_HOST="${REDIS_HOST:-localhost}"
    REDIS_PORT="${REDIS_PORT:-6379}"
    
    if redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" ping &> /dev/null; then
        echo "✓ Redis is accessible at $REDIS_HOST:$REDIS_PORT"
    else
        echo "⚠ Warning: Cannot connect to Redis at $REDIS_HOST:$REDIS_PORT"
        echo "  Tests may fail if Redis is not running"
    fi
else
    echo "⚠ Warning: redis-cli not found. Cannot verify Redis connection."
fi
echo ""

# Check if Ollama is accessible
echo "Checking Ollama connection..."
OLLAMA_HOST="${OLLAMA_HOST:-http://ollama:11434}"

if command -v curl &> /dev/null; then
    if curl -s -o /dev/null -w "%{http_code}" "$OLLAMA_HOST/api/tags" | grep -q "200"; then
        echo "✓ Ollama is accessible at $OLLAMA_HOST"
        
        # Check for required models
        echo ""
        echo "Checking for required models..."
        if curl -s "$OLLAMA_HOST/api/tags" | grep -q "deepseek-r1:1.5b"; then
            echo "✓ Model deepseek-r1:1.5b is available"
        else
            echo "⚠ Warning: Model deepseek-r1:1.5b not found"
            echo "  Pull it with: ollama pull deepseek-r1:1.5b"
        fi
        
        if curl -s "$OLLAMA_HOST/api/tags" | grep -q "llama3.2:1b"; then
            echo "✓ Model llama3.2:1b is available"
        else
            echo "⚠ Warning: Model llama3.2:1b not found"
            echo "  Pull it with: ollama pull llama3.2:1b"
        fi
    else
        echo "⚠ Warning: Cannot connect to Ollama at $OLLAMA_HOST"
        echo "  Tests may fail if Ollama is not running"
    fi
else
    echo "⚠ Warning: curl not found. Cannot verify Ollama connection."
fi
echo ""

# Navigate to test directory
cd "$(dirname "$0")"

# Display configuration
echo "Test Configuration:"
echo "  Redis: ${Redis__ConnectionString:-localhost:6379}"
echo "  Ollama: ${Ollama__BaseUrl:-http://ollama:11434}"
echo "  Default Model: ${Ollama__DefaultModel:-deepseek-r1:1.5b}"
echo ""

# Check command line arguments
TEST_FILTER="${1:-}"
VERBOSITY="${2:-normal}"

if [ -n "$TEST_FILTER" ]; then
    echo "Running filtered tests: $TEST_FILTER"
    dotnet test --filter "$TEST_FILTER" --logger "console;verbosity=$VERBOSITY"
else
    echo "Running all tests..."
    dotnet test --logger "console;verbosity=$VERBOSITY"
fi

echo ""
echo "======================================"
echo "Tests completed!"
echo "======================================"
