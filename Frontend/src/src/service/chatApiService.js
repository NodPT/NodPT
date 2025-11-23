// Use environment variable if available, otherwise fallback to localhost
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5049/api';

class ChatApiService {
    constructor() {
        this.baseURL = `${API_BASE_URL}/chat`;
        this.api = null;
        this.connectionId = null;
    }

    /**
     * Initialize the API plugin reference
     * @param {Object} api - The injected API plugin
     */
    setApi(api) {
        this.api = api;
    }

    /**
     * Set the SignalR connection ID for chat requests
     * @param {string} connectionId - The SignalR connection ID
     */
    setConnectionId(connectionId) {
        this.connectionId = connectionId;
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

            const messageDto = {
                Sender: 'user',
                Message: message.content,
                NodeId: message.nodeId,
                MarkedAsSolution: false,
                Liked: false,
                Disliked: false
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

            const response = await this.api.post(`${this.baseURL}/mark-solution`, {
                MessageId: messageId,
                NodeId: nodeId
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
     * Like a chat message
     * @param {string} messageId - Message ID to like
     * @returns {Promise<Object>} API response
     */
    async likeMessage(messageId) {
        try {
            if (!messageId) {
                throw new Error('messageId is required');
            }

            const response = await this.api.post(`${this.baseURL}/like`, {
                ChatMessageId: messageId
            });
            return response;
        } catch (error) {
            console.error('Failed to like message:', error);
            throw error;
        }
    }

    /**
     * Dislike a chat message
     * @param {string} messageId - Message ID to dislike
     * @returns {Promise<Object>} API response
     */
    async dislikeMessage(messageId) {
        try {
            if (!messageId) {
                throw new Error('messageId is required');
            }

            const response = await this.api.post(`${this.baseURL}/dislike`, {
                ChatMessageId: messageId
            });
            return response;
        } catch (error) {
            console.error('Failed to dislike message:', error);
            throw error;
        }
    }
}

export default new ChatApiService();