import { auth, googleProvider, facebookProvider, microsoftProvider, signOutAll as firebaseSignOutAll } from '../plugins/firebase';
import { createUserWithEmailAndPassword, signInWithEmailAndPassword, signInWithPopup } from 'firebase/auth';
import { bus, EVENT_TYPES } from '../plugins/bus';
import { storeToken, getToken, clearAllTokens, clearLocalStorageTokens } from '../plugins/tokenStorage';

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
			// await firebaseSignOuAll();
			// Event is emitted by signOutAll function
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
	 * Login with Firebase token and optional remember me
	 * @param {string} FirebaseToken - Firebase ID token
	 * @param {boolean} rememberMe - Whether to remember the user
	 * @returns {Promise<Object>} API response with auth tokens
	 */
	async login(FirebaseToken, rememberMe = false, providerName = 'Google') {
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

			if (rememberMe) {
				localStorage.setItem('rememberMeTimestamp', Date.now().toString());
				localStorage.setItem('lastActivity', Date.now().toString());
			} else {
				// Clear any stale remembered session from a prior rememberMe login BEFORE
				// storing new session tokens, to avoid wiping the just-stored sessionStorage tokens
				this.clearRememberedSession();
			}

			// Store user data including PhotoUrl in localStorage/sessionStorage
			if (response && response.User) {
				const storage = rememberMe ? localStorage : sessionStorage;
				storage.setItem('userData', JSON.stringify(response.User));
			}

			storeToken('FirebaseToken', tokenToSend, rememberMe);
			if (response?.AccessToken) {
				storeToken('AccessToken', response.AccessToken, rememberMe);
			}
			if (response?.refreshToken && rememberMe) {
				storeToken('refreshToken', response.refreshToken, true);
			}

			return response;
		} catch (error) {
			console.error('Failed to login:', error);
			throw error;
		}
	}

	async loginAndStore(rememberMe = false, provider = 'Google') {
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
		const response = await this.login(firebaseToken, rememberMe, providerName);
		this.notifySignIn(); // Notify other parts of the app about sign-in
		return response;
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

	/**
	 * Check if a valid remembered session exists (rememberMe was used and session is not expired)
	 * Validates: token in localStorage, 3-month max life (calendar months), 1-week inactivity rule
	 * @returns {boolean} True if a valid remembered session exists
	 */
	isSessionValid() {
		// Check if the AccessToken key exists directly in localStorage (key is stored as-is, only value is obfuscated)
		const token = localStorage.getItem('AccessToken');
		if (!token) return false;

		const userData = localStorage.getItem('userData');
		if (!userData) return false;

		const rememberMeTimestamp = localStorage.getItem('rememberMeTimestamp');
		if (!rememberMeTimestamp) return false;

		const now = Date.now();
		const ONE_WEEK_MS = 7 * 24 * 60 * 60 * 1000;

		// Parse and validate rememberMe timestamp
		const rememberMeTimestampMs = Number(rememberMeTimestamp);
		if (!Number.isFinite(rememberMeTimestampMs)) {
			this.clearRememberedSession();
			return false;
		}

		// Check 3-month maximum token lifetime using calendar months (matches backend AddMonths(3))
		const rememberMeDate = new Date(rememberMeTimestampMs);
		const maxLifetimeDate = new Date(rememberMeDate.getTime());
		maxLifetimeDate.setMonth(maxLifetimeDate.getMonth() + 3);
		if (now > maxLifetimeDate.getTime()) {
			this.clearRememberedSession();
			return false;
		}

		// Check 1-week inactivity rule
		const lastActivity = localStorage.getItem('lastActivity');
		if (!lastActivity) {
			this.clearRememberedSession();
			return false;
		}

		const lastActivityMs = Number(lastActivity);
		if (!Number.isFinite(lastActivityMs)) {
			this.clearRememberedSession();
			return false;
		}

		if (now - lastActivityMs > ONE_WEEK_MS) {
			this.clearRememberedSession();
			return false;
		}

		return true;
	}

	/**
	 * Attempt to restore a remembered session by refreshing the access token.
	 * Call this on app startup / page load when the user returns after a browser close.
	 * @returns {Promise<boolean>} True if session was successfully restored
	 */
	async restoreSession() {
		if (!this.isSessionValid()) return false;

		const refreshTokenValue = getToken('refreshToken', true);
		if (!refreshTokenValue) {
			this.clearRememberedSession();
			return false;
		}

		try {
			const response = await this.refresh(refreshTokenValue);
			if (response?.Success && response.AccessToken) {
				storeToken('AccessToken', response.AccessToken, true);
				if (response.RefreshToken) {
					storeToken('refreshToken', response.RefreshToken, true);
				}
				if (response.User) {
					localStorage.setItem('userData', JSON.stringify(response.User));
				}
				this.updateLastActivity();
				return true;
			}
			this.clearRememberedSession();
			return false;
		} catch (error) {
			console.error('Session restore failed:', error);
			this.clearRememberedSession();
			return false;
		}
	}

	/**
	 * Update the last activity timestamp (call on each page visit with valid session)
	 */
	updateLastActivity() {
		localStorage.setItem('lastActivity', Date.now().toString());
	}

	/**
	 * Clear all remembered session data from localStorage
	 */
	clearRememberedSession() {
		localStorage.removeItem('userData');
		localStorage.removeItem('rememberMeTimestamp');
		localStorage.removeItem('lastActivity');
		clearLocalStorageTokens();
	}
}

export default new AuthApiService();
