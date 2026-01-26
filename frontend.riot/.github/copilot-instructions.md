# NodPT frontend.riot (Riot + Vite)

# app brief documentation
- this is the app visualize the AI nodes graph and manage the projects.
- there are 4 node types:
  nodeTypes:
  - Director (cannot delete or clear)
  - Manager
  - Inspector
  - Worker

- file `components/graph.riot` is the main graph editor component. used litegraph.js to visualize the nodes and connections. use `editorService.js` to control the graph with public functions of AddNode, RemoveNode, Clear. Important: Director node is always present and cannot be removed.

## folder structure
frontend.riot
- src
-- components: all the components that are being used in the pages folder, only .riot files are here
-- services: all the services of data and logic
-- plugins: all the tools
-- styles: all the global styles (dark/light themes)
-- nginx: nginx config for production

## Stack and entry points
- Riot 10 + Vite; components live in .riot files (no <template> tag).
- App boot: src/index.js mounts src/app.riot and registers globals via src/register-global-components.js.
- UI uses Bootstrap 5 and bootstrap-icons; 
- litegraph.js is a core dependency for visualizing the AI nodes on the main editor. https://github.com/jagenjo/litegraph.js and https://paladium-developpement.github.io/litegraph.js/
- pure javaScript (ES6+), no TypeScript.
- pure CSS (no SASS/LESS).


## Routing and page structure
- @riotjs/route is wired in src/app.riot; 
- using router and route from the riotjs/route package. Eg.
```html
<app>
  <router>
    <!-- These links will trigger automatically HTML5 history events -->
    <nav>
      <a href="/home">Home</a>
      <a href="/about">About</a>
      <a href="/team/gianluca">Gianluca</a>
    </nav>

    <!-- Your application routes will be rendered here -->
    <route path="/home"> Home page </route>
    <route path="/about"> <About></About> </route>
    <route path="/team/:person"> Hello dear { route.params.person } </route>
  </router>

  <script>
    import { Router, Route } from '@riotjs/route'
    // riot cannot load route by array of components. add route mannually as above.
    export default {
      components: { Router, Route }
    }
  </script>
</app>
```
- Pages are lazy-loaded via @riotjs/lazy + Loader component; NotFound is shown when no route matches.

## Components and styles
- Keep styles in src/styles (dark/light variants exist). Theme is toggled via body[data-theme] (see src/services/useTheme.js).  
- Avoid inline <style> blocks in .riot components at all time.

## API and auth services
- HTTP is centralized in src/services/api-plugin.js (axios wrapper, auto Bearer token from tokenStorage). `api` was already installed in the riot plugins, you don't need to set or import it again, use `this.api`.

Eg.
```javascript
  // GET with query parameters
    const filteredProjects = await this.api.get("/projects", { status: "active" });
    
    // POST request
    const newProject = await this.api.post("/projects", { name: "My Project" });
    
    // PUT request
    const updated = await this.api.put("/projects/123", { name: "Updated Name" });
    
    // DELETE request
    await this.api.delete("/projects/123");
    
    // Custom request with full config
    const data = await this.api.fetch("/custom", {
      method: "PATCH",
      data: { field: "value" },
      headers: { "X-Custom": "header" }
    });
```

- Tokens are stored via src/services/tokenStorage.js (obfuscated in localStorage/sessionStorage).
- Firebase auth config comes from VITE_FIREBASE_SHIT (JSON string) in src/services/firebase.js.
- Auth API login uses /auth/login and stores userData; in VITE_ENV=Development it sends a dev-mock-token (see src/services/authApiService.js).

## Realtime and events
- SignalR connection is managed in src/services/signalRService.js; it persists connectionId in localStorage 
- using this.bus to emits events accross the componentes. `bus` was installed in the riot as plugins, you don't need to set or import it again, use `this.bus`
Eg.
```javascript
 // trigger an event and pass the data to the event
 this.bus.trigger('EventName', data, data2, data3)
 // listen to an event
  this.bus.on('EventName', (data, data2, data3) => {
  // handle the event
  });
```
- Chat API attaches X-SignalR-ConnectionId header when available (src/services/chatApiService.js).
- SignalR endpoints are controlled by VITE_SIGNALR_BASE_URL

## Developer workflows
- dev: npm run dev
- build: npm run build
- preview: npm run preview

# important notes:
- do not use public cdn, links, fonts. all must be local.
- do not use inline styles in .riot components. all styles must be in src/styles folder