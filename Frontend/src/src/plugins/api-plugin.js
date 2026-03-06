// src/plugins/api-plugin.js
import axios from 'axios';
import { router } from '@riotjs/route';
import { getToken, clearAllTokens } from './tokenStorage';

/**
 * Read the access token from sessionStorage
 * @returns {string|null} Access token or null if not found
 */
function readTokenFromStorage() {
	try {
		const token = getToken('AccessToken');
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

	function clearAuthStateForRelogin() {
		clearAllTokens();
	}

	// Lightweight fetch wrapper that returns response.data
	async function fetch(url, config = {}) {
		try {
			const res = await apiAxios.request({ url, ...config });
			return res.data;
		} catch (error) {
			const statusCode = error?.response?.status;

			if (statusCode === 401 || statusCode === 403) {
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
