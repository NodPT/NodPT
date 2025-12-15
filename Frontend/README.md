# NodPT Frontend

Visual workflow editor built with Vue.js 3, Rete.js, and Bootstrap 5. This is the user-facing web application that provides an intuitive drag-and-drop interface for creating AI-powered workflows.

## 🛠️ Technology Stack

- **Vue.js 3**: Progressive JavaScript framework with Composition API
- **Rete.js 2.x**: Framework for visual programming and node-based editors
- **Bootstrap 5.3**: Modern CSS framework for responsive UI
- **Vite 7**: Next-generation frontend build tool
- **SignalR**: Real-time communication with backend
- **Firebase**: Authentication and user management
- **Axios**: HTTP client for API requests
- **Font Awesome & Bootstrap Icons**: Icon libraries

### Key Libraries

- **rete-area-plugin**: Canvas area management
- **rete-auto-arrange-plugin**: Automatic node layout
- **rete-connection-plugin**: Node connections
- **rete-context-menu-plugin**: Right-click context menus
- **rete-history-plugin**: Undo/redo functionality
- **rete-minimap-plugin**: Minimap overview
- **Vue Router**: Navigation and routing
- **Tiny Emitter**: Event bus for component communication

## 🏗️ Architecture

### Folder Structure

```
Frontend/
├── src/
│   ├── components/        # Reusable Vue components
│   │   ├── TopBar.vue    # Project and node controls
│   │   ├── BottomBar.vue # Status and build progress
│   │   ├── LeftPanel.vue # Rete.js canvas
│   │   └── RightPanel.vue # AI chat, logs, properties tabs
│   ├── views/            # Page components
│   │   └── MainEditor.vue # Main editor layout
│   ├── service/          # API and service layer
│   ├── plugins/          # Vue plugins (API, SignalR, etc.)
│   ├── router/           # Vue Router configuration
│   ├── assets/           # Static assets (images, styles)
│   ├── App.vue           # Root component
│   └── main.js           # Application entry point
├── public/               # Public static files
├── nginx/                # Nginx configuration for production
├── Dockerfile            # Multi-stage Docker build
├── docker-compose.yml    # Docker Compose configuration
└── package.json          # Dependencies and scripts
```

### Main Components

- **TopBar**: File controls (New, Open, Save...) and node controls (Add, Delete...)
- **BottomBar**: Selected node status, build progress, minimap toggle, Arrange nodes, Toggle Left/Right panels
- **LeftPanel**: Rete.js canvas for visual node editing (resizable)
- **RightPanel**: Tabbed interface for AI Chat, Logs, Properties, Files (floating panel)

### API Plugin

The frontend uses a custom API plugin (`src/plugins/api-plugin`) that handles all HTTP requests with automatic authentication.

**Important**: Always use the API plugin instead of Axios directly:

```javascript
// In component setup()
const api = inject('api');

// GET request
const data = await api.get('/api/endpoint');

// POST request
const result = await api.post('/api/endpoint', { data: 'value' });

// PUT request
await api.put('/api/endpoint/id', { data: 'updated' });

// DELETE request
await api.delete('/api/endpoint/id');
```

For services, inject the API and set it before use:

```javascript
// In service file
class MyService {
  setApi(api) {
    this.api = api;
  }
  
  async getData() {
    return await this.api.get('/api/data');
  }
}

// In component
const api = inject('api');
const service = new MyService();
service.setApi(api);
```

### EventBus Plugin

The frontend uses **Tiny Emitter** for an event bus system, enabling loose coupling between components.

**Setup** (already configured in `src/plugins/eventbus-plugin.js`):

```javascript
import TinyEmitter from 'tiny-emitter/instance';

// The eventBus is registered as a Vue plugin and injected into all components
app.provide('eventBus', TinyEmitter);
```

**Emit an Event** (send data from one component):

```javascript
import { inject } from 'vue';

export default {
  setup() {
    const eventBus = inject('eventBus');
    
    const handleNodeSelected = (nodeId) => {
      // Emit event with data
      eventBus.emit('node:selected', { nodeId, timestamp: Date.now() });
    };
    
    const handleWorkflowSaved = (workflowData) => {
      eventBus.emit('workflow:saved', workflowData);
    };
    
    return { handleNodeSelected, handleWorkflowSaved };
  }
};
```

**Listen to an Event** (receive data in another component):

```javascript
import { inject, onMounted, onUnmounted } from 'vue';

export default {
  setup() {
    const eventBus = inject('eventBus');
    
    const onNodeSelected = (data) => {
      console.log('Node selected:', data.nodeId);
      // Update component state based on event
    };
    
    onMounted(() => {
      // Register listener when component mounts
      eventBus.on('node:selected', onNodeSelected);
    });
    
    onUnmounted(() => {
      // Clean up listener when component unmounts
      eventBus.off('node:selected', onNodeSelected);
    });
    
    return {};
  }
};
```

**Best Practices**:

- Always use `onUnmounted()` to remove listeners and prevent memory leaks
- Use descriptive event names with namespacing (e.g., `node:selected`, `workflow:saved`)
- Pass event data as a single object for clarity
- Emit from child components and listen in parent/sibling components
- Avoid event bus for simple parent-child communication (use props and events instead)
- Document emitted events in component comments

## 🚀 Getting Started

### Prerequisites

- Node.js 20.x or later
- npm 9.x or later
- Docker (for containerized deployment)

### Local Development

1. **Clone the repository**:
   ```bash
   cd Frontend/src
   ```

2. **Install dependencies**:
   ```bash
   npm install
   ```

3. **Create environment file** (`.env.local`):
   ```env
   # Firebase Configuration
   VITE_FIREBASE_API_KEY=your-api-key
   VITE_FIREBASE_AUTH_DOMAIN=your-project.firebaseapp.com
   VITE_FIREBASE_PROJECT_ID=your-project-id
   VITE_FIREBASE_STORAGE_BUCKET=your-project.appspot.com
   VITE_FIREBASE_MESSAGING_SENDER_ID=your-sender-id
   VITE_FIREBASE_APP_ID=your-app-id
   
   # API Configuration
   VITE_API_BASE_URL=http://localhost:5049/api
   
   # SignalR Configuration
   VITE_SIGNALR_BASE_URL=http://localhost:5049
   VITE_SIGNALR_HUB_PATH=/signalr
   ```

4. **Run development server**:
   ```bash
   npm run dev
   ```

   The application will be available at `http://localhost:5173`

5. **Build for production**:
   ```bash
   npm run build
   ```

6. **Preview production build**:
   ```bash
   npm run preview
   ```

### Linting

```bash
npm run lint
```

## 🐳 Docker Deployment

### Environment Setup

Create the environment file at `/home/runner_user/envs/frontend.env`:

```env
# Firebase Configuration (encoded as single variable), sorry for my language... just easier this way to remove from codebase
VITE_FIREBASE_SHIT={"apiKey":"xxx","authDomain":"xxx","projectId":"xxx","storageBucket":"xxx","messagingSenderId":"xxx","appId":"xxx"}

# Or individual variables
VITE_FIREBASE_API_KEY=your-api-key
VITE_FIREBASE_AUTH_DOMAIN=your-project.firebaseapp.com
VITE_FIREBASE_PROJECT_ID=your-project-id
VITE_FIREBASE_STORAGE_BUCKET=your-project.appspot.com
VITE_FIREBASE_MESSAGING_SENDER_ID=your-sender-id
VITE_FIREBASE_APP_ID=your-app-id

# API Endpoints
VITE_API_BASE_URL=http://nodpt-api:8846/api

# SignalR Configuration
VITE_SIGNALR_BASE_URL=http://nodpt-signalr:8846
VITE_SIGNALR_HUB_PATH=/signalr
```

### Build and Run with Docker

```bash
# Create network if not exists
docker network create frontend_network

# Build the image
docker-compose build

# Start the container
docker-compose up -d

# View logs
docker-compose logs -f

# Stop the container
docker-compose down
```

The frontend will be accessible at `http://localhost:8443`

### Dockerfile Stages

1. **Build Stage**: Uses Node.js 20 Alpine to build the application
2. **Production Stage**: Uses Nginx Alpine to serve static files

## 🔥 Firebase Setup

1. Create a Firebase project at [Firebase Console](https://console.firebase.google.com/)
2. Enable Authentication with desired providers
3. Get your Firebase configuration from Project Settings
4. Add configuration to environment variables
5. Set up Firebase Admin SDK on the backend (WebAPI) for token validation

## 📡 SignalR Integration

The frontend connects to SignalR for real-time updates:

```javascript
// SignalR is configured in src/service/signalRService.js
// It automatically connects when user is authenticated
import signalRService from '@/service/signalRService';

// Listen for node updates
signalRService.on('ReceiveNodeUpdate', (data) => {
  console.log('Node update:', data);
});

// Send message
await signalR.invoke('SendMessage', user, message, targetGroup);
```

### SignalR Events

- **ReceiveNodeUpdate**: Node execution results from Executor
- **ReceiveMessage**: General messages
- **JoinedGroup**: Confirmation of joining a workflow group

## 🎨 Styling Guidelines

- **Bootstrap 5 Only**: Use Bootstrap 5 classes for all styling
- **No Custom CSS Frameworks**: Do not introduce other CSS frameworks
- **Custom CSS**: Only when necessary, keep in separate files
- **Responsive Design**: Ensure components work on all screen sizes

## 📝 Development Guidelines

### Component Conventions

- Use Vue 3 Composition API
- Use PascalCase for data from backend (backend sends PascalCase)
- Use eventBus (Tiny Emitter) instead of `watch` for cross-component communication
- Keep components self-contained and modular
- Use clear naming for props and events

### Code Style

- Follow existing patterns in the codebase
- Use single quotes for strings
- Use 2-space indentation (tabs)
- Comment only when necessary to explain complex logic

### State Management

- Use `inject`/`provide` for dependency injection
- Use eventBus for cross-component events
- Keep component state local when possible

## 🧪 Testing

Currently, no automated tests are configured. When adding tests:

- Use existing testing patterns
- Test critical user workflows
- Test API integration
- Test SignalR connection handling

## 🤝 Contributing

1. Read the main project README and this document
2. Check existing issues or create a new one
3. Fork the repository and create a feature branch
4. Follow the code style and conventions
5. Test your changes thoroughly
6. Submit a pull request with a clear description

### Important Notes

- Always use the API plugin for HTTP requests (not Axios directly)
- Test with both development and production builds
- Ensure Firebase authentication works correctly
- Verify SignalR real-time updates
- Check responsive design on different screen sizes

## 📚 Additional Documentation

- [Firebase & SignalR Auth](src/docs/FIREBASE_SIGNALR_AUTH.md)
- [PWA & SignalR](src/docs/PWA_AND_SIGNALR.md)
- [Implementation Summary](src/docs/IMPLEMENTATION_SUMMARY.md)
- [Architecture Diagram](src/docs/ARCHITECTURE_DIAGRAM.md)
- [Plugin Usage Examples](src/src/plugins/USAGE_EXAMPLES.md)

## 🐛 Troubleshooting

### Common Issues

**Build fails with module errors**:
```bash
rm -rf node_modules package-lock.json
npm ci
```

**SignalR connection fails**:
- Verify SignalR service is running
- Check VITE_SIGNALR_URL in environment
- Ensure Firebase token is valid

**API requests fail**:
- Verify WebAPI service is running
- Check VITE_API_BASE_URL in environment
- Check browser console for CORS errors

**Docker build fails**:
- Ensure VITE_FIREBASE_SHIT build arg is provided
- Check environment file path in docker-compose.yml
- Verify network exists: `docker network create frontend_network`

## 📞 Support

For issues and questions:
- Open an issue on GitHub
- Check existing documentation
- Contact the development team
