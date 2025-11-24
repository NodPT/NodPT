# NodPT.SignalR

## ⚠️ OBSOLETE - DO NOT USE ⚠️

**This project has been marked as OBSOLETE and is no longer actively used in the NodPT architecture.**

### Migration Notice

The SignalR Hub functionality has been **consolidated into the WebAPI project**. All real-time communication now happens through:

- **WebAPI SignalR Hub**: The SignalR Hub is now hosted directly in the WebAPI service
- **Unified Redis Streams**: WebAPI uses the shared `RedisService` from the Data project to listen to `signalr:updates` stream
- **Single Service**: Reduces deployment complexity by eliminating a separate SignalR service

### Why This Change?

1. **Simplified Architecture**: One less service to deploy and manage
2. **Unified Redis Communication**: All services use the same shared `RedisService` with Redis Streams
3. **Better Resource Utilization**: WebAPI can handle both HTTP/REST and WebSocket connections
4. **Easier Development**: Developers only need to run WebAPI for full functionality

### For New Developers

**Do not use this project.** Instead:
- The SignalR Hub is in `/WebAPI/src/Hubs/NodptHub.cs`
- Real-time updates are handled by `/WebAPI/src/BackgroundServices/SignalRUpdateListener.cs`
- See WebAPI README for updated architecture documentation

---

## Legacy Documentation (For Historical Reference Only)

A SignalR hub server built with .NET 8 and C# that listens to Redis streams and delivers real-time updates to frontend clients.

## Architecture

The SignalR hub operates as a **listener and router only**:

- **Listens** to Redis stream `signalr:updates` for messages from the Executor
- **Routes** messages to the appropriate frontend clients based on routing criteria
- **Delivers** real-time updates via WebSocket connections
- **No task submission** - SignalR does not trigger or submit any tasks

Communication flow:
1. Executor writes task results to Redis stream `signalr:updates`
2. SignalR listener reads messages from the stream
3. SignalR routes messages to connected frontend clients based on userId, workflowGroup, or clientConnectionId

## Features

- **Redis Stream Listener**: Background service that continuously reads from Redis stream
- **Smart Message Routing**: Routes messages by clientConnectionId, workflowGroup, or userId
- **NodptHub**: SignalR hub accessible at `/nodpt_hub` endpoint
- **Firebase Authentication**: Secure token-based authentication for frontend clients
- **Monitor Page**: Real-time monitoring interface for all SignalR communications
- **Group Messaging**: Send messages to all clients or specific groups
- Built on .NET 9.0
- Real-time bidirectional communication

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Redis server (for stream communication)
- Firebase project (for authentication)

### Firebase Configuration

For production use, you need to configure Firebase credentials:

1. Create a Firebase project at [Firebase Console](https://console.firebase.google.com/)
2. Download your service account key JSON file
3. Set the `GOOGLE_APPLICATION_CREDENTIALS` environment variable:
   ```bash
   export GOOGLE_APPLICATION_CREDENTIALS="/path/to/your/serviceAccountKey.json"
   ```

Or configure in `appsettings.json`:
```json
{
  "Firebase": {
    "ProjectId": "your-project-id"
  }
}
```

### Redis Configuration

Configure Redis connection in `appsettings.json`:

```json
{
  "Redis": {
    "ConnectionString": "nodpt-redis:6379"
  }
}
```

For development, the connection string defaults to `localhost:6379` in `appsettings.Development.json`.

### Running the Application

```bash
cd NodPT.SignalR
dotnet run
```

The application will start and listen on `http://localhost:5288` by default.

**Note:** Ensure Redis is running and accessible before starting the application. The Redis stream listener will attempt to connect on startup.

## Monitor Page

Access the monitoring interface at `http://localhost:5288/monitor.html`

The monitor page acts as a **master client** that receives all messages sent through the SignalR hub. It provides:

- Real-time message display
- Connection status monitoring
- Message statistics
- Easy connect/disconnect controls

### Using the Monitor Page

1. Navigate to `http://localhost:5288/monitor.html`
2. Enter the server URL (default: `http://localhost:5288/nodpt_hub`)
3. Leave the access token empty to connect as an executor (no authentication required)
4. Click "Connect as Master"
5. All messages sent through the hub will appear in real-time

## Authentication

### For Frontend Clients

Frontend clients must authenticate using Firebase ID tokens:

#### JavaScript Example

```javascript
// Obtain Firebase ID token from Firebase Auth
const user = firebase.auth().currentUser;
const token = await user.getIdToken();

// Connect to SignalR with the token
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5288/nodpt_hub", {
        accessTokenFactory: () => token
    })
    .build();

await connection.start();
```

#### C# Client Example

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5288/nodpt_hub", options =>
    {
        options.AccessTokenProvider = async () =>
        {
            // Get your Firebase ID token
            return await GetFirebaseIdTokenAsync();
        };
    })
    .Build();

await connection.StartAsync();
```

### For Executor Client (Background Service)

The executor client can connect without Firebase authentication by using a special client type parameter:

#### JavaScript Example

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5288/nodpt_hub?clientType=executor-client")
    .build();

await connection.start();
```

#### C# Client Example

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5288/nodpt_hub?clientType=executor-client")
    .Build();

await connection.StartAsync();
```

## Hub API

### Hub Endpoint

- **URL**: `/nodpt_hub`
- **Authentication**: Required (Firebase ID token or executor client type)

### Hub Methods

#### SendMessage
Sends a message to all clients or a specific group.

```javascript
await connection.invoke("SendMessage", user, message, targetGroup);
```

**Parameters:**
- `user` (string): Name of the user sending the message
- `message` (string): Message content
- `targetGroup` (string, optional): Target group name. If null, sends to all clients.

#### JoinGroup
Joins a specific group.

```javascript
await connection.invoke("JoinGroup", groupName);
```

**Parameters:**
- `groupName` (string): Name of the group to join

#### LeaveGroup
Leaves a specific group.

```javascript
await connection.invoke("LeaveGroup", groupName);
```

**Parameters:**
- `groupName` (string): Name of the group to leave

#### JoinMasterGroup
Joins the master monitoring group to receive all messages.

```javascript
await connection.invoke("JoinMasterGroup");
```

### Client Events

Clients should listen to these events:

#### ReceiveNodeUpdate
**Primary event** - Triggered when a node result is received from Redis stream.

```javascript
connection.on("ReceiveNodeUpdate", (data) => {
    console.log(`Node Update: ${data.nodeId}`);
    console.log(`Type: ${data.type}`);
    console.log(`Payload: ${data.payload}`);
    console.log(`Project: ${data.projectId}, User: ${data.userId}`);
    console.log(`Timestamp: ${data.timestamp}`);
});
```

**Data structure:**
- `messageId` (string): Unique message identifier
- `nodeId` (string): Node that generated the update
- `projectId` (string): Associated project ID
- `userId` (string): User ID for routing
- `type` (string): Message type (e.g., "result")
- `payload` (string): The actual data/result
- `timestamp` (DateTime): When the message was created
- `workflowGroup` (string): Workflow group for routing

#### ReceiveMessage
Triggered when a message is received via SendMessage method.

```javascript
connection.on("ReceiveMessage", (data) => {
    console.log(`${data.User}: ${data.Message}`);
    console.log(`Group: ${data.Group}, Timestamp: ${data.Timestamp}`);
});
```

#### MonitorMessage
Triggered for master clients when monitoring messages.

```javascript
connection.on("MonitorMessage", (data) => {
    console.log("Monitored:", data);
});
```

#### JoinedGroup
Confirmation of joining a group.

```javascript
connection.on("JoinedGroup", (groupName) => {
    console.log(`Joined group: ${groupName}`);
});
```

## Project Structure

```
NodPT.SignalR/
├── Authentication/
│   └── FirebaseAuthenticationHandler.cs  # Firebase authentication handler
├── Hubs/
│   └── NodptHub.cs                       # SignalR hub implementation
├── Models/
│   └── NodeMessage.cs                    # Message model for Redis stream data
├── Services/
│   └── RedisStreamListener.cs            # Background service that listens to Redis
├── wwwroot/
│   ├── monitor.html                      # Real-time monitoring page
│   └── test-client.html                  # Test client page
├── Program.cs                             # Application entry point and configuration
├── NodPT.SignalR.csproj                  # Project file
└── appsettings.json                      # Configuration settings
```

## Redis Stream Message Format

The Executor writes messages to the `signalr:updates` stream with the following fields:

- `MessageId`: Unique identifier for the message
- `NodeId`: ID of the node that produced the result
- `ProjectId`: Project identifier
- `UserId`: User identifier for routing
- `ClientConnectionId`: (Optional) Specific client connection to target
- `WorkflowGroup`: (Optional) Workflow group name for routing
- `Type`: Message type (e.g., "result")
- `Payload`: The actual data to deliver to the client
- `Timestamp`: ISO 8601 timestamp

### Routing Logic

Messages are routed with the following priority:

1. **ClientConnectionId** - If specified, message goes to that specific connection
2. **WorkflowGroup** - If specified, message goes to all clients in that group
3. **UserId** - Message goes to all clients connected with that user ID (via `user:{userId}` group)

## Security Notes

1. **Firebase Credentials**: Never commit Firebase service account keys to version control
2. **CORS Configuration**: Configure allowed origins in `appsettings.json` for production
3. **Token Validation**: All regular clients must provide valid Firebase ID tokens
4. **Executor Access**: The executor client type should be protected and only used by trusted background services
5. **Environment-Based Security**: Firebase fallback authentication is only available in development mode

## Development vs Production

### Development
- Firebase credentials can be omitted (will run without validation)
- CORS allows all origins automatically
- Uses default localhost URLs
- Firebase authentication bypassed for testing

### Production
- **Must** configure Firebase credentials via `GOOGLE_APPLICATION_CREDENTIALS` environment variable
- **Must** configure CORS allowed origins in `appsettings.json`:
  ```json
  {
    "Cors": {
      "AllowedOrigins": [
        "https://yourdomain.com",
        "https://www.yourdomain.com"
      ]
    }
  }
  ```
- Firebase authentication is enforced (no fallback)
- Use HTTPS for all connections
- Consider implementing rate limiting and additional security measures