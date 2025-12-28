# Ollama Connectivity Test Implementation

## Overview

This document describes the implementation of curl in the Executor Docker container and the Ollama connectivity test as requested in the issue.

## Changes Made

### 1. Updated Dockerfile

**File:** `Executor/Dockerfile`

Added curl and jq installation in the runtime stage:

```dockerfile
# Install curl for connectivity testing
RUN apt-get update && apt-get install -y curl jq && rm -rf /var/lib/apt/lists/*
```

This command:
- Updates the package lists (`apt-get update`)
- Installs curl and jq (`apt-get install -y curl jq`)
- Cleans up package lists to reduce image size (`rm -rf /var/lib/apt/lists/*`)

### 2. Created Test Script

**File:** `Executor/test-ollama-connectivity.sh`

A bash script that tests connectivity to the Ollama service by sending a POST request with:
- **Endpoint:** `http://ollama:11434/api/generate`
- **Model:** `llama3:8b` (as specified in the issue)
- **Prompt:** "Hello"
- **Stream:** `false` (synchronous response)

The script provides clear output indicating success or failure and includes troubleshooting tips.

### 3. Integration into Docker Image

The test script is copied into the Docker image during the build process:

```dockerfile
# Copy the Ollama connectivity test script
COPY Executor/test-ollama-connectivity.sh /app/test-ollama-connectivity.sh
RUN chmod +x /app/test-ollama-connectivity.sh
```

This ensures the test is always available in deployed containers.

### 4. Updated Documentation

**File:** `Executor/README.md`

Added a new section "Ollama Connectivity Test" under the Testing section with:
- Instructions on how to run the test
- Description of what the test does
- Expected behavior

## Usage

### Running the Test

Once the Executor container is built and running with the changes:

```bash
# Run the test from outside the container
docker exec nodpt-executor bash /app/test-ollama-connectivity.sh

# Or run it interactively inside the container
docker exec -it nodpt-executor bash
/app/test-ollama-connectivity.sh
```

### Expected Output

**Success:**
```
======================================
Ollama Connectivity Test
======================================

Testing connection to: http://ollama:11434/api/generate
Model: llama3:8b
Message: Hello

✓ Successfully connected to Ollama service

Response:
{
  "model": "llama3:8b",
  "created_at": "...",
  "response": "...",
  "done": true
}

======================================
Test completed successfully!
======================================
```

**Failure:**
```
======================================
Ollama Connectivity Test
======================================

Testing connection to: http://ollama:11434/api/generate
Model: llama3:8b
Message: Hello

✗ Failed to connect to Ollama service

Error: Unable to reach http://ollama:11434
Please ensure:
  1. Ollama container is running
  2. Both containers are on the same backend_network
  3. Ollama is listening on port 11434

======================================
Test failed!
======================================
```

## Prerequisites

For the test to work successfully:

1. **Ollama container must be running:**
   ```bash
   docker ps | grep ollama
   ```

2. **Both containers must be on the same network:**
   Both Executor and Ollama should be connected to `backend_network`

3. **Ollama must have the model available:**
   ```bash
   docker exec ollama ollama list
   # Should show llama3:8b in the list
   ```

4. **Pull the model if not available:**
   ```bash
   docker exec ollama ollama pull llama3:8b
   ```

## Network Architecture

The test assumes the following network setup (as defined in the docker-compose files):

```
backend_network (Docker network)
    │
    ├── nodpt-executor (Executor container)
    │   └── Can reach: http://ollama:11434
    │
    └── ollama (Ollama container)
        └── Listening on: 0.0.0.0:11434
```

## Troubleshooting

If the test fails:

1. **Check Ollama container status:**
   ```bash
   docker ps | grep ollama
   docker logs ollama
   ```

2. **Verify network connectivity:**
   ```bash
   docker exec nodpt-executor curl http://ollama:11434/api/tags
   ```

3. **Check if model is available:**
   ```bash
   docker exec ollama ollama list
   ```

4. **Verify Ollama environment variables:**
   ```bash
   docker inspect ollama | grep -A 5 Env
   # Should show: OLLAMA_HOST=0.0.0.0:11434
   ```

## Files Modified

- `Executor/Dockerfile` - Added curl installation and test script integration
- `Executor/README.md` - Added documentation for the test
- `Executor/test-ollama-connectivity.sh` - New test script (created)
- `Executor/OLLAMA_TEST_IMPLEMENTATION.md` - This documentation file (created)

## Notes

- The test uses `llama3:8b` as specified in the issue, which may differ from the current default model (`deepseek-r1:1.5b`) configured in the Executor service
- The test is designed to be run manually for verification purposes
- curl and jq are now available in the Executor container for any debugging or connectivity testing needs
- The test script uses `jq` for JSON formatting but gracefully falls back to raw output if jq is unavailable
- The cleanup command in the Dockerfile (`rm -rf /var/lib/apt/lists/*`) keeps the image size minimal
