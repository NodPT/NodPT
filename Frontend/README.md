# NodPT

Visual AI-assisted workflow editor built with Vue 3, Rete.js, and Bootstrap 5.

## 🌟 Our Vision

### The Problem We're Solving
AI tools today are scattered, siloed, and limited. You're forced to jump between different platforms, copy-paste results, and manually coordinate tasks that should flow naturally together. We believe AI should work the way humans do—as a collaborative team where each member brings unique expertise and they all communicate seamlessly.

### Our Mission
NodPT is building the future of AI collaboration—a visual, node-based platform where AI agents work together as an intelligent team. We're creating an open-source ecosystem that democratizes access to multi-agent AI workflows, making it easy for anyone to orchestrate complex tasks through simple visual connections.

## 🤝 Join the Movement

We're an open-source project driven by the belief that powerful AI tools should be accessible to everyone. Whether you're a developer, designer, writer, or domain expert—your contribution matters.

### How You Can Contribute

- **💻 Code Contributions**: Help build features, fix bugs, or improve performance
- **🎨 Design & UX**: Enhance the user experience and visual design
- **📚 Documentation**: Write guides, tutorials, and API documentation
- **💬 Community**: Share ideas, help others, and spread the word

Get started by checking out our [Issues](https://github.com/rongxike/NodPT.FrontEnd/issues) or reach out to the community!

## Features

- **Visual Node Editor**: Intuitive drag-and-drop interface for creating workflows
- **Progressive Web App (PWA)**: Install the app on any device for offline access
- **Real-time Collaboration**: SignalR integration for live updates and collaboration
- **AI-Powered Tools**: Integrated AI assistance for workflow optimization

## New Features

### Progressive Web App (PWA)
- ✅ Add to Home Screen on mobile and desktop
- ✅ Offline support with service worker caching
- ✅ Automatic cache updates when new versions are available
- ✅ App-like experience with standalone display mode

### SignalR Integration
- ✅ Real-time communication with the server
- ✅ Live connection status indicator in the bottom bar
- ✅ Automatic reconnection with exponential backoff
- ✅ Event-based architecture for easy integration
- ✅ Support for node updates and editor commands from server

## Getting Started

### Installation

```bash
npm install
```

### Development

```bash
npm run dev
```

### Build

```bash
npm run build
```

### Preview Production Build

```bash
npm run preview
```

## Documentation

- [PWA and SignalR Features](./docs/PWA_AND_SIGNALR.md) - Detailed documentation for PWA and SignalR features

## Configuration

### PWA Configuration
- Edit `public/manifest.json` to customize app name, icons, and display settings
- Update `public/service-worker.js` to configure caching strategy

### SignalR Configuration
- Update hub URL in `src/views/MainEditor.vue`
- Implement corresponding SignalR hub on your backend server
- See [documentation](./docs/PWA_AND_SIGNALR.md) for detailed setup instructions

## Project Structure

```
src/
├── components/       # Vue components
│   ├── TopBar.vue
│   ├── Footer.vue    # Includes SignalR connection status
│   ├── LeftPanel.vue
│   └── RightPanel.vue
├── views/            # Page components
│   └── MainEditor.vue # Main editor with SignalR integration
├── service/          # Service layer
│   └── signalRService.js # SignalR service
├── rete/             # Rete.js configuration
│   └── eventBus.js   # Event bus with SignalR events
└── main.js           # Service worker registration
```

## Browser Support

- Chrome (recommended)
- Edge
- Safari
- Firefox

PWA features require HTTPS in production.

## License

See LICENSE file for details.

