# PWA and SignalR Features Documentation

## Table of Contents
1. [Progressive Web App (PWA)](#progressive-web-app-pwa)
2. [SignalR Integration](#signalr-integration)
3. [Usage Examples](#usage-examples)

---

## Progressive Web App (PWA)

### Overview
The application now supports Progressive Web App features, allowing users to install it on their devices and access it offline.

### Features Implemented

#### 1. Add to Home Screen
- Users can install the app on their mobile devices or desktop browsers
- Creates an app-like experience with a standalone window
- Configurable via `public/manifest.json`

#### 2. Service Worker for Caching
- Caches essential files for offline access
- Automatically updates when a new version is available
- Located in `public/service-worker.js`

#### 3. Cache Update Mechanism
- Service worker automatically checks for updates
- Prompts user when a new version is available
- User can choose to reload immediately or continue with current version

### Configuration

#### Manifest Configuration (`public/manifest.json`)
```json
{
  "name": "NodPT - Visual AI Workflow Editor",
  "short_name": "NodPT",
  "description": "Visual AI-assisted workflow editor with Rete.js",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#0d6efd",
  "orientation": "any",
  "icons": [...]
}
```

**Key Configuration Options:**
- `name`: Full application name displayed during installation
- `short_name`: Short name displayed on home screen
- `start_url`: URL to open when app is launched
- `display`: Display mode (standalone, fullscreen, minimal-ui, browser)
- `theme_color`: Browser toolbar color
- `icons`: App icons for different sizes (currently using favicon.ico)

#### Service Worker Cache Configuration
Edit `public/service-worker.js` to configure caching:

```javascript
const CACHE_NAME = 'NodPT-cache-v1.0.0'; // Update version to trigger cache refresh
const urlsToCache = [
  '/',
  '/index.html',
  '/favicon.ico'
  // Add more files to cache
];
```

**To force a cache update:**
1. Increment the `CACHE_NAME` version in `service-worker.js`
2. Deploy the new version
3. Users will be prompted to reload the application

### Installation Instructions

#### For Users
1. Open the application in a supported browser (Chrome, Edge, Safari)
2. Look for the "Install" or "Add to Home Screen" prompt in the browser
3. Click "Install" to add the app to your device
4. The app will appear on your home screen or app launcher

#### For Developers
The service worker is automatically registered in `src/main.js`. No additional configuration is required.

---

## SignalR Integration

### Overview
SignalR provides real-time communication between the editor and the server, enabling features like:
- Live collaboration
- Real-time node updates from server
- Editor commands from server
- Connection status monitoring

### Architecture

#### SignalR Service (`src/service/signalRService.js`)
A singleton service that manages the SignalR connection lifecycle.

**Key Methods:**
- `initialize(hubUrl)`: Initialize connection to SignalR hub
- `start()`: Start the connection
- `stop()`: Stop the connection
- `invoke(methodName, ...args)`: Send messages to server
- `on(eventName, callback)`: Listen to server events
- `getConnectionStatus()`: Get current connection status
- `onStatusChange(callback)`: Subscribe to status changes

#### Connection States
- `disconnected`: Not connected to server
- `connecting`: Attempting to connect
- `connected`: Successfully connected
- `reconnecting`: Connection lost, attempting to reconnect

### Configuration

#### Hub URL Configuration
Update the hub URL in `src/views/MainEditor.vue`:

```javascript
// Initialize SignalR connection
await signalRService.initialize(); // Uses /signalr hub path by default
```

#### Server-Side Requirements
Your backend must implement a SignalR hub. Example ASP.NET Core hub:

```csharp
public class EditorHub : Hub
{
    public async Task SendNodeUpdate(object nodeData)
    {
        await Clients.All.SendAsync("NodeUpdated", nodeData);
    }

    public async Task SendEditorCommand(object command)
    {
        await Clients.All.SendAsync("EditorCommand", command);
    }
}
```

And configure it in your Startup/Program.cs:

```csharp
app.MapHub<EditorHub>("/signalr");
```

### Connection Status Display

The connection status is displayed in the bottom bar (Footer component) with:
- **Green WiFi icon**: Connected
- **Yellow hourglass/spinner**: Connecting/Reconnecting
- **Gray WiFi-off icon**: Disconnected

### Usage Examples

#### Example 1: Sending Messages to Server
```javascript
import signalRService from '@/service/signalRService';

// Send a node update to the server
async function updateNode(nodeData) {
  try {
    await signalRService.invoke('UpdateNode', nodeData);
    console.log('Node updated successfully');
  } catch (error) {
    console.error('Failed to update node:', error);
  }
}
```

#### Example 2: Listening to Server Events
```javascript
import signalRService from '@/service/signalRService';

// Listen for node updates from server
signalRService.on('NodeUpdated', (nodeData) => {
  console.log('Node updated from server:', nodeData);
  // Update your node in the editor
});

// Listen for editor commands from server
signalRService.on('EditorCommand', (command) => {
  console.log('Editor command received:', command);
  // Execute the command
});
```

#### Example 3: Handling Connection Status
```javascript
import signalRService from '@/service/signalRService';

// Subscribe to connection status changes
const unsubscribe = signalRService.onStatusChange((status) => {
  console.log('Connection status changed:', status);
  
  if (status === 'connected') {
    // Connection established, sync data
  } else if (status === 'disconnected') {
    // Connection lost, show offline mode
  }
});

// Clean up when component unmounts
onBeforeUnmount(() => {
  unsubscribe();
});
```

#### Example 4: Integration with EventBus
The SignalR service is integrated with the application's event bus:

```javascript
import { listenEvent, EVENT_TYPES } from '@/rete/eventBus';

// Listen for SignalR status changes via event bus
listenEvent(EVENT_TYPES.SIGNALR_STATUS_CHANGED, (status) => {
  console.log('SignalR status:', status);
});

// Listen for node updates from server via event bus
listenEvent(EVENT_TYPES.NODE_UPDATED_FROM_SERVER, (nodeData) => {
  console.log('Node updated from server:', nodeData);
});

// Listen for editor commands from server via event bus
listenEvent(EVENT_TYPES.EDITOR_COMMAND_FROM_SERVER, (command) => {
  console.log('Editor command from server:', command);
});
```

### Event Types

The following event types are available in `src/rete/eventBus.js`:

- `SIGNALR_STATUS_CHANGED`: Fired when connection status changes
- `NODE_UPDATED_FROM_SERVER`: Fired when a node is updated from the server
- `EDITOR_COMMAND_FROM_SERVER`: Fired when an editor command is received from the server

### Automatic Reconnection

The SignalR connection automatically attempts to reconnect if the connection is lost:
- Uses exponential backoff strategy
- Initial retry: immediately
- Subsequent retries: 2s, 10s, 30s, then 60s intervals
- Continues retrying until connection is re-established

### Testing Without a Backend

The SignalR service is designed to fail gracefully. If no backend is available:
- Connection status will show as "disconnected"
- No errors will be thrown
- Application continues to work normally
- To enable: Uncomment the initialization line in `MainEditor.vue`:

```javascript
// In MainEditor.vue onMounted()
await signalRService.initialize(); // SignalR is initialized automatically
```

### Security Considerations

1. **Authentication**: Add authentication tokens to the SignalR connection:
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl(hubUrl, {
    accessTokenFactory: () => getAuthToken()
  })
  .build();
```

2. **CORS**: Configure CORS on your server to allow connections from your domain

3. **Authorization**: Implement authorization in your hub methods to verify user permissions

---

## Troubleshooting

### PWA Installation Issues
- **Install prompt doesn't appear**: Ensure HTTPS is enabled (required for PWA)
- **Service worker not registering**: Check browser console for errors
- **Cache not updating**: Increment `CACHE_NAME` version in service-worker.js

### SignalR Connection Issues
- **Connection fails**: Verify hub URL is correct and server is running
- **Reconnection loops**: Check server logs for errors
- **Messages not received**: Ensure event names match between client and server

### Performance Considerations
- Service worker caching improves load times
- SignalR uses WebSockets for efficient real-time communication
- Connection state is managed efficiently to minimize battery usage on mobile devices

---

## Future Enhancements

Potential improvements for future versions:
1. Add offline data sync when connection is restored
2. Implement conflict resolution for concurrent edits
3. Add presence detection (show who else is viewing/editing)
4. Expand caching strategy for better offline experience
5. Add push notifications for important updates
