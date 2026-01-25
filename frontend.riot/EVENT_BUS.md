# Event Bus Plugin for Riot.js

This project now includes an event bus plugin using `@riotjs/observable` and `riot.install` to enable communication between Riot.js components without direct coupling.

## Overview

The event bus plugin provides a global event system that allows components to:
- Trigger events with data
- Listen to events from other components
- Communicate without direct parent-child relationships
- Maintain loose coupling between components

## Installation

The event bus is already installed and configured in this project. It's automatically available in all Riot.js components via `this.bus`.

## Usage

### In Components

#### Triggering Events

```javascript
export default {
  sendMessage() {
    this.bus.trigger('my-event', { 
      message: 'Hello World',
      timestamp: Date.now()
    });
  }
}
```

#### Listening to Events

```javascript
export default {
  onMounted() {
    // Add event listener
    this.bus.on('my-event', this.handleEvent);
  },
  onBeforeUnmount() {
    // IMPORTANT: Always remove listeners to prevent memory leaks
    this.bus.off('my-event', this.handleEvent);
  },
  handleEvent(data) {
    console.log('Event received:', data);
  }
}
```

#### Listening Once

```javascript
export default {
  onMounted() {
    // Listen only once, then automatically remove
    this.bus.one('my-event', (data) => {
      console.log('This will only fire once:', data);
    });
  }
}
```

### Outside Components

You can also use the event bus outside of components by importing it directly:

```javascript
import { bus } from './services/eventBusPlugin.js';

bus.trigger('my-event', { data: 'value' });

bus.on('my-event', (data) => {
  console.log('Event received:', data);
});
```

## API

### Methods

- **`trigger(eventName, data)`** - Trigger an event with optional data
- **`on(eventName, callback)`** - Listen to an event
- **`one(eventName, callback)`** - Listen to an event once, then automatically remove
- **`off(eventName, callback)`** - Remove an event listener
- **`off(eventName)`** - Remove all listeners for an event
- **`off('*')`** - Remove all listeners for all events

### Example Events

```javascript
// Simple event
this.bus.trigger('user-login');

// Event with data
this.bus.trigger('user-login', { userId: 123, username: 'john' });

// Event with complex data
this.bus.trigger('data-updated', {
  type: 'project',
  id: 456,
  changes: { name: 'New Name' }
});
```

## Best Practices

1. **Always clean up listeners** - Use `off()` in `onBeforeUnmount()` to prevent memory leaks
2. **Use descriptive event names** - e.g., `'user:login'`, `'project:updated'`, `'data:loaded'`
3. **Document your events** - Keep track of what events exist and what data they pass
4. **Use namespacing** - Prefix related events (e.g., `'user:*'`, `'data:*'`)
5. **Don't overuse** - Use props and slots for parent-child communication

## Demo

Visit `/event-bus-demo` in the application to see a working example of two components communicating via the event bus.

## Implementation Details

### Files

- **`src/services/bus.js`** - Core event bus using @riotjs/observable
- **`src/services/eventBusPlugin.js`** - Riot.js plugin wrapper
- **`src/index.js`** - Plugin installation via `riot.install()`

### How it Works

1. The event bus is created using `@riotjs/observable`:
   ```javascript
   import observable from '@riotjs/observable';
   const bus = observable({});
   ```

2. A Riot.js plugin injects it into all components:
   ```javascript
   export default function eventBusPlugin(component) {
     component.bus = bus;
   }
   ```

3. The plugin is installed at app startup:
   ```javascript
   import { install } from 'riot';
   import eventBusPlugin from './services/eventBusPlugin.js';
   
   install(eventBusPlugin);
   ```

## References

- [@riotjs/observable on npm](https://www.npmjs.com/package/@riotjs/observable)
- [Riot.js Documentation](https://riot.js.org/)
- [Riot.js API - riot.install](https://riot.js.org/api/)
