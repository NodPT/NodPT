// Use environment variable if available, otherwise fallback to localhost
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5049/api';

class NodeApiService {
	constructor() {
		this.baseURL = `${API_BASE_URL}/nodes`;
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
	 * Get a node by ID
	 * @param {string} id - Node ID
	 * @returns {Promise<Object>} Node data
	 */
	async getNode(id) {
		try {
			// Check localStorage first
			const cachedNode = this.getNodeFromCache(id);
			if (cachedNode) {
				return cachedNode;
			}

			// Fetch from API
			const response = await this.api.get(`${this.baseURL}/${id}`);
			
			// Cache the node data
			this.cacheNode(response);
			
			return response;
		} catch (error) {
			console.error('Failed to get node:', error);
			throw error;
		}
	}

	/**
	 * Get all nodes for a project
	 * @param {number} projectId - Project ID
	 * @returns {Promise<Array>} Array of nodes
	 */
	async getNodesByProject(projectId) {
		try {
			const response = await this.api.get(`${this.baseURL}/project/${projectId}`);
			
			// Cache all nodes
			if (response && Array.isArray(response)) {
				response.forEach(node => this.cacheNode(node));
			}
			
			return response;
		} catch (error) {
			console.error('Failed to get nodes by project:', error);
			throw error;
		}
	}

	/**
	 * Create a new node
	 * @param {Object} node - Node data (NodeDto)
	 * @returns {Promise<Object>} Created node with ID
	 */
	async createNode(node) {
		try {
			const response = await this.api.post(this.baseURL, node);
			
			// Cache the created node
			this.cacheNode(response);
			
			return response;
		} catch (error) {
			console.error('Failed to create node:', error);
			throw error;
		}
	}

	/**
	 * Update a node
	 * @param {string} id - Node ID
	 * @param {Object} node - Updated node data
	 * @returns {Promise<Object>} Updated node
	 */
	async updateNode(id, node) {
		try {
			const response = await this.api.put(`${this.baseURL}/${id}`, node);
			
			// Update cache
			this.cacheNode(response);
			
			return response;
		} catch (error) {
			console.error('Failed to update node:', error);
			throw error;
		}
	}

	/**
	 * Delete a node
	 * @param {string} id - Node ID
	 * @returns {Promise<boolean>} True if deleted successfully
	 */
	async deleteNode(id) {
		try {
			await this.api.delete(`${this.baseURL}/${id}`);
			
			// Remove from cache
			this.removeNodeFromCache(id);
			
			return true;
		} catch (error) {
			console.error('Failed to delete node:', error);
			throw error;
		}
	}

	/**
	 * Get node from localStorage cache
	 * @param {string} nodeId - Node ID
	 * @returns {Object|null} Cached node or null
	 */
	getNodeFromCache(nodeId) {
		try {
			const cacheKey = `node_${nodeId}`;
			const cachedData = localStorage.getItem(cacheKey);
			
			if (cachedData) {
				const parsedData = JSON.parse(cachedData);
				
				// Check if cache is still valid (e.g., less than 1 hour old)
				const cacheAge = Date.now() - parsedData.cachedAt;
				const maxAge = 60 * 60 * 1000; // 1 hour
				
				if (cacheAge < maxAge) {
					return parsedData.node;
				} else {
					// Cache expired, remove it
					localStorage.removeItem(cacheKey);
				}
			}
			
			return null;
		} catch (error) {
			console.error('Error reading node from cache:', error);
			return null;
		}
	}

	/**
	 * Cache node data in localStorage
	 * @param {Object} node - Node data to cache
	 */
	cacheNode(node) {
		try {
			if (!node || !node.Id) {
				return;
			}
			
			const cacheKey = `node_${node.Id}`;
			const cacheData = {
				node: node,
				cachedAt: Date.now()
			};
			
			localStorage.setItem(cacheKey, JSON.stringify(cacheData));
		} catch (error) {
			console.error('Error caching node:', error);
		}
	}

	/**
	 * Remove node from cache
	 * @param {string} nodeId - Node ID
	 */
	removeNodeFromCache(nodeId) {
		try {
			const cacheKey = `node_${nodeId}`;
			localStorage.removeItem(cacheKey);
		} catch (error) {
			console.error('Error removing node from cache:', error);
		}
	}

	/**
	 * Clear all node cache
	 */
	clearCache() {
		try {
			const keys = Object.keys(localStorage);
			keys.forEach(key => {
				if (key.startsWith('node_')) {
					localStorage.removeItem(key);
				}
			});
		} catch (error) {
			console.error('Error clearing node cache:', error);
		}
	}
}

export default new NodeApiService();
