import { auth, googleProvider, facebookProvider, microsoftProvider } from '../plugins/firebase';
import { createUserWithEmailAndPassword, signInWithEmailAndPassword, signInWithPopup } from 'firebase/auth';
import { bus, EVENT_TYPES } from '../plugins/bus';
import { storeToken, getToken } from '../plugins/tokenStorage';

class AuthApiService {
	constructor() {
		this.baseURL = '/auth';
		this.api = null;
		this.loginProviders = {
			Google: this.loginWithGoogle.bind(this),
			Facebook: this.loginWithFacebook.bind(this),
			Microsoft: this.loginWithMicrosoft.bind(this),
		};
	}

	/**
	 * Initialize the API plugin reference
	 * @param {Object} api - The injected API plugin
	 */
	setApi(api) {
		this.api = api;
	}

	/**
	 * Check if Firebase auth is initialized
	 * @private
	 * @throws {Error} If Firebase auth is not initialized
	 */
	_checkAuthInitialized() {
		if (!auth) {
			throw new Error('Firebase authentication is not initialized. Please check your Firebase configuration.');
		}
	}

	registerWithEmail(email, pw) {
		this._checkAuthInitialized();
		return createUserWithEmailAndPassword(auth, email, pw);
	}

	loginWithEmail(email, pw) {
		this._checkAuthInitialized();
		return signInWithEmailAndPassword(auth, email, pw);
	}

	loginWithGoogle() {
		this._checkAuthInitialized();
		return signInWithPopup(auth, googleProvider);
	}

	loginWithFacebook() {
		this._checkAuthInitialized();
		return signInWithPopup(auth, facebookProvider);
	}

	loginWithMicrosoft() {
		this._checkAuthInitialized();
		return signInWithPopup(auth, microsoftProvider);
	}

	/**
	 * Logout and clean up all sessions
	 */
	async logout() {
		try {
			await this.logoutApi();
		} catch (error) {
			console.error('Logout error:', error);
			throw error;
		}
	}

	async logoutApi() {
		try {
			const response = await this.api.get('/auth/logout');
			localStorage.removeItem('userData');
			sessionStorage.removeItem('userData');
			return response;
		} catch (error) {
			console.error('Failed to logout:', error);
			throw error;
		}
	}

	/**
	 * Emit sign-in event after successful authentication
	 * Should be called after Firebase authentication completes
	 */
	notifySignIn() {
		bus.trigger(EVENT_TYPES.AUTH_SIGNED_IN);
	}

	/**
	 * Login with Firebase token
	 * @param {string} FirebaseToken - Firebase ID token
	 * @returns {Promise<Object>} API response with auth tokens
	 */
	async login(FirebaseToken, providerName = 'Google') {
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
			});

			// Clear any legacy localStorage auth state from previous sessions
			['AccessToken', 'FirebaseToken', 'refreshToken', 'userData', 'rememberMeTimestamp', 'lastActivity'].forEach(k => localStorage.removeItem(k));

			// Store user data in sessionStorage
			if (response && response.User) {
				sessionStorage.setItem('userData', JSON.stringify(response.User));
			}

			storeToken('FirebaseToken', tokenToSend);
			if (response?.AccessToken) {
				storeToken('AccessToken', response.AccessToken);
			}
			if (response?.RefreshToken) {
				storeToken('RefreshToken', response.RefreshToken);
			}

			return response;
		} catch (error) {
			console.error('Failed to login:', error);
			throw error;
		}
	}

	async loginAndStore(provider = 'Google') {
		const isDevelopment = import.meta.env.VITE_ENV === 'Development';
		let firebaseToken = 'dev-mock-token';
		const providerName = this.loginProviders[provider] ? provider : 'Google';

		if (!isDevelopment) {
			// perform actual Firebase login such as Google, Facebook, etc.
			const loginFn = this.loginProviders[providerName];
			if (!loginFn) {
				throw new Error('Login provider is not configured');
			}

			const result = await loginFn();
			const user = result.user;
			firebaseToken = await user.getIdToken();
		}

		// Now perform backend login with the obtained Firebase token
		const response = await this.login(firebaseToken, providerName);
		this.notifySignIn(); // Notify other parts of the app about sign-in
		return response;
	}

	/**
	 * Refresh the access token using the stored refresh token.
	 * On success, updates AccessToken and RefreshToken in sessionStorage.
	 * Returns true if the refresh succeeded, false otherwise.
	 * @returns {Promise<boolean>}
	 */
	async refreshToken() {
		try {
			const refreshToken = getToken('RefreshToken');
			if (!refreshToken) {
				return false;
			}

			const response = await this.api.post(`${this.baseURL}/refresh`, {
				RefreshToken: refreshToken,
			});

			if (response?.AccessToken) {
				storeToken('AccessToken', response.AccessToken);
			}
			if (response?.RefreshToken) {
				storeToken('RefreshToken', response.RefreshToken);
			}
			if (response?.User) {
				sessionStorage.setItem('userData', JSON.stringify(response.User));
			}

			return true;
		} catch (error) {
			console.error('Token refresh failed:', error);
			return false;
		}
	}

	/**
	 * Get stored user data
	 * @returns {Object|null} User data or null if not found
	 */
	getUserData() {
		try {
			const userData = sessionStorage.getItem('userData');
			return userData ? JSON.parse(userData) : null;
		} catch (error) {
			console.error('Failed to parse user data:', error);
			return null;
		}
	}
}

export default new AuthApiService();
