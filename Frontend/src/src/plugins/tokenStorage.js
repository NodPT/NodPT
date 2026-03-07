// src/service/tokenStorage.js
// Simple token storage with basic obfuscation
// Note: This is client-side security, not true encryption.
// Real security must be handled by backend with HTTPS

/**
 * Simple XOR-based obfuscation for token storage
 * @param {string} data - Data to obfuscate
 * @param {string} key - Obfuscation key
 * @returns {string} Obfuscated data in base64
 */
function obfuscate(data, key) {
	const keyBytes = new TextEncoder().encode(key);
	const dataBytes = new TextEncoder().encode(data);
	const result = new Uint8Array(dataBytes.length);

	for (let i = 0; i < dataBytes.length; i++) {
		result[i] = dataBytes[i] ^ keyBytes[i % keyBytes.length];
	}

	return btoa(String.fromCharCode.apply(null, result));
}

/**
 * Reverse the obfuscation
 * @param {string} obfuscatedData - Obfuscated data in base64
 * @param {string} key - Obfuscation key
 * @returns {string} Original data
 */
function deobfuscate(obfuscatedData, key) {
	try {
		const keyBytes = new TextEncoder().encode(key);
		const data = atob(obfuscatedData);
		const dataBytes = new Uint8Array(data.split('').map((c) => c.charCodeAt(0)));
		const result = new Uint8Array(dataBytes.length);

		for (let i = 0; i < dataBytes.length; i++) {
			result[i] = dataBytes[i] ^ keyBytes[i % keyBytes.length];
		}

		return new TextDecoder().decode(result);
	} catch (error) {
		console.error('Failed to deobfuscate data');
		return null;
	}
}

// Use a consistent key derived from browser fingerprint
// This is basic obfuscation, not cryptographic security
const getObfuscationKey = () => {
	return navigator.userAgent + window.location.origin;
};

/**
 * Store token with basic obfuscation in sessionStorage
 * @param {string} key - Storage key
 * @param {string} value - Token value
 */
export function storeToken(key, value) {
	if (!value) return;

	const obfuscated = obfuscate(value, getObfuscationKey());
	sessionStorage.setItem(key, obfuscated);
}

/**
 * Retrieve and deobfuscate token from sessionStorage
 * @param {string} key - Storage key
 * @returns {string|null} Token value or null
 */
export function getToken(key) {
	const obfuscated = sessionStorage.getItem(key);
	if (!obfuscated) return null;

	var tk = deobfuscate(obfuscated, getObfuscationKey());
	console.log('Retrieved token for key', key, ':', tk ? '***' : 'null');
	return tk;
}

/**
 * Remove token from storage
 * @param {string} key - Storage key
 */
export function removeToken(key) {
	localStorage.removeItem(key);
	sessionStorage.removeItem(key);
}

const AUTH_TOKEN_KEYS = ['FirebaseToken', 'AccessToken', 'RefreshToken'];

/**
 * Clear all auth tokens
 */
export function clearAllTokens() {
	AUTH_TOKEN_KEYS.forEach((key) => removeToken(key));
}
