import axios from 'axios';

// Use environment variable if available, otherwise fallback to localhost
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5049/api';

class AuthApiService {
	constructor() {
		this.baseURL = `${API_BASE_URL}/auth`;
	}

	/**
	 * Login with Firebase token and optional remember me
	 * @param {string} FirebaseToken - Firebase ID token
	 * @param {boolean} rememberMe - Whether to remember the user
	 * @returns {Promise<Object>} API response with auth tokens
	 */
	async login(FirebaseToken, rememberMe = false) {
		try {
			const response = await axios.post(`${this.baseURL}/login`, {
				FirebaseToken,
				rememberMe,
			});

			// Store user data including PhotoUrl in localStorage/sessionStorage
			if (response.data && response.data.User) {
				const storage = rememberMe ? localStorage : sessionStorage;
				storage.setItem('userData', JSON.stringify(response.data.User));
			}

			return response.data;
		} catch (error) {
			console.error('Failed to login:', error);
			throw error;
		}
	}

	/**
	 * Refresh authentication token
	 * @param {string} refreshToken - Refresh token
	 * @returns {Promise<Object>} API response with new tokens
	 */
	async refresh(refreshToken) {
		try {
			const response = await axios.post(`${this.baseURL}/refresh`, {
				refreshToken,
			});
			return response.data;
		} catch (error) {
			console.error('Failed to refresh token:', error);
			throw error;
		}
	}



	/**
	 * Get stored user data
	 * @returns {Object|null} User data or null if not found
	 */
	getUserData() {
		try {
			const userData = localStorage.getItem('userData') || sessionStorage.getItem('userData');
			return userData ? JSON.parse(userData) : null;
		} catch (error) {
			console.error('Failed to parse user data:', error);
			return null;
		}
	}
}

export default new AuthApiService();
