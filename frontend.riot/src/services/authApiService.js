import { auth, googleProvider, facebookProvider, microsoftProvider, signOutAll as firebaseSignOutAll } from '../firebase';
import {
	createUserWithEmailAndPassword,
	signInWithEmailAndPassword,
	signInWithPopup,
} from 'firebase/auth';
import { this.bus.trigger, EVENT_TYPES } from '../rete/bus';

class AuthApiService {
	constructor() {
		this.baseURL = '/auth';
		this.api = null;
	}

	/**
	 * Initialize the API plugin reference
	 * @param {Object} api - The injected API plugin
	 */
	setApi(api) {
		this.api = api;
	}

	registerWithEmail(email, pw) {
		return createUserWithEmailAndPassword(auth, email, pw);
	}

	loginWithEmail(email, pw) {
		return signInWithEmailAndPassword(auth, email, pw);
	}

	loginWithGoogle() {
		return signInWithPopup(auth, googleProvider);
	}

	loginWithFacebook() {
		return signInWithPopup(auth, facebookProvider);
	}

	loginWithMicrosoft() {
		return signInWithPopup(auth, microsoftProvider);
	}

	/**
	 * Logout and clean up all sessions
	 */
	async logout() {
		try {
			await firebaseSignOutAll();
			// Event is emitted by signOutAll function
		} catch (error) {
			console.error('Logout error:', error);
			throw error;
		}
	}

	/**
	 * Emit sign-in event after successful authentication
	 * Should be called after Firebase authentication completes
	 */
	notifySignIn() {
		this.bus.trigger(EVENT_TYPES.AUTH_SIGNED_IN);
	}

	/**
	 * Login with Firebase token and optional remember me
	 * @param {string} FirebaseToken - Firebase ID token
	 * @param {boolean} rememberMe - Whether to remember the user
	 * @returns {Promise<Object>} API response with auth tokens
	 */
	async login(FirebaseToken, rememberMe = false) {
		try {
			// Check if we're in Development mode
			const isDevelopment = import.meta.env.VITE_ENV === 'Development';
			
			let tokenToSend = FirebaseToken;
			if (isDevelopment) {
				// In Development mode, bypass Firebase and send mock token
				console.log('Development mode: Bypassing Firebase authentication');
				tokenToSend = 'dev-mock-token';
			}

			const response = await this.api.post(`${this.baseURL}/login`, {
				FirebaseToken: tokenToSend,
				rememberMe,
			});

			// Store user data including PhotoUrl in localStorage/sessionStorage
			if (response && response.User) {
				const storage = rememberMe ? localStorage : sessionStorage;
				storage.setItem('userData', JSON.stringify(response.User));
			}

			return response;
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
			const response = await this.api.post(`${this.baseURL}/refresh`, {
				refreshToken,
			});
			return response;
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
