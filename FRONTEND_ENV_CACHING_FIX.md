# Frontend Environment Variable Caching Issue - Resolution

## Problem Summary

The Docker build was caching the old SignalR URL (`https://signalr.nodpt.com`) instead of using the new value (`https://api.nodpt.com`). This caused the frontend to attempt connections to the wrong endpoint even after updating environment variables.

## Root Causes

### 1. Outdated `.env.production` File
The file `Frontend/src/.env.production` contained the old URL:
```env
VITE_SIGNALR_BASE_URL=https://signalr.nodpt.com
```

### 2. Vite Build-Time Variable Embedding
**Critical Understanding**: Vite environment variables (prefixed with `VITE_`) are embedded into the JavaScript bundle **at build time**, not at runtime.

This means:
- ✅ Variables are read from `.env.production` during `npm run build` (inside Dockerfile)
- ❌ Runtime environment files (like docker-compose `env_file`) **DO NOT** affect `VITE_*` variables
- ❌ Changing runtime environment files will **NOT** update the built JavaScript bundle
- ✅ To change `VITE_*` variables, you **MUST** update `.env.production` and **rebuild the Docker image**

### 3. Docker Caching Misconception
Even with `--no-cache` flag, if the `.env.production` file hasn't changed in the git commit, the old values persist because they are embedded in the source code being built.

### 4. Debug Alert in Code
There was a debug alert on line 56 of `signalRService.js` that should be removed for production.

## Solution Applied

### Files Modified

1. **Frontend/src/.env.production**
   ```diff
   - VITE_SIGNALR_BASE_URL=https://signalr.nodpt.com
   + VITE_SIGNALR_BASE_URL=https://api.nodpt.com
   ```

2. **Frontend/src/src/service/signalRService.js**
   - Removed debug alert that was displaying the URL
   - Cleaned up code formatting

3. **Frontend/README.md**
   - Added comprehensive documentation about Vite environment variable behavior
   - Added troubleshooting section for environment variable issues
   - Clarified the difference between build-time and runtime variables

## Verification Steps

After merging this PR and deployment:

### 1. Verify the Build Uses New URL

```bash
# On the deployment server, after GitHub Actions completes:
docker exec -it nodpt-frontend sh
cat /usr/share/nginx/html/assets/*.js | grep -o "https://[a-z.]*nodpt.com/signalr" | sort -u
```

Expected output: `https://api.nodpt.com/signalr`

### 2. Verify Browser Console

Open the application in a browser and check the console when SignalR initializes. You should see connection attempts to `https://api.nodpt.com/signalr`.

### 3. Check Network Tab

In browser DevTools Network tab, filter for "signalr" or "negotiate" and verify the request is going to:
```
https://api.nodpt.com/signalr/negotiate
```

### 4. Test SignalR Connection

The SignalR service should successfully connect and you should not see any errors about failing to connect to `signalr.nodpt.com`.

## How to Update Environment Variables in the Future

### For Production (`VITE_*` variables)

1. Edit `Frontend/src/.env.production` in the repository
2. Update the desired environment variable
3. Commit and push to `main` branch
4. GitHub Actions will automatically:
   - Pull latest code
   - Build Docker image with `--no-cache`
   - Embed the new values during `npm run build`
   - Deploy the new container

### For Development

1. Create or edit `Frontend/src/.env.local` (not tracked by git)
2. Add your local environment variables
3. Restart the development server: `npm run dev`

## Key Takeaways

1. **Vite variables are build-time**: `VITE_*` variables are compiled into the JavaScript bundle
2. **Source of truth**: For production, `.env.production` is the source of truth
3. **Runtime env files are ineffective**: The `env_file` in docker-compose.yml does NOT affect `VITE_*` variables
4. **Must rebuild**: Any change to `VITE_*` variables requires a complete rebuild
5. **Use `--no-cache`**: Always use `--no-cache` when rebuilding to ensure fresh dependencies

## Additional Notes

### Why Not Use Runtime Environment Variables?

Some deployment solutions inject environment variables at runtime (e.g., using a script to replace placeholders). However:
- This approach is more complex
- Vite's design philosophy is build-time optimization
- Static variables enable better tree-shaking and minification
- The current approach is simpler and follows Vite best practices

### Firebase Configuration

The `VITE_FIREBASE_SHIT` variable is handled differently:
- Passed as a Docker build argument
- Also embedded at build time
- Defined in GitHub Actions secrets and passed to docker-compose

## Related Files

- `Frontend/Dockerfile` - Multi-stage build with Node.js and Nginx
- `Frontend/docker-compose.yml` - Docker Compose configuration
- `.github/workflows/Frontend-deploy.yml` - Automated deployment workflow
- `Frontend/src/vite.config.js` - Vite configuration

## References

- [Vite Environment Variables Documentation](https://vitejs.dev/guide/env-and-mode.html)
- [Docker Build Arguments](https://docs.docker.com/engine/reference/commandline/build/#build-arg)
- [Docker Layer Caching](https://docs.docker.com/build/cache/)
