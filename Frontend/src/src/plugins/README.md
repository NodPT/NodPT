# API Plugin

A minimal Vue 3 plugin that automatically attaches Bearer tokens from localStorage/sessionStorage and provides convenient HTTP methods.

## Features

- 🔐 **Automatic Token Management**: Reads and attaches Bearer tokens automatically from storage
- 🚀 **Simplified API Calls**: All methods return `response.data` directly
- 🔌 **Vue 3 Integration**: Available via Composition API (inject) and Options API ($api)
- ⚙️ **Configurable**: Custom baseURL and timeout options
- 📦 **Minimal**: No external dependencies beyond axios

## Installation

The plugin is already installed in `src/main.js`:

```javascript
import ApiPlugin from './plugins/api-plugin';

const app = createApp(App);
app.use(ApiPlugin); // Uses VITE_API_BASE_URL from .env
// Or with custom options:
// app.use(ApiPlugin, { baseURL: "https://api.yourdomain.com", timeout: 20000 });
```

## Usage

### Composition API (Recommended)

```javascript
import { inject } from "vue";

export default {
  async setup() {
    const api = inject("api");
    
    // GET request
    const projects = await api.get("/projects");
    
    // GET with query parameters
    const filteredProjects = await api.get("/projects", { status: "active" });
    
    // POST request
    const newProject = await api.post("/projects", { name: "My Project" });
    
    // PUT request
    const updated = await api.put("/projects/123", { name: "Updated Name" });
    
    // DELETE request
    await api.delete("/projects/123");
    
    // Custom request with full config
    const data = await api.fetch("/custom", {
      method: "PATCH",
      data: { field: "value" },
      headers: { "X-Custom": "header" }
    });
    
    return { projects };
  }
}
```

### Options API

```javascript
export default {
  async mounted() {
    // All methods available via this.$api
    const projects = await this.$api.get("/projects");
    console.log(projects);
    
    const newProject = await this.$api.post("/projects", {
      name: "My Project"
    });
  }
}
```

## API Methods

### `api.get(url, params?, config?)`
Performs a GET request.
- **url**: Endpoint URL (relative to baseURL)
- **params**: Query parameters object
- **config**: Additional axios config
- **Returns**: Response data directly

### `api.post(url, data?, config?)`
Performs a POST request.
- **url**: Endpoint URL
- **data**: Request body
- **config**: Additional axios config
- **Returns**: Response data directly

### `api.put(url, data?, config?)`
Performs a PUT request.
- **url**: Endpoint URL
- **data**: Request body
- **config**: Additional axios config
- **Returns**: Response data directly

### `api.delete(url, config?)`
Performs a DELETE request.
- **url**: Endpoint URL
- **config**: Additional axios config
- **Returns**: Response data directly

### `api.fetch(url, config?)`
Performs a custom request with full control.
- **url**: Endpoint URL
- **config**: Full axios request config
- **Returns**: Response data directly

### `api.axios`
Direct access to the configured axios instance for advanced usage.

## Token Management

The plugin automatically reads the access token from storage using the existing `tokenStorage` service:
- Looks for `AccessToken` key in both localStorage and sessionStorage
- Attaches as `Authorization: Bearer <token>` header
- If no token is present, requests are sent without Authorization header

## Error Handling

All API methods throw errors on failed requests. Wrap calls in try-catch blocks:

```javascript
try {
  const data = await api.get("/projects");
} catch (error) {
  console.error("API call failed:", error);
  // Handle 401, 403, 500 errors, etc.
}
```

## Integration with Services

The plugin is integrated with existing services. For example, `projectApiService.js` now uses the plugin:

```javascript
import projectApiService from '@/service/projectApiService';

// In components
const projects = await projectApiService.getProjectsByUser(userId);
```

## Configuration

Default configuration:
- **baseURL**: `import.meta.env.VITE_API_BASE_URL` or `"http://localhost:5049/api"`
- **timeout**: 15000ms (15 seconds)

Override during plugin installation:

```javascript
app.use(ApiPlugin, {
  baseURL: "https://api.production.com",
  timeout: 30000
});
```
