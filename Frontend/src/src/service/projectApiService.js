import { getToken } from './tokenStorage';

// Use environment variable if available, otherwise fallback to localhost
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5049/api';

class ProjectApiService {

	constructor() {
		this.baseURL = `${API_BASE_URL}/projects`;
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
	 * Get authorization headers with access token
	 * @returns {Object} Headers with Authorization
	 */
	getAuthHeaders() {
		const token = getToken('AccessToken');
		return {
			'Authorization': `Bearer ${token}`,
			'Content-Type': 'application/json'
		};
	}

	/**
	 * Create a new project
	 * @param {Object} project - Project data (ProjectDto)
	 * @returns {Promise<Object>} Created project with ID
	 */
	async createProject(project) {
		try {
			const response = await this.api.post(this.baseURL, project);
			return response;
		} catch (error) {
			console.error('Failed to create project:', error);
			throw error;
		}
	}

	/**
	 * Delete a project by ID
	 * @param {number} id - Project ID
	 * @returns {Promise<boolean>} True if deleted successfully
	 */
	async deleteProject(id) {
		try {
			await this.api.delete(`${this.baseURL}/${id}`);
			return true;
		} catch (error) {
			console.error('Failed to delete project:', error);
			throw error;
		}
	}

	/**
	 * Get a project by ID
	 * @param {number} id - Project ID
	 * @returns {Promise<Object>} Project data
	 */
	async getProject(id) {
		try {
			const response = await this.api.get(`${this.baseURL}/${id}`);
			return response;
		} catch (error) {
			console.error('Failed to get project:', error);
			throw error;
		}
	}

	async getProjectsByUser(userId) {
		try {
			const data = await this.api.get(`/projects/user/${userId}`)
			return data
		} catch (err) {
			console.error('Failed to get projects by user:', err)
			throw err
		}
	}
}

export default new ProjectApiService();
