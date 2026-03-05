class ChatApiService {
	constructor() {
		this.baseURL = '/chat';
		this.api = null;
		this.connectionId = localStorage.getItem('connectionId'); // Optional SignalR connection ID
	}

	/**
	 * Initialize the API plugin reference
	 * @param {Object} api - The injected API plugin
	 */
	setApi(api) {
		this.api = api;
	}

	/**
	 * Send a message to the chat API and queue for AI processing
	 * @param {Object} message - Message object with content, nodeId, etc.
	 * @returns {Promise<Object>} API response
	 */
	async sendMessage(message) {
		try {
			if (!message.nodeId) {
				throw new Error('nodeId is required for sending messages');
			}
			this.connectionId = localStorage.getItem('connectionId'); // Optional SignalR connection ID
			const messageDto = {
				Sender: 'user',
				Message: message.content,
				NodeId: message.nodeId,
				MarkedAsSolution: false,
				ConnectionId: this.connectionId || null,
			};

			// Add SignalR connectionId to headers if available
			const headers = {};
			if (this.connectionId) {
				headers['X-SignalR-ConnectionId'] = this.connectionId;
			}

			const response = await this.api.post(`${this.baseURL}/send`, messageDto, { headers });
			return response;
		} catch (error) {
			console.error('Failed to send message:', error);
			throw error;
		}
	}

	/**
	 * Mark a message as solution
	 * @param {string} messageId - Message ID to mark as solution
	 * @param {string} nodeId - Optional node ID context
	 * @returns {Promise<Object>} API response
	 */
	async markAsSolution(messageId, nodeId = null) {
		try {
			if (!messageId) {
				throw new Error('messageId is required for marking as solution');
			}

			this.connectionId = localStorage.getItem('connectionId'); // Get current SignalR connection ID
			
			const response = await this.api.post(`${this.baseURL}/mark-solution`, {
				MessageId: messageId,
				NodeId: nodeId,
				ConnectionId: this.connectionId || null,
			});
			return response;
		} catch (error) {
			console.error('Failed to mark as solution:', error);
			throw error;
		}
	}

	/**
	 * Get chat messages for a specific node (from database)
	 * @param {string} nodeId - Node ID
	 * @returns {Promise<Array>} Array of chat messages
	 */
	async getMessagesByNodeId(nodeId) {
		try {
			if (!nodeId) {
				throw new Error('nodeId is required');
			}

			const response = await this.api.get(`${this.baseURL}/node/${nodeId}`);
			return response;
		} catch (error) {
			console.error('Failed to get messages by node ID:', error);
			throw error;
		}
	}

	/**
	 * Retry processing an AI chat message
	 * @param {string} messageId - Message ID to retry
	 * @param {string} nodeId - Optional node ID context
	 * @returns {Promise<Object>} API response
	 */
	async retryMessage(messageId, nodeId = null) {
		try {
			if (!messageId) {
				throw new Error('messageId is required for retry');
			}

			this.connectionId = localStorage.getItem('connectionId'); // Get current SignalR connection ID
			
			const response = await this.api.post(`${this.baseURL}/retry`, {
				MessageId: messageId,
				NodeId: nodeId,
				ConnectionId: this.connectionId || null,
			});
			return response;
		} catch (error) {
			console.error('Failed to retry message:', error);
			throw error;
		}
	}
}

export default new ChatApiService();
