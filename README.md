# NodPT

Visual AI-assisted workflow editor built with a modern microservices architecture using Vue 3, Rete.js, Bootstrap 5, .NET 8, and Docker.

## 🌟 Our Vision

### The Problem We're Solving
AI tools today are scattered, siloed, and limited. You're forced to jump between different platforms, copy-paste results, and manually coordinate tasks that should flow naturally together. We believe AI should work the way humans do—as a collaborative team where each member brings unique expertise and they all communicate seamlessly.

### Our Mission
NodPT is building the future of AI collaboration—a visual, node-based platform where AI agents work together as an intelligent team. We're creating an open-source ecosystem that democratizes access to multi-agent AI workflows, making it easy for anyone to orchestrate complex tasks through simple visual connections.

## 🏗️ Microservices Architecture

NodPT is built using a modern microservices architecture with Docker containers. Each service is independently deployable and scalable.

### Architecture Overview

```
┌─────────────┐
│   Frontend  │ (Vue 3 + Rete.js + Bootstrap 5)
│   Port 8443 │
└──────┬──────┘
       │ HTTP/REST
       ▼
┌─────────────┐         ┌─────────────┐
│   WebAPI    │◄───────►│    Redis    │
│   Port 8846 │  Write  │   Port 6379 │
└─────────────┘         └──────┬──────┘
                               │
                               │ Read/Write
                               │
       ┌───────────────────────-
       │                                               
       ▼                                               
┌─────────────┐         ┌─────────────┐         ┌─────────────┐
│  Executor   │◄───────►│     AI      │         │   SignalR   │WebSocket
│             │ Request │  (Ollama)   │         │   Port 8848 │--------> [ Frontend ]
└─────────────┘         └─────────────┘         └──────┬──────┘
       │                                               │
       │ Write Results                                 │ Real-time
       ▼                                               │
┌─────────────┐                                        │
│    Redis    │                                        │
└──────┬──────┘                                        │
       │                                               │
       │ Read Updates                                  │
       └───────────────────────────────────────────────│
                                                         
```

### Data Flow

1. **Frontend → WebAPI**: User interacts with visual editor, sends HTTP requests to WebAPI
2. **WebAPI → Redis**: API injects job data and tasks into Redis streams
3. **Executor ← Redis**: Executor pulls job data from Redis streams
4. **Executor → AI**: Executor requests AI processing (via Ollama)
5. **AI → Executor**: AI responds with processed results
6. **Executor → Redis**: Executor injects AI response data back into Redis streams
7. **SignalR ← Redis**: SignalR pulls update data from Redis streams
8. **SignalR → Frontend**: SignalR sends real-time updates to Frontend via WebSocket

### Technology Stack

| Service      | Technology                           | Description                                        | Port  |
| ------------ | ------------------------------------ | -------------------------------------------------- | ----- |
| **Frontend** | Vue.js 3, Rete.js, Bootstrap 5, Vite | Visual workflow editor with node-based interface   | 8443  |
| **WebAPI**   | .NET 8, ASP.NET Core, DevExpress XPO | RESTful API for data management and authentication | 8846  |
| **SignalR**  | .NET 8, SignalR Core                 | Real-time communication hub                        | 8848  |
| **Executor** | .NET 8, Agent Service                | Background job processor                           | N/A   |
| **AI**       | Ollama                               | LLM inference engine (requires GPU)                | 11434 |
| **Redis**    | Redis 7 Alpine                       | Message broker and caching                         | 6379  |
| **Data**     | DevExpress XPO, MySQL/MariaDB        | Data access layer and ORM                          | N/A   |

## 🚀 Quick Start with Docker

### Prerequisites

- Docker and Docker Compose installed
- NVIDIA GPU (for AI service)
- At least 16GB RAM recommended

### Environment Setup

Each service requires environment configuration. Create the following files:

```bash
# Create environment directory
mkdir -p /home/runner_user/envs

# Frontend environment
/home/runner_user/envs/frontend.env

# Backend environment (shared by WebAPI, SignalR, Executor)
/home/runner_user/envs/backend.env
```

See individual service README files for specific environment variable requirements.

### Running All Services

```bash
# Create external networks
docker network create frontend_network
docker network create backend_network

# Start Redis (required by other services)
cd Redis
docker-compose up -d

# Start AI service
cd ../AI
docker-compose up -d

# Start WebAPI
cd ../WebAPI
docker-compose up -d

# Start SignalR
cd ../SignalR
docker-compose up -d

# Start Executor
cd ../Executor
docker-compose up -d

# Start Frontend
cd ../Frontend
docker-compose up -d
```

Access the application at: `http://localhost:{your-port}`

## 📁 Project Structure

```
NodPT/
├── Frontend/          # Vue.js 3 visual editor (Port 8443)
│   ├── src/          # Source code
│   ├── Dockerfile    # Frontend container
│   └── README.md     # Frontend documentation
├── WebAPI/           # .NET 8 REST API (Port 8846)
│   ├── src/          # API source code
│   ├── Dockerfile    # API container
│   └── README.md     # API documentation
├── SignalR/          # .NET 8 SignalR hub (Port 8848)
│   ├── src/          # SignalR source code
│   ├── Dockerfile    # SignalR container
│   └── README.md     # SignalR documentation
├── Executor/         # .NET 8 background agent
│   ├── src/          # Executor source code
│   ├── Dockerfile    # Executor container
│   └── README.md     # Executor documentation
├── AI/               # Ollama AI service (Port 11434)
│   ├── docker-compose.yml
│   └── README.md     # AI service documentation
├── Redis/            # Redis message broker (Port 6379)
│   ├── src/          # Redis configuration
│   ├── Dockerfile    # Redis container
│   └── README.md     # Redis documentation
└── Data/             # Shared data layer (DevExpress XPO)
    ├── src/          # Data models and repositories
    └── README.md     # Data layer documentation
```

## 🧠 Memory Architecture

NodPT implements a rolling conversational memory system that gives AI models persistent context across conversations. Each node maintains its own memory, enabling coherent long-term interactions.

### Overview

Each `nodeId` has:
- **Rolling Summary**: A compressed representation of the conversation history stored in Redis (hot) and MariaDB (persistent)
- **Short-term History Buffer**: The last few raw messages stored in Redis for tone and recent details
- **Persistent Storage**: Summary backed up to MariaDB via DevExpress XPO

This architecture simulates "chat memory" over stateless models like Ollama and TensorRT.

### Memory Flow

```
User Message → Executor
                │
                ├─→ Load Summary (Redis → MariaDB fallback)
                ├─→ Load History (Redis)
                ├─→ Build Context (Summary + History + Prompts + Message)
                ├─→ Send to Main Model (Ollama)
                │
                ├─→ Add User Message to History
                ├─→ Rolling Summarization (User Message)
                │
AI Response ←───┘
                │
                ├─→ Add AI Message to History
                └─→ Rolling Summarization (AI Message)
```

### Services

#### MemoryService (Data Project)

Central coordinator for all memory operations:

- **LoadSummaryAsync**: Loads summary for a node
  1. Check Redis for cached summary
  2. If not found, load from MariaDB via XPO
  3. Cache in Redis for fast access
  4. If not found anywhere, initialize empty summary

- **RollingSummarizeAsync**: Triggers rolling summarization after each message
  1. Load existing summary
  2. Call SummarizationService
  3. Update Redis with new summary
  4. Persist to MariaDB

- **AddToHistoryAsync**: Manages short-term message history
  - Adds messages to Redis list
  - Trims to configured limit (default: 3 messages)

- **GetHistoryAsync**: Retrieves recent messages for context

#### SummarizationService (Data Project)

Handles the actual LLM call for summarization:

- Reads configuration from appsettings (base URL, model, timeout)
- Builds summarization prompts optimized for user vs AI messages
- Calls Ollama summarizer endpoint (separate from main chat model)
- Returns merged summary text
- Falls back gracefully on errors

### Executor Flow

#### When a User Message Arrives

1. Executor receives nodeId and user message
2. Load summary via MemoryService
3. Load recent history from Redis
4. Build context: system prompts + summary + history + user message
5. Send to main chat model (Ollama/TensorRT)
6. Add user message to history
7. Trigger rolling summarization for user message

#### When AI Responds

1. Executor receives AI response
2. Add AI message to history
3. Trigger rolling summarization for AI message
4. Publish result to SignalR for frontend

### Rolling Summarization Logic

Summarization is optimized based on message role:

**For User Messages:**
- Captures goals, tasks, questions
- Preserves constraints (time, budget, technology)
- Records preferences (style, tone, priorities)
- Extracts key contextual facts

**For AI Messages:**
- Captures decisions and commitments
- Records frameworks and plans
- Preserves clarifications
- Extracts actionable conclusions

### Configuration

Add to `appsettings.json`:

```json
{
  "Summarization": {
    "BaseUrl": "http://localhost:11434/api/generate",
    "Model": "llama3.2:1b",
    "TimeoutSeconds": 60,
    "MaxSummaryLength": 2000
  },
  "Memory": {
    "HistoryLimit": 3,
    "SummaryKeyPrefix": "summary",
    "HistoryKeyPrefix": "history"
  }
}
```

Environment variable overrides:
- `SUMMARIZATION_BASE_URL`
- `SUMMARIZATION_MODEL`
- `SUMMARIZATION_TIMEOUT_SECONDS`
- `SUMMARIZATION_MAX_LENGTH`
- `MEMORY_HISTORY_LIMIT`
- `MEMORY_SUMMARY_KEY_PREFIX`
- `MEMORY_HISTORY_KEY_PREFIX`

### Redis Key Strategy

- Summary: `summary:{nodeId}` (string)
- History: `history:{nodeId}` (list)

### Operational Notes

1. **Summarization runs in background** - doesn't block the chat flow, allowing for smooth user experience
2. **Redis can be flushed** without data loss - summaries reload from MariaDB on demand
3. **Separate summarizer model** recommended (smaller, faster like llama3.2:1b)
4. **Memory is per-node**, enabling different contexts for different workflow nodes

## 🤝 Join the Movement

We're an open-source project driven by the belief that powerful AI tools should be accessible to everyone. Whether you're a developer, designer, writer, or domain expert—your contribution matters.

### How You Can Contribute

- **💻 Code Contributions**: Help build features, fix bugs, or improve performance
- **🎨 Design & UX**: Enhance the user experience and visual design
- **📚 Documentation**: Write guides, tutorials, and API documentation
- **💬 Community**: Share ideas, help others, and spread the word

Get started by checking out our [Issues](https://github.com/NodPT/NodPT/issues) or reach out to the community!

### Development Guidelines

1. Read the README in each service directory before contributing
2. Follow the existing code style and conventions
3. Write tests for new features
4. Update documentation for any changes
5. Submit pull requests with clear descriptions

## ✨ Features

- **Visual Node Editor**: Intuitive drag-and-drop interface for creating workflows
- **Progressive Web App (PWA)**: Install the app on any device for offline access
- **Real-time Collaboration**: SignalR integration for live updates and collaboration
- **AI-Powered Tools**: Integrated AI assistance for workflow optimization
- **Microservices Architecture**: Scalable and maintainable service-based design
- **Docker Support**: Easy deployment with Docker and Docker Compose
- **Redis Streams**: Efficient message passing between services
- **Firebase Authentication**: Secure user authentication and authorization

## 🌐 Browser Support

- Chrome (recommended)
- Edge
- Safari
- Firefox

PWA features require HTTPS in production.

## 📄 License

See LICENSE file for details.

