FROM redis:7-alpine

# Copy custom configuration
COPY src/redis.conf /usr/local/etc/redis/redis.conf

# Note: vm.overcommit_memory=1 must be configured on the host system
# This cannot be set within the container due to security restrictions
# Run on host: sudo sysctl vm.overcommit_memory=1
# Or add to /etc/sysctl.conf: vm.overcommit_memory = 1 and reboot

EXPOSE 8847

CMD ["redis-server", "/usr/local/etc/redis/redis.conf"]
