// src/firebase.js
import { initializeApp } from 'firebase/app';
import { getAuth, GoogleAuthProvider, FacebookAuthProvider, OAuthProvider, signOut as firebaseSignOut } from 'firebase/auth';
import { triggerEvent, EVENT_TYPES } from './rete/eventBus';

// console.log('Firebase config string:', import.meta.env.VITE_FIREBASE_SHIT);
const firebaseConfig = JSON.parse(import.meta.env.VITE_FIREBASE_SHIT);

// --- Safe Firebase initialization ---
let app = null;
let auth = null;

try {
	const valid = firebaseConfig.apiKey && firebaseConfig.authDomain && firebaseConfig.projectId && firebaseConfig.appId;

	if (!valid) {
		console.warn('⚠️ Firebase configuration is incomplete. Auth will be disabled.');
	} else {
		app = initializeApp(firebaseConfig);
		auth = getAuth(app);
	}
} catch (err) {
	console.error('Firebase initialization failed:', err);
	// fallback to a null auth object so other parts won’t crash
	app = null;
	auth = null;
}

// --- Providers (these can exist even if auth is null) ---
export const googleProvider = new GoogleAuthProvider();
export const facebookProvider = new FacebookAuthProvider();
export const microsoftProvider = new OAuthProvider('microsoft.com');

// export auth safely (may be null)
export { auth };

// --- Helper constants ---
const TOKEN_EXPIRY_THRESHOLD = 5 * 60 * 1000; // 5 minutes

// --- Auth functions ---
export async function getFreshIdToken() {
	if (!auth || !auth.currentUser) {
		console.warn('Firebase auth unavailable or user not logged in.');
		return null; // safely ignore
	}

	try {
		const tokenResult = await auth.currentUser.getIdTokenResult();
		const expirationTime = new Date(tokenResult.expirationTime).getTime();
		const timeUntilExpiry = expirationTime - Date.now();

		if (timeUntilExpiry <= TOKEN_EXPIRY_THRESHOLD) {
			return await auth.currentUser.getIdToken(true);
		}
		return tokenResult.token;
	} catch (error) {
		if (error.code === 'auth/user-token-expired' || error.code === 'auth/id-token-revoked' || error.code === 'auth/user-disabled') {
			triggerEvent(EVENT_TYPES.AUTH_REQUIRES_RELOGIN, { reason: error.code });
			return null;
		}
		console.error('Error getting token:', error);
		return null;
	}
}

export async function signOutAll() {
	if (!auth) {
		console.warn('Firebase not initialized; skipping sign-out.');
		return;
	}

	try {
		localStorage.removeItem('AccessToken');
		localStorage.removeItem('refreshToken');
		sessionStorage.removeItem('AccessToken');
		sessionStorage.removeItem('refreshToken');

		await firebaseSignOut(auth);
		triggerEvent(EVENT_TYPES.AUTH_SIGNED_OUT);
	} catch (error) {
		console.error('Error during sign out:', error);
	}
}
