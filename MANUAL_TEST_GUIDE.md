# Manual Testing Guide for Ollama Remote Access & Verification

This guide provides step-by-step instructions to manually test the Ollama remote access configuration and verification feature.

## Prerequisites

- Docker and Docker Compose installed
- Access to the NodPT repository
- Terminal access to the deployment environment

## Test Scenarios

### Test 1: Verify Ollama Configuration

**Purpose:** Confirm Ollama is configured with correct environment variables

**Steps:**
```bash
# 1. Navigate to AI directory
cd /home/runner/work/NodPT/NodPT/AI

# 2. Start Ollama container
docker-compose up -d

# 3. Wait for container to be ready (5-10 seconds)
sleep 10

# 4. Verify container is running
docker ps | grep ollama

# 5. Check environment variables are set
docker inspect ollama | grep -A 10 "Env"
```

**Expected Result:**
- Container status: Running
- Environment shows:
  - `OLLAMA_HOST=0.0.0.0:11434`
  - `OLLAMA_ORIGINS=*`

**Pass/Fail Criteria:**
✅ PASS: Both environment variables are present
❌ FAIL: Missing environment variables

---

### Test 2: Test Ollama Accessibility from Host

**Purpose:** Verify Ollama is accessible from the host machine

**Steps:**
```bash
# 1. Test API endpoint
curl http://localhost:11434/api/tags

# 2. Test with a simple generate request
curl -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d '{"model":"llama3.2:3b","prompt":"Hello","stream":false}'
```

**Expected Result:**
- `/api/tags` returns JSON with model list
- `/api/generate` returns JSON with response content

**Pass/Fail Criteria:**
✅ PASS: Both requests return valid JSON responses
❌ FAIL: Connection refused or timeout

---

### Test 3: Test Ollama Accessibility from Executor Container

**Purpose:** Verify Executor can reach Ollama using Docker network

**Steps:**
```bash
# 1. Start Executor (if not already running)
cd /home/runner/work/NodPT/NodPT/Executor
docker-compose up -d

# 2. Test network connectivity from Executor to Ollama
docker exec nodpt-executor curl http://ollama:11434/api/tags

# 3. Test generate endpoint from Executor
docker exec nodpt-executor curl -X POST http://ollama:11434/api/generate \
  -H "Content-Type: application/json" \
  -d '{"model":"llama3.2:3b","prompt":"Hello","stream":false}'
```

**Expected Result:**
- Both curl commands succeed with valid JSON responses

**Pass/Fail Criteria:**
✅ PASS: Executor can reach Ollama via container name
❌ FAIL: Connection refused or name resolution fails

---

### Test 4: Verify Executor Startup Verification

**Purpose:** Confirm the verification service runs at startup and logs correctly

**Steps:**
```bash
# 1. Restart Executor to trigger verification
docker restart nodpt-executor

# 2. Wait a few seconds for startup
sleep 15

# 3. Check logs for verification messages
docker logs nodpt-executor | grep -A 20 "Verifying Ollama"

# 4. Look for success message
docker logs nodpt-executor | grep "Ollama Endpoint Verified Successfully"
```

**Expected Result:**
Logs should show:
```
=== Verifying Ollama Endpoint Connectivity ===
Testing endpoint: http://ollama:11434/api/generate
Ollama verification attempt 1 of 3
Sending test message: 'Hello'
=== Ollama Verification Successful ===
Received response from Ollama: XX characters
=== Ollama Endpoint Verified Successfully ===
Executor is ready to process chat requests.
```

**Pass/Fail Criteria:**
✅ PASS: Verification succeeds on first or subsequent attempts
❌ FAIL: All 3 verification attempts fail

---

### Test 5: Verify Graceful Failure Handling

**Purpose:** Confirm Executor continues running even when Ollama is unavailable

**Steps:**
```bash
# 1. Stop Ollama container
docker stop ollama

# 2. Restart Executor
docker restart nodpt-executor

# 3. Wait and check logs
sleep 15
docker logs nodpt-executor | grep -A 30 "Verifying Ollama"

# 4. Verify Executor is still running
docker ps | grep nodpt-executor

# 5. Restart Ollama for subsequent tests
docker start ollama
```

**Expected Result:**
Logs should show:
```
=== Verifying Ollama Endpoint Connectivity ===
Ollama verification attempt 1 of 3
[Error messages...]
Ollama verification attempt 2 of 3
[Error messages...]
Ollama verification attempt 3 of 3
[Error messages...]
=== WARNING: Ollama Endpoint Verification Failed ===
Executor will continue to start, but chat functionality may not work properly.
Please check Ollama container status and network connectivity.
```

Executor container should still be running despite verification failure.

**Pass/Fail Criteria:**
✅ PASS: Executor continues running and logs warning messages
❌ FAIL: Executor crashes or stops

---

### Test 6: End-to-End Chat Flow

**Purpose:** Verify the entire chat workflow works with Ollama

**Steps:**
```bash
# 1. Ensure all services are running
docker ps | grep -E "ollama|nodpt-executor|nodpt-redis"

# 2. Check that a model is available
docker exec ollama ollama list

# 3. Pull model if not available
docker exec ollama ollama pull llama3.2:3b

# 4. Trigger a chat request (via WebAPI or direct Redis queue)
# This step depends on your WebAPI setup

# 5. Monitor Executor logs for chat processing
docker logs -f nodpt-executor
```

**Expected Result:**
- Chat job is picked up from Redis
- Request sent to Ollama
- Response received
- New message saved to database
- Result published to SignalR

**Pass/Fail Criteria:**
✅ PASS: Chat request completes successfully with AI response
❌ FAIL: Chat request fails or times out

---

## Cleanup

After testing, you can stop the services:
```bash
# Stop Executor
cd /home/runner/work/NodPT/NodPT/Executor
docker-compose down

# Stop Ollama
cd /home/runner/work/NodPT/NodPT/AI
docker-compose down

# Or stop all at once
docker stop nodpt-executor ollama
```

## Troubleshooting Common Issues

### Issue: "Could not resolve host: ollama"
**Cause:** Executor not on backend_network
**Solution:**
```bash
docker network inspect backend_network
docker network connect backend_network nodpt-executor
```

### Issue: "Connection refused"
**Cause:** Ollama not listening on 0.0.0.0
**Solution:** Check environment variables in docker-compose.yml

### Issue: "Model not found"
**Cause:** Model not pulled
**Solution:**
```bash
docker exec ollama ollama pull llama3.2:3b
```

### Issue: Verification times out
**Cause:** Ollama starting up or processing slow
**Solution:** This is expected on first startup; verification will retry

## Test Results Template

| Test # | Test Name | Status | Notes |
|--------|-----------|--------|-------|
| 1 | Ollama Configuration | ⬜ | |
| 2 | Host Accessibility | ⬜ | |
| 3 | Executor Network Access | ⬜ | |
| 4 | Startup Verification | ⬜ | |
| 5 | Graceful Failure | ⬜ | |
| 6 | End-to-End Chat | ⬜ | |

Legend: ✅ Pass | ❌ Fail | ⚠️ Partial | ⬜ Not Tested

## Notes

- Tests should be run in order
- Each test builds on the previous one
- Document any unexpected behavior
- Keep logs from failed tests for analysis
