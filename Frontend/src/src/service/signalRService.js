import * as signalR from '@microsoft/signalr';
import { triggerEvent, listenEvent, EVENT_TYPES } from '../rete/eventBus';
import { getToken } from './tokenStorage';

class SignalRService {
  constructor() {
    this.connection = null;
    this.connectionStatus = 'disconnected'; // disconnected, connecting, connected, reconnecting
    this.listeners = [];
    this.authListenerCleanups = [];
    this.isAuthenticated = false;
    this.retryCount = 0;
    this.maxRetries = 1;

    // Setup auth lifecycle listeners
    this.setupAuthLifecycle();
  }

  /**
   * Setup authentication lifecycle listeners
   */
  setupAuthLifecycle() {
    // Track authentication state but don't auto-start SignalR
    // Connection will be started explicitly when navigating to /editor
    this.authListenerCleanups.push(
      listenEvent(EVENT_TYPES.AUTH_SIGNED_IN, () => {
        this.isAuthenticated = true;
      })
    );

    // Stop hub when user signs out
    this.authListenerCleanups.push(
      listenEvent(EVENT_TYPES.AUTH_SIGNED_OUT, async () => {
        this.isAuthenticated = false;
        await this.stop();
      })
    );

    // Stop hub and clear state when relogin is required
    this.authListenerCleanups.push(
      listenEvent(EVENT_TYPES.AUTH_REQUIRES_RELOGIN, async () => {
        this.isAuthenticated = false;
        await this.stop();
      })
    );
  }

  /**
   * Get SignalR hub URL from environment or use default
   * @returns {string} Hub URL
   */
  getHubUrl() {
    const baseUrl = import.meta.env.VITE_SIGNALR_BASE_URL || 'http://localhost:8848';
    const hubPath = import.meta.env.VITE_SIGNALR_HUB_PATH || '/nodpt_hub';
    return `${baseUrl}${hubPath}`;
  }

  /**
   * Token factory for SignalR authentication
   * Called automatically during negotiate and reconnect
   * @returns {string} Firebase ID token from localStorage
   */
  tokenFactory() {
    try {
      const token = getToken('FirebaseToken', true);
      if (!token) {
        console.error('Firebase token not found in localStorage');
        throw new Error('Firebase token not available');
      }
      return token;
    } catch (error) {
      console.error('Failed to get Firebase token for SignalR:', error);
      throw error;
    }
  }

  /**
   * Initialize SignalR connection
   */
  async initialize() {
    // Stop existing connection before starting a new one
    if (this.connection) {
      console.log('Stopping existing SignalR connection before initializing new one');
      await this.stop();
    }

    // Check if user has a valid token (supports page refresh)
    const token = getToken('FirebaseToken', true);
    this.isAuthenticated = !!token;
    if (!this.isAuthenticated) {
      console.warn('Cannot initialize SignalR: user not authenticated');
      return;
    }
    try {
      const hubUrl = this.getHubUrl();

      // Build the connection with automatic reconnection and auth token
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: () => this.tokenFactory()
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            // Exponential backoff: 0s, 2s, 10s, 30s, then 60s
            if (retryContext.elapsedMilliseconds < 60000) {
              return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
            }
            return 60000;
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Setup event handlers
      this.setupEventHandlers();

      // Start the connection
      await this.start();
    } catch (error) {
      console.error('Error initializing SignalR:', error);
      this.updateConnectionStatus('disconnected');

      // Handle 401/403 errors with retry
      if (this.isAuthError(error)) {
        await this.handleAuthError(error);
      } else {
        throw error;
      }
    }
  }

  /**
   * Check if error is authentication related
   * @param {Error} error - Error object
   * @returns {boolean} True if auth error
   */
  isAuthError(error) {
    const statusCode = error?.statusCode || error?.response?.status;
    return statusCode === 401 || statusCode === 403;
  }

  /**
   * Handle authentication errors with retry logic
   * @param {Error} error - Authentication error
   */
  async handleAuthError(error) {
    if (this.retryCount >= this.maxRetries) {
      console.error('Max auth retry attempts reached, signing out');
      this.retryCount = 0;
      triggerEvent(EVENT_TYPES.AUTH_REQUIRES_RELOGIN, { reason: 'signalr-auth-failed' });
      return;
    }

    this.retryCount++;
    console.log(`Auth error, attempting reconnect (attempt ${this.retryCount}/${this.maxRetries})`);

    try {
      // Retry connection
      if (this.connection) {
        await this.connection.stop();
        this.connection = null;
      }

      await this.initialize();

      // Reset retry count on success
      this.retryCount = 0;
    } catch (retryError) {
      console.error('Reconnect failed:', retryError);

      if (this.retryCount >= this.maxRetries) {
        triggerEvent(EVENT_TYPES.AUTH_REQUIRES_RELOGIN, { reason: 'signalr-reconnect-failed' });
      }
    }
  }

  /**
   * Setup SignalR event handlers
   */
  setupEventHandlers() {
    // Connection closed
    this.connection.onclose((error) => {
      console.log('SignalR connection closed', error);
      this.updateConnectionStatus('disconnected');

      // Check if closure was due to auth error
      if (error && this.isAuthError(error)) {
        this.handleAuthError(error);
      }
    });

    // Reconnecting
    this.connection.onreconnecting((error) => {
      console.log('SignalR reconnecting', error);
      this.updateConnectionStatus('reconnecting');
    });

    // Reconnected
    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected', connectionId);
      this.updateConnectionStatus('connected');
      this.retryCount = 0; // Reset retry count on successful reconnect
    });

    // Listen to server messages (examples - customize based on your needs)
    this.connection.on('NodeUpdated', (nodeData) => {
      console.log('Node updated from server:', nodeData);
      triggerEvent(EVENT_TYPES.NODE_UPDATED_FROM_SERVER, nodeData);
    });

    this.connection.on('EditorCommand', (command) => {
      console.log('Editor command from server:', command);
      triggerEvent(EVENT_TYPES.EDITOR_COMMAND_FROM_SERVER, command);
    });
  }

  /**
   * Start the SignalR connection
   */
  async start() {
    if (!this.connection) {
      console.error('SignalR connection not initialized');
      return;
    }

    try {
      this.updateConnectionStatus('connecting');
      await this.connection.start();
      console.log('SignalR connected');
      this.updateConnectionStatus('connected');
    } catch (error) {
      console.error('Error starting SignalR connection:', error);
      this.updateConnectionStatus('disconnected');

      // Handle auth errors
      if (this.isAuthError(error)) {
        await this.handleAuthError(error);
      } else {
        throw error;
      }
    }
  }

  /**
   * Stop the SignalR connection
   */
  async stop() {
    if (!this.connection) {
      return;
    }

    try {
      await this.connection.stop();
      console.log('SignalR stopped');
      this.connection = null;
      this.updateConnectionStatus('disconnected');
    } catch (error) {
      console.error('Error stopping SignalR connection:', error);
    }
  }

  /**
   * Send a message to the server
   * @param {string} methodName - The server method name
   * @param  {...any} args - Arguments to pass to the server method
   */
  async invoke(methodName, ...args) {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      console.error('SignalR not connected');
      throw new Error('SignalR not connected');
    }

    try {
      return await this.connection.invoke(methodName, ...args);
    } catch (error) {
      console.error(`Error invoking ${methodName}:`, error);
      throw error;
    }
  }

  /**
   * Update connection status and notify listeners
   * @param {string} status - The new connection status
   */
  updateConnectionStatus(status) {
    this.connectionStatus = status;
    // Trigger event for components to update UI
    triggerEvent(EVENT_TYPES.SIGNALR_STATUS_CHANGED, status);

    // Notify all listeners
    this.listeners.forEach(listener => listener(status));
  }

  /**
   * Get current connection status
   * @returns {string} Current connection status
   */
  getConnectionStatus() {
    return this.connectionStatus;
  }

  /**
   * Add a listener for connection status changes
   * @param {Function} callback - Callback function
   * @returns {Function} Cleanup function
   */
  onStatusChange(callback) {
    this.listeners.push(callback);
    // Return cleanup function
    return () => {
      const index = this.listeners.indexOf(callback);
      if (index > -1) {
        this.listeners.splice(index, 1);
      }
    };
  }

  /**
   * Listen to a specific server event
   * @param {string} eventName - The event name
   * @param {Function} callback - Callback function
   */
  on(eventName, callback) {
    if (!this.connection) {
      console.error('SignalR connection not initialized');
      return;
    }
    this.connection.on(eventName, callback);
  }

  /**
   * Remove a specific event listener
   * @param {string} eventName - The event name
   * @param {Function} callback - Callback function
   */
  off(eventName, callback) {
    if (!this.connection) {
      return;
    }
    this.connection.off(eventName, callback);
  }

  /**
   * Cleanup all auth listeners
   */
  destroy() {
    this.authListenerCleanups.forEach(cleanup => cleanup());
    this.authListenerCleanups = [];
  }
}

// Create and export a singleton instance
const signalRService = new SignalRService();
export default signalRService;
