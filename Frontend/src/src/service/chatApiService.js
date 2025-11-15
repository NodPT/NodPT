// Use environment variable if available, otherwise fallback to localhost
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5049/api';

class ChatApiService {
    constructor() {
        this.baseURL = `${API_BASE_URL}/chat`;
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
     * Send a message to the chat API and get AI response
     * @param {Object} message - Message object with content, nodeId, etc.
     * @returns {Promise<Object>} API response with AI message
     */
    async sendMessage(message) {
        try {
            const messageDto = {
                sender: 'user',
                message: message.content,
                nodeId: message.nodeId || null,
                markedAsSolution: false,
                liked: false,
                disliked: false
            };

            const response = await this.api.post(`${this.baseURL}/send`, messageDto);
            return response;
        } catch (error) {
            console.error('Failed to send message:', error);
            throw error;
        }
    }

    /**
     * Mark the latest message as solution and get comprehensive AI response
     * @param {string} nodeId - Optional node ID context
     * @returns {Promise<Object>} API response with comprehensive solution
     */
    async markAsSolution(nodeId = null) {
        try {
            const response = await this.api.post(`${this.baseURL}/mark-solution`, {
                nodeId: nodeId
            });
            return response;
        } catch (error) {
            console.error('Failed to mark as solution:', error);
            throw error;
        }
    }

    /**
     * Get chat messages for a specific node (from in-memory chat service)
     * @param {string} nodeId - Node ID
     * @returns {Promise<Array>} Array of chat messages
     */
    async getMessagesByNodeId(nodeId) {
        try {
            const response = await this.api.get(`${this.baseURL}/node/${nodeId}`);
            return response;
        } catch (error) {
            console.error('Failed to get messages by node ID:', error);
            throw error;
        }
    }

    /**
     * Get persisted chat messages for a specific node from the database
     * @param {string} nodeId - Node ID
     * @returns {Promise<Array>} Array of chat messages
     */
    async getPersistedMessagesByNodeId(nodeId) {
        try {
            const response = await this.api.get(`/chatmessages/node/${nodeId}`);
            return response;
        } catch (error) {
            console.error('Failed to get persisted messages by node ID:', error);
            throw error;
        }
    }

    /**
     * Get all chat messages
     * @returns {Promise<Array>} Array of all chat messages
     */
    async getAllMessages() {
        try {
            const response = await this.api.get(this.baseURL);
            return response;
        } catch (error) {
            console.error('Failed to get all messages:', error);
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
            const response = await this.api.post(`${this.baseURL}/like`, {
                chatMessageId: messageId
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
            const response = await this.api.post(`${this.baseURL}/dislike`, {
                chatMessageId: messageId
            });
            return response;
        } catch (error) {
            console.error('Failed to dislike message:', error);
            throw error;
        }
    }

    /**
     * Regenerate a chat message
     * @param {string} messageId - Original message ID to regenerate
     * @returns {Promise<Object>} API response with new message
     */
    async regenerateMessage(messageId) {
        try {
            const response = await this.api.post(`${this.baseURL}/regenerate`, {
                chatMessageId: messageId
            });
            return response;
        } catch (error) {
            console.error('Failed to regenerate message:', error);
            throw error;
        }
    }
}

export default new ChatApiService();