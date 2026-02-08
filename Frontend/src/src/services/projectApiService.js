import { getToken } from '../plugins/tokenStorage';


class ProjectApiService {
	constructor() {
		this.baseURL = '/projects';
		this.api = null;
	}

	/**
	 * Initialize the API plugin reference
	 * @param {Object} api - The injected API plugin
	 */
	setApi(api) {
		this.api = api;
	}

	setProjectId(id) {
		this.projectId = id;
		localStorage.setItem('CurrentProjectId', id);
	}

	getProjectId() {
		this.projectId = localStorage.getItem('CurrentProjectId');
		return this.projectId;
	}

	/**
	 * Get authorization headers with access token
	 * @returns {Object} Headers with Authorization
	 */
	getAuthHeaders() {
		const token = getToken('AccessToken');
		return {
			Authorization: `Bearer ${token}`,
			'Content-Type': 'application/json',
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
	 * @returns {Promise<Object>} Project data (ProjectDto)
	 *
	 * Response format (ProjectDto from backend):
	 * {
	 *   Id: number,
	 *   Name: string,
	 *   Description: string,
	 *   IsActive: boolean,
	 *   CreatedAt: DateTime,
	 *   UpdatedAt: DateTime,
	 *   UserId: number,
	 *   TemplateId: number,
	 *   TemplateName: string,
	 *   UserEmail: string,
	 *   Nodes: [                          // Array of NodeDto
	 *     {
	 *       Id: string,                   // GUID - Required for frontend node creation
	 *       Name: string,                 // Node display name
	 *       NodeType: string,             // Enum: "Director", "Manager", "Supervisor", "Agent", "Compiler", "Tester", "Runner"
	 *       MessageType: string,          // Enum value
	 *       Status: string,               // "Active", etc.
	 *       ParentId: string,             // Optional: Parent node GUID
	 *       ProjectId: number,
	 *       TemplateId: number,
	 *       Properties: object,           // Key-value pairs
	 *       CreatedAt: DateTime,
	 *       UpdatedAt: DateTime,
	 *       ...
	 *     }
	 *   ]
	 * }
	 *
	 * Note: Backend uses PascalCase (C# convention). Properties are case-sensitive.
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

	/**
	 * Get all projects for the authenticated user
	 * No need to pass userId - backend extracts it from JWT token
	 * @returns {Promise<Array>} Array of projects
	 */
	async getUserProjects() {
		try {
			const data = await this.api.get(this.baseURL);
			return data;
		} catch (err) {
			console.error('Failed to get user projects:', err);
			throw err;
		}
	}

	/**
	 * @deprecated Use getUserProjects() instead
	 * Get projects by user ID (legacy method for backward compatibility)
	 * @param {string} userId - User ID (firebaseUid)
	 * @returns {Promise<Array>} Array of projects
	 */
	async getProjectsByUser(userId) {
		throw new Error('getProjectsByUser() is deprecated and removed. Use getUserProjects() instead. Note: Backend now only returns projects for the authenticated user.');
	}

	/**
	 * Update project name
	 * @param {number} id - Project ID
	 * @param {string} name - New project name
	 * @returns {Promise<Object>} Updated project data
	 */
	async updateProjectName(id, name) {
		try {
			const response = await this.api.put(`${this.baseURL}/${id}/name`, { Name: name });
			return response;
		} catch (error) {
			console.error('Failed to update project name:', error);
			throw error;
		}
	}
}

export default new ProjectApiService();
