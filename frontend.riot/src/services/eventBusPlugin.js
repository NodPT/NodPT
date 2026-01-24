// Event Bus plugin for Riot.js
// Uses riot.install to inject event bus into all components

import eventBus from './eventBus.js';

/**
 * Event Bus Plugin for Riot.js
 * 
 * This plugin uses riot.install to add the event bus to all Riot components.
 * Once installed, all components will have access to this.$eventBus
 * 
 * Usage:
 *   import { install } from 'riot';
 *   import eventBusPlugin from './services/eventBusPlugin.js';
 *   
 *   // Install the plugin before mounting components
 *   install(eventBusPlugin);
 * 
 * In components:
 *   // Trigger events
 *   this.$eventBus.trigger('my-event', data);
 *   
 *   // Listen to events
 *   this.$eventBus.on('my-event', handler);
 *   
 *   // Remove listeners (important for cleanup!)
 *   this.$eventBus.off('my-event', handler);
 * 
 * Best practices:
 *   - Always remove event listeners in onBeforeUnmount to prevent memory leaks
 *   - Use descriptive event names (e.g., 'user:login', 'data:updated')
 *   - Document the data structure passed with each event
 */
export default function eventBusPlugin(component) {
  // Add $eventBus to each component instance
  // riot.install calls this function for each component instance
  component.$eventBus = eventBus;
}

// Also export the event bus for direct use outside components
export { eventBus };
