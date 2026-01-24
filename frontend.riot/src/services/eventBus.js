// Event bus service using @riotjs/observable
// Provides a global event bus for triggering and listening to events across Riot components

import observable from '@riotjs/observable';

/**
 * Create a global event bus using @riotjs/observable
 * This allows components to communicate without direct coupling
 * 
 * Usage in components:
 *   // Trigger an event
 *   this.$eventBus.trigger('custom-event', { data: 'value' });
 * 
 *   // Listen to an event
 *   this.$eventBus.on('custom-event', (data) => {
 *     console.log('Event received:', data);
 *   });
 * 
 *   // Listen once
 *   this.$eventBus.one('custom-event', (data) => {
 *     console.log('Event received once:', data);
 *   });
 * 
 *   // Remove listener
 *   this.$eventBus.off('custom-event', handler);
 */
const eventBus = observable({});

// Export the event bus for direct use
export default eventBus;
