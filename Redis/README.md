# Redis Installation

This directory contains the Docker configuration for the NodPT Redis instance.

## Features

- Based on official Redis 7 Alpine image (ARM64 compatible)
- Custom port configuration (8847)
- Persistent data storage
- Optimized for ARM64 Linux servers

## Port Configuration

- **Redis Port**: 8847 (exposed on host)

## Prerequisites

### Memory Overcommit Configuration (IMPORTANT)

To prevent Redis background save and replication failures, you must enable memory overcommit on the host system.

**Option 1: Temporary (until reboot)**
```bash
sudo sysctl vm.overcommit_memory=1
```

**Option 2: Permanent**
```bash
echo "vm.overcommit_memory = 1" | sudo tee -a /etc/sysctl.conf
sudo sysctl -p
```

Without this configuration, you will see the following warning in Redis logs:
```
WARNING Memory overcommit must be enabled! Without it, a background save or 
replication may fail under low memory condition.
```

## CORS Configuration

The Redis configuration allows connections from localhost on any port by setting:
- `bind 0.0.0.0` - Accepts connections from all interfaces
- `protected-mode no` - Allows connections without authentication (for development)

**Note**: Redis itself doesn't handle CORS headers. CORS is typically managed at the application layer (e.g., in your Web API or SignalR service).

## Usage

### Build the Image
```bash
docker compose build --no-cache
```

### Start Redis
```bash
docker compose up -d
```

### Stop Redis
```bash
docker compose down
```

### View Logs
```bash
docker compose logs -f
```

### Test Connection
```bash
docker exec nodpt-redis redis-cli -p 8847 ping
```

Expected output: `PONG`

## File Structure

```
Redis/
├── Dockerfile           # Redis image configuration
├── docker-compose.yml   # Docker Compose configuration
├── src/
│   └── redis.conf      # Custom Redis configuration
└── README.md           # This file
```

## Deployment

Changes to the Redis folder are automatically deployed via GitHub Actions when pushed to the `main` or `master` branch.

See `.github/workflows/Redis-deploy.yml` for deployment configuration.
