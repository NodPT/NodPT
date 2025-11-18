// Use environment variable if available, otherwise fallback to localhost
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5049/api';

class ChatMessageApiService {
	constructor() {
		this.baseURL = `${API_BASE_URL}/chatmessages`;
		this.api = null;
	}

	/**
	 * Initialize the API plugin reference
	 * @param {Object} api - The injected API plugin
	 */
	setApi(api) {
		this.api = api;
	}

	/**
	 * Get chat messages for a specific node
	 * @param {string} nodeId - Node ID
	 * @returns {Promise<Array>} Array of chat messages
	 */
	async getMessagesByNode(nodeId) {
		try {
			// Check localStorage first
			const cachedMessages = this.getMessagesFromCache(nodeId);
			if (cachedMessages) {
				return cachedMessages;
			}

			// Fetch from API
			const response = await this.api.get(`${this.baseURL}/node/${nodeId}`);
			
			// Cache the messages
			this.cacheMessages(nodeId, response);
			
			return response || [];
		} catch (error) {
			console.error('Failed to get messages by node:', error);
			// Return empty array instead of throwing to avoid breaking the UI
			return [];
		}
	}

	/**
	 * Create first chat for a node
	 * @param {string} nodeId - Node ID
	 * @param {string} userId - User Firebase UID
	 * @returns {Promise<Object>} Created chat message
	 */
	async createFirstChat(nodeId, userId) {
		try {
			const firstMessage = {
				Sender: 'ai',
				Message: 'Hello! I\'m here to help you with this node. What would you like to work on?',
				Timestamp: new Date().toISOString(),
				NodeId: nodeId,
				MarkedAsSolution: false
			};

			// Use the chatmessages endpoint for creating the first chat message
			const response = await this.api.post(this.baseURL, firstMessage);
			
			// Clear cache for this node to force refresh
			this.clearMessagesCache(nodeId);
			
			return response;
		} catch (error) {
			console.error('Failed to create first chat:', error);
			throw error;
		}
	}

	/**
	 * Get messages from localStorage cache
	 * @param {string} nodeId - Node ID
	 * @returns {Array|null} Cached messages or null
	 */
	getMessagesFromCache(nodeId) {
		try {
			const cacheKey = `chat_messages_${nodeId}`;
			const cachedData = localStorage.getItem(cacheKey);
			
			if (cachedData) {
				const parsedData = JSON.parse(cachedData);
				
				// Check if cache is still valid (e.g., less than 5 minutes old for chat messages)
				const cacheAge = Date.now() - parsedData.cachedAt;
				const maxAge = 5 * 60 * 1000; // 5 minutes
				
				if (cacheAge < maxAge) {
					return parsedData.messages;
				} else {
					// Cache expired, remove it
					localStorage.removeItem(cacheKey);
				}
			}
			
			return null;
		} catch (error) {
			console.error('Error reading messages from cache:', error);
			return null;
		}
	}

	/**
	 * Cache messages in localStorage
	 * @param {string} nodeId - Node ID
	 * @param {Array} messages - Messages to cache
	 */
	cacheMessages(nodeId, messages) {
		try {
			if (!nodeId) {
				return;
			}
			
			const cacheKey = `chat_messages_${nodeId}`;
			const cacheData = {
				messages: messages || [],
				cachedAt: Date.now()
			};
			
			localStorage.setItem(cacheKey, JSON.stringify(cacheData));
		} catch (error) {
			console.error('Error caching messages:', error);
		}
	}

	/**
	 * Clear messages cache for a node
	 * @param {string} nodeId - Node ID
	 */
	clearMessagesCache(nodeId) {
		try {
			const cacheKey = `chat_messages_${nodeId}`;
			localStorage.removeItem(cacheKey);
		} catch (error) {
			console.error('Error clearing messages cache:', error);
		}
	}

	/**
	 * Clear all messages cache
	 */
	clearAllCache() {
		try {
			const keys = Object.keys(localStorage);
			keys.forEach(key => {
				if (key.startsWith('chat_messages_')) {
					localStorage.removeItem(key);
				}
			});
		} catch (error) {
			console.error('Error clearing all messages cache:', error);
		}
	}
}

export default new ChatMessageApiService();
