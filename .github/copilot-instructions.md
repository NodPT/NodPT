📄 Overview
This project builds a visual AI-assisted workflow editor frontend using Vue 3, Rete.js, and Bootstrap 5. The goal is to create a clean, modular UI split into four main areas:
■ Top Bar: Fixed controls for nodes, project management, AI tools, user profile.
■ Bottom Bar: Fixed status area with zoom controls, build progress, and a minimap toggle.
■ Left Panel: Rete.js canvas for visual node editing (resizable).
■ Right Panel: Tab container for AI chat, notebook, logs, timeline, and properties (resizable).

📦 NodPT Repository Structure
```
NodPT/
├── Frontend/
│   ├── Dockerfile
│   ├── docker-compose.yml
│   └── src/
│       ├── components/
│       ├── views/
│       ├── service/
│       ├── plugins/
│       └── ...
│
├── WebAPI/
│   ├── Dockerfile
│   ├── docker-compose.yml
│   └── src/
│       └── ... (C# .NET Web API)
│
├── SignalR/
│   ├── Dockerfile
│   ├── docker-compose.yml
│   └── src/
│       └── ... (Real-time communication hub)
│
├── Executor/
│   ├── Dockerfile
│   ├── docker-compose.yml
│   └── src/
│       └── ... (Task execution service)
│
├── Redis/
│   ├── Dockerfile
│   └── ... (Caching and message broker)
│
├── AI/
│   ├── Dockerfile
│   └── ... (AI processing service)
│
└── .github/
    ├── workflows/
    │   ├── Frontend-deploy.yml
    │   ├── Backend-deploy.yml
    │   ├── SignalR-deploy.yml
    │   └── (Executor-deploy.yml - to be added)
    └── copilot-instructions.md
```


📚 Libraries To Use
Vue 3 (Composition API)
Rete.js (https://retejs.org/)
Bootstrap 5 (https://getbootstrap.com/)
Vue Router (for navigation)
Vuex / Tiny Emitter (eventBus) (for state management if needed)
FontAwesome (for icons)


📁 Folder structure for frontend:
/src
/components
/views
/public
App.vue
main.js

🖱️ 2. Top Bar (Fixed Header)
User menu 
File: components/TopBar.vue buttons:
Project Controls: New, Open, Save, Export, Build, Run, Publish.
Node Controls: Add Node, Clear, Group, Ungroup, Lock, Unlock.
Sesrch button

🖱️ 3. Create Bottom Bar (Fixed Footer)
File: components/BottomBar.vue
■ Selected Node Status
■ Arrage nodes
■ Build Progress Bar
■ 💡 Minimap Toggle Button (show/hide minimap component)
■ Use Bootstrap’s fixed-bottom class for positioning.

🖱️ 4. Create Left Panel (Resizable Rete.js Canvas)
File: components/LeftPanel.vue
Render Rete.js editor canvas.
This panel is where nodes are displayed and manipulated.

🖱️ 5. Create Right Panel (floating panel)
File: components/RightPanel.vue
Use Bootstrap nav-tabs for tabs:
■ AI Chat: Interactive chat UI
■ Logs: Real-time logs
■ Properties: Selected node configuration panel
■ Files

🖱️ 7. Assemble Main Editor View
File: views/MainEditor.vue
Combine TopBar, BottomBar, LeftPanel, RightPanel into a responsive layout

🖱️ 8. App.vue
Load MainEditor.vue.
Setup Vue Router if needed for additional pages.

## useful components can be used during coding
■ api-plugin already has all functions of crud by using axios and bearer token. Use this plugin by calling const api=inject('api'). The available functions are get, put, delete, post. Pass the parameters as same with axios function. 
■ **Important: Don't use axios directly in components or services.** All HTTP requests should use the api plugin which automatically handles authentication headers.
■ **For services**: Service classes should have a `setApi(api)` method. Components must inject the api and call `service.setApi(api)` before using the service:
  ```javascript
  // In component setup()
  const api = inject('api');
  someService.setApi(api);
  ```

📝 Keep It Simple
Each Vue component should be self-contained.
Avoid over-engineering or unnecessary abstractions.
No backend logic is needed; use mock data for testing UI.
Use clear naming conventions for props and events.
Use eventBus instead of watch

✅ Important Notes

-  Focus only on frontend UI layout and interactions.
-  Use mock data for node status and AI chat until backend is ready.
-  Minimap toggle must dynamically show/hide the minimap overlay.
-  Strictly use only bootstrap 5 for styling and layout. do not introduce other CSS frameworks. do not use custom CSS.
-  Ensure all components are responsive and work well on different screen sizes.
-  Do not use camelCase for data from backend, keep it as it is, properly data from backend uses PascalCase.
