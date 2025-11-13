import { createRouter, createWebHashHistory } from 'vue-router';
import MainEditor from '../views/MainEditor.vue';
import Project from '../views/Project.vue';
import LandingPage from '../views/LandingPage.vue';
import Login from '../components/Login.vue';
import Profile from '../components/Profile.vue';
import DeleteAccount from '../components/DeleteAccount.vue';
import TnC from '../components/TnC.vue';
import { auth } from '../firebase';
import { onAuthStateChanged } from 'firebase/auth';

const routes = [
	{
		path: '/',
		name: 'LandingPage',
		component: LandingPage,
	},
	{
		path: '/login',
		name: 'Login',
		component: Login,
	},
	{
		path: '/profile',
		name: 'Profile',
		component: Profile,
		meta: { requiresAuth: true },
	},
	{
		path: '/delete-account',
		name: 'DeleteAccount',
		component: DeleteAccount,
		meta: { requiresAuth: true },
	},
	{
		path: '/project',
		name: 'Project',
		component: Project,
		meta: { requiresAuth: true },
	},
	{
		path: '/editor',
		name: 'MainEditor',
		component: MainEditor,
		meta: { requiresAuth: true },
	},
	{
		path: '/terms',
		name: 'TnC',
		component: TnC,
	},
];

const router = createRouter({
	history: createWebHashHistory(),
	routes,
});

// Function to check if user is authenticated
const getCurrentUser = () => {
	return new Promise((resolve, reject) => {
		const unsubscribe = onAuthStateChanged(
			auth,
			(user) => {
				unsubscribe();
				resolve(user);
			},
			(error) => {
				unsubscribe();
				// Sanitize error before rejecting
				reject(new Error('Authentication check failed'));
			},
		);
	});
};

// Navigation guard to check authentication
router.beforeEach(async (to, from, next) => {
	// If running in QA environment, bypass authentication checks
	// Set VITE_ENV=QA in your .env file to enable this behavior
	let isQA = false;
	if (typeof import.meta !== 'undefined' && import.meta.env && import.meta.env.VITE_ENV === 'QA') {
		isQA = true;
	}
	let requiresAuth = to.matched.some((record) => record.meta.requiresAuth);

	if (isQA) {
		requiresAuth = false;
	}

	if (requiresAuth) {
		try {
			// Wait for Firebase to determine auth state
			const user = await getCurrentUser();

			if (!user) {
				// User is not authenticated, redirect to login
				next({ name: 'Login' });
			} else {
				// User is authenticated, proceed
				next();
			}
		} catch (error) {
			// Log sanitized error message only in development
			if (process.env.NODE_ENV === 'development') {
				console.error('Error checking auth state');
			}
			// On error, redirect to login for safety
			next({ name: 'Login' });
		}
	} else {
		// Route doesn't require auth, proceed
		next();
	}
});

export default router;
