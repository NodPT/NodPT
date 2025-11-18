// eventBus.js
import Emitter from 'tiny-emitter';

// Create an event bus instance
export const eventBus = new Emitter();

// Event names constants to avoid typos across the application
export const EVENT_TYPES = {
	// Node events
	NODE_ADDED: 'node:added',
	NODE_SELECTED: 'node:selected',
	NODE_DELETED: 'node:deleted',
	DELETE_NODE: 'node:delete',
	GROUP_NODES: 'node:group',
	UNGROUP_NODES: 'node:ungroup',
	NODE_REGENERATE: 'node:regenerate',
	NODE_STATUS_CHANGE: 'node:status-change',
	NODE_REFRESH: 'node:refresh',
	LOCK_NODE: 'node:lock',
	UNLOCK_NODE: 'node:unlock',

	// Editor events
	EDITOR_READY: 'editor:ready',
	ARRANGE_NODES: 'editor:arrange-nodes',
	ZOOM_FIT: 'editor:zoom-fit',
	UNDO: 'editor:undo',
	REDO: 'editor:redo',

	// UI events
	TOGGLE_MINIMAP: 'ui:toggle-minimap',
	TOGGLE_RIGHT_PANEL: 'ui:toggle-right-panel',

	// Project events
        PROJECT_ACTION: 'project:action',
        PROJECT_NAME_UPDATE: 'project:name-update',
        PROJECT_CONTEXT_CHANGED: 'project:context-changed',
        OPEN_NEW_PROJECT_MODAL: 'project:open-new-project-modal',

	// Node action events
	NODE_ACTION: 'node:action',

	// AI events
	AI_ACTION: 'ai:action',

	// User action events
	USER_ACTION: 'user:action',

	// Search events
	SEARCH_NODES: 'search:nodes',
	SEARCH_NEXT: 'search:next',
	SEARCH_PREVIOUS: 'search:previous',
	SEARCH_CLEAR: 'search:clear',
	SEARCH_FOCUS_NODE: 'search:focus-node',

	// SignalR events
	SIGNALR_STATUS_CHANGED: 'signalr:status-changed',
	SIGNALR_TOGGLE_CONNECTION: 'signalr:toggle-connection',
	NODE_UPDATED_FROM_SERVER: 'signalr:node-updated',
	EDITOR_COMMAND_FROM_SERVER: 'signalr:editor-command',

	// Auth lifecycle events
	AUTH_SIGNED_IN: 'auth:signed-in',
	AUTH_SIGNED_OUT: 'auth:signed-out',
	AUTH_REQUIRES_RELOGIN: 'auth:requires-relogin',
};

// Helper functions for event handling using trigger style
export const triggerEvent = (eventName, payload) => {
	eventBus.emit(eventName, payload);
};

export const listenEvent = (eventName, callback) => {
	eventBus.on(eventName, callback);
	// Return unsubscribe function for cleanup
	return () => eventBus.off(eventName, callback);
};

export const removeListener = (eventName, callback) => {
	eventBus.off(eventName, callback);
};
