// src/plugins/api-plugin.js
import axios from 'axios';
import { router } from '@riotjs/route';
import { getToken, storeToken, clearAllTokens } from './tokenStorage';

/**
 * Read the access token from storage using the existing tokenStorage service
 * The tokenStorage service automatically checks both localStorage and sessionStorage
 * @returns {string|null} Access token or null if not found
 */
function readTokenFromStorage() {
	try {
		// getToken with persist=true automatically falls back to sessionStorage if not in localStorage
		const token = getToken('AccessToken', true);
		return token;
	} catch (e) {
		console.error('Failed to read token from storage:', e);
		return null;
	}
}

function createApi(options = {}) {
	const baseURL = options.baseURL || import.meta.env.VITE_API_BASE_URL || 'http://localhost:5049/api';
	const apiAxios = axios.create({
		baseURL,
		timeout: options.timeout || 600000, // extend timeout to 10 minutes
	});

	// Attach Bearer token automatically for every request
	// Note: Token is read on each request to ensure we always use the latest token
	// (important when tokens are refreshed). The deobfuscation is fast (simple XOR).
	apiAxios.interceptors.request.use(
		(cfg) => {
			const token = readTokenFromStorage();
			if (token) {
				cfg.headers = cfg.headers || {};
				cfg.headers.Authorization = `Bearer ${token}`;
			}
			return cfg;
		},
		(err) => Promise.reject(err),
	);

	// Track whether a token refresh is already in progress to avoid concurrent refreshes
	let isRefreshing = false;
	let refreshPromise = null;

	function clearAuthStateForRelogin() {
		// Delegate auth state cleanup to the token storage layer to avoid
		// duplicating the list of auth-related keys in multiple places.
		clearAllTokens();
	}

	/**
	 * Attempt to refresh the access token using the stored refresh token.
	 * Returns true if the token was refreshed and the caller should retry.
	 */
	async function tryRefreshToken() {
		const refreshTokenValue = getToken('refreshToken', true);
		if (!refreshTokenValue) return false;

		if (isRefreshing) {
			// Another call is already refreshing – wait for it
			return refreshPromise;
		}

		isRefreshing = true;
		refreshPromise = (async () => {
			try {
				const res = await axios.post(`${baseURL}/auth/refresh`, { refreshToken: refreshTokenValue });
				const data = res?.data;
				if (data?.Success && data.AccessToken) {
					storeToken('AccessToken', data.AccessToken, true);
					if (data.RefreshToken) {
						storeToken('refreshToken', data.RefreshToken, true);
					}
					if (data.User) {
						localStorage.setItem('userData', JSON.stringify(data.User));
					}
					localStorage.setItem('lastActivity', Date.now().toString());
					return true;
				}
				return false;
			} catch {
				return false;
			} finally {
				isRefreshing = false;
				refreshPromise = null;
			}
		})();

		return refreshPromise;
	}

	// Lightweight fetch wrapper that returns response.data
	async function fetch(url, config = {}) {
		try {
			const res = await apiAxios.request({ url, ...config });
			return res.data;
		} catch (error) {
			const statusCode = error?.response?.status;

			// On 401, try refreshing the token once before giving up
			if (statusCode === 401) {
				// Avoid refreshing in a loop if the refresh call itself was the 401 source
				const isRefreshCall = url?.includes('/auth/refresh');
				if (!isRefreshCall) {
					const refreshed = await tryRefreshToken();
					if (refreshed) {
						// Retry the original request with the new token
						try {
							const retryRes = await apiAxios.request({ url, ...config });
							return retryRes.data;
						} catch (retryError) {
							// Retry also failed – fall through to redirect
						}
					}
				}
				const toast = window?.$toast;
				if (toast && typeof toast.alert === 'function') {
					toast.alert('Access denied. Please log in again.');
				}
				clearAuthStateForRelogin();
				router.push('/login');
				return null;
			}
			if (statusCode === 403) {
				const toast = window?.$toast;
				if (toast && typeof toast.alert === 'function') {
					toast.alert('Access denied. Please log in again.');
				}
				clearAuthStateForRelogin();
				router.push('/login');
				return null;
			}
			if (statusCode === 400 || statusCode === 404) {
				return null;
			}
			throw error;
		}
	}

	const get = (url, params = {}, config = {}) => fetch(url, { method: 'GET', params, ...config });

	const post = (url, data = {}, config = {}) => fetch(url, { method: 'POST', data, ...config });

	const put = (url, data = {}, config = {}) => fetch(url, { method: 'PUT', data, ...config });

	const patch = (url, data = {}, config = {}) => fetch(url, { method: 'PATCH', data, ...config });

	const del = (url, config = {}) => fetch(url, { method: 'DELETE', ...config });

	return {
		axios: apiAxios,
		fetch,
		get,
		post,
		put,
		patch,
		delete: del,
	};
}

const api = createApi();

export default function apiPlugin(component) {
	component.$api = api;
	component.api = api;
}

export { api, createApi };
