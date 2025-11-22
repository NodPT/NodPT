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
│   Port 8846 │  Write  │   Port 8847 │
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

| Service | Technology | Description | Port |
|---------|-----------|-------------|------|
| **Frontend** | Vue.js 3, Rete.js, Bootstrap 5, Vite | Visual workflow editor with node-based interface | 8443 |
| **WebAPI** | .NET 8, ASP.NET Core, DevExpress XPO | RESTful API for data management and authentication | 8846 |
| **SignalR** | .NET 8, SignalR Core | Real-time communication hub | 8848 |
| **Executor** | .NET 8, Worker Service | Background job processor | N/A |
| **AI** | Ollama | LLM inference engine (requires GPU) | 11434 |
| **Redis** | Redis 7 Alpine | Message broker and caching | 8847 |
| **Data** | DevExpress XPO, MySQL/MariaDB | Data access layer and ORM | N/A |

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
├── Executor/         # .NET 8 background worker
│   ├── src/          # Executor source code
│   ├── Dockerfile    # Executor container
│   └── README.md     # Executor documentation
├── AI/               # Ollama AI service (Port 11434)
│   ├── docker-compose.yml
│   └── README.md     # AI service documentation
├── Redis/            # Redis message broker (Port 8847)
│   ├── src/          # Redis configuration
│   ├── Dockerfile    # Redis container
│   └── README.md     # Redis documentation
└── Data/             # Shared data layer (DevExpress XPO)
    ├── src/          # Data models and repositories
    └── README.md     # Data layer documentation
```

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

