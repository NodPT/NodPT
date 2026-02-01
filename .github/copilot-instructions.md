# briefs
NodPT is a multi-service app with a Riot + Vite frontend, a .NET WebAPI, a SignalR hub, Redis for coordination, and AI/Executor services for model work.

### Frontend communication:
- WebAPI: HTTP calls via the frontend API plugin for auth, projects, prompts, and chat requests.
- SignalR: persistent connection for realtime updates and streaming events; connectionId is stored client-side and sent with chat requests.

### Request flow (high level):
- The frontend sends a chat/request to WebAPI, including the SignalR connectionId.
- WebAPI validates/authenticates, persists request data, and enqueues work to Redis.
- Executor/AI services pull work from Redis, call AI endpoints/models, then inject the results to Redis.
- SignalR listens for updates in Redis and pushes incremental results to the frontend.
  
### Component roles:

- WebAPI: orchestration, auth, persistence, routing requests to background processing.
- Redis: queue/backplane for async work and coordination.
- SignalR: realtime channel to push progress and streaming results.
- AI/Executor: model execution, streaming tokens, and completion output.


## NodPT Frontend (Riot + Vite) instructions

### Scope
- Applies to Frontend app in [Frontend/src](Frontend/src).
- Tech: Riot 10, Vite, ES6+ JavaScript, Bootstrap 5, bootstrap-icons, litegraph.js.

### Architecture & entry points
- App boot: [Frontend/src/src/index.js](Frontend/src/src/index.js) mounts [Frontend/src/src/app.riot](Frontend/src/src/app.riot) 
- Main graph editor: [Frontend/src/src/components/graph.riot](Frontend/src/src/components/graph.riot) using litegraph.js and `editorService.js` (AddNode, RemoveNode, Clear). Director node is always present and cannot be removed or cleared.
- Node types: Director, Manager, Inspector, Worker.

### Folder structure
- [Frontend/src/src/components](Frontend/src/src/components): `.riot` components only.
- [Frontend/src/src/services](Frontend/src/src/services): data and logic services.
- [Frontend/src/src/plugins](Frontend/src/src/plugins): plugin integrations.
- [Frontend/src/src/styles](Frontend/src/src/styles): global CSS themes (dark/light).

### Routing
- Use `@riotjs/route` in [Frontend/src/src/app.riot](Frontend/src/src/app.riot) with `<router>` and `<route>`.
- Pages are lazy-loaded via `@riotjs/lazy` + Loader; NotFound when no route matches.

### Styling rules
- **No inline styles** in `.riot` files. Put all CSS in [Frontend/src/src/styles](Frontend/src/src/styles).
- Theme toggle via `body[data-theme]` (see `useTheme.js`).
- **No public CDNs/remote fonts/links**. Keep assets local.

### Component coding conventions
- `.riot` components have no `<template>` tag.
- Avoid inline execution in handlers. Use `onclick={copyMessage}` or `onclick={copyMessage(message)}` with function defined in `<script>`.
- No spaces inside binding braces: `{functionName}` not `{ functionName }`.
- Place `setApi` calls in `onMounted`, not `onBeforeMount`.

### Services & APIs
- HTTP via `api-plugin.js` (axios wrapper). Use `this.api` (already installed); do not re-import.
- Token storage via `tokenStorage.js` (local/session storage obfuscation).
- Auth: `/auth/login`; in `VITE_ENV=Development` uses mock token.
- Firebase config from `VITE_FIREBASE_SHIT` JSON string (see `firebase.js`).

### Realtime & events
- SignalR in `signalRService.js`, connectionId in localStorage.
- Event bus available as `this.bus`. Use `this.bus.trigger(...)` and `this.bus.on(...)`.
- Chat API attaches `X-SignalR-ConnectionId` when available; SignalR base URL from `VITE_SIGNALR_BASE_URL`.

### LiteGraph usage
- litegraph.js is core for the graph editor. Custom nodes are registered via `LiteGraph.registerNodeType`.
- Use LiteGraph APIs for node inputs/outputs and `onExecute` for runtime behavior.

### Dev workflows (Frontend)
- `npm run dev`, `npm run build`, `npm run preview`, `npm test`.

# WebAPI/.NET instructions
## Endpoints:
- Auth: `/auth/login` (POST) - returns JWT token.
- Projects: `/projects` (GET, POST), `/projects/{id}` (GET, PUT, DELETE).
- Prompts: `/prompts` (GET, POST), `/prompts/{id}` (GET, PUT, DELETE).
- Chat: `/chat` (POST) - submits chat request.
- SignalR Hub: `/hubs/updates` - for realtime updates.
- Template: `/templates` (GET) - fetches available node templates.
- Nodes: `/nodes` (GET) - fetches available node types.
