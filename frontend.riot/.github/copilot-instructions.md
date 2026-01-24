# NodPT frontend.riot (Riot + Vite)

## Stack and entry points
- Riot 10 + Vite; components live in .riot files (no <template> tag).
- App boot: src/index.js mounts src/app.riot and registers globals via src/register-global-components.js.
- UI uses Bootstrap 5 and bootstrap-icons; litegraph.js is a core dependency.

## Routing and page structure
- @riotjs/route is wired in src/app.riot; routes are declared in src/pages.js.
- Pages are lazy-loaded via @riotjs/lazy + Loader component; NotFound is shown when no route matches.

## Components and styles
- Global components are registered from src/components/global (see src/register-global-components.js).
- Shared “includes” live under src/components/includes.
- Keep styles in src/styles (dark/light variants exist). Theme is toggled via body[data-theme] (see src/services/useTheme.js). Avoid inline <style> blocks in .riot components when possible.

## API and auth services
- HTTP is centralized in src/services/api-plugin.js (axios wrapper, auto Bearer token from tokenStorage). Services are singleton classes with setApi(api) and a baseURL (see src/services/*ApiService.js).
- Tokens are stored via src/services/tokenStorage.js (obfuscated in localStorage/sessionStorage).
- Firebase auth config comes from VITE_FIREBASE_SHIT (JSON string) in src/services/firebase.js.
- Auth API login uses /auth/login and stores userData; in VITE_ENV=Development it sends a dev-mock-token (see src/services/authApiService.js).

## Realtime and events
- SignalR connection is managed in src/services/signalRService.js; it persists connectionId in localStorage and emits events via src/rete/eventBus (imported by auth/firebase/signalR).
- Chat API attaches X-SignalR-ConnectionId header when available (src/services/chatApiService.js).
- SignalR endpoints are controlled by VITE_SIGNALR_BASE_URL and VITE_SIGNALR_HUB_PATH.

## Developer workflows
- dev: npm run dev
- build: npm run build
- test: npm test (Vitest; see src/components/**/**.spec.js)
- preview: npm run preview