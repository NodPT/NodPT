// Use environment variable if available, otherwise fallback to localhost
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5049/api';

class UserApiService {
	constructor() {
		this.baseURL = `${API_BASE_URL}/users`;
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
	 * Update user profile
	 * The backend automatically gets the user from the JWT token
	 * @param {Object} profileData - Profile data to update (DisplayName, PhotoUrl)
	 * @returns {Promise<Object>} API response
	 */
	async updateProfile(profileData) {
		try {
			const response = await this.api.put(`${this.baseURL}/me`, profileData);
			return response;
		} catch (error) {
			console.error('Failed to update profile:', error);
			throw error;
		}
	}
}

export default new UserApiService();
