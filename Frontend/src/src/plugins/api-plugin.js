// src/plugins/api-plugin.js
import axios from 'axios';
import { getToken } from '../service/tokenStorage';


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

export default {
	install(app, options = {}) {
		const baseURL = options.baseURL || import.meta.env.VITE_API_BASE_URL || 'http://localhost:5049/api';
		const apiAxios = axios.create({
			baseURL,
			timeout: options.timeout || 15000,
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

		// Lightweight fetch wrapper that returns response.data
		async function fetch(url, config = {}) {
			const res = await apiAxios.request({ url, ...config });
			return res.data;
		}

		const get = (url, params = {}, config = {}) => fetch(url, { method: 'GET', params, ...config });

		const post = (url, data = {}, config = {}) => fetch(url, { method: 'POST', data, ...config });

		const put = (url, data = {}, config = {}) => fetch(url, { method: 'PUT', data, ...config });

		const patch = (url, data = {}, config = {}) => fetch(url, { method: 'PATCH', data, ...config });

		const del = (url, config = {}) => fetch(url, { method: 'DELETE', ...config });

		const exposed = {
			axios: apiAxios,
			fetch,
			get,
			post,
			put,
			patch,
			delete: del,
		};

		app.config.globalProperties.$api = exposed;
		app.provide('api', exposed);

	},
};
