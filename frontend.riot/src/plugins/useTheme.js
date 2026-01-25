import { ref } from 'vue';

// Shared theme state across all components
const isDarkTheme = ref(true);

function applyBodyTheme() {
	if (typeof document !== 'undefined' && document.body) {
		document.body.setAttribute('data-theme', isDarkTheme.value ? 'dark' : 'light');
	}
}

export function useTheme() {
	// Load theme from localStorage on first use
	const loadTheme = () => {
		const savedTheme = localStorage.getItem('appTheme');
		isDarkTheme.value = savedTheme ? savedTheme === 'dark' : true;
		applyBodyTheme();
	};

	// Toggle theme
	const toggleTheme = () => {
		isDarkTheme.value = !isDarkTheme.value;
		localStorage.setItem('appTheme', isDarkTheme.value ? 'dark' : 'light');
		applyBodyTheme();
	};

	// Set theme explicitly
	const setTheme = (theme) => {
		isDarkTheme.value = theme === 'dark';
		localStorage.setItem('appTheme', theme);
		applyBodyTheme();
	};

	return {
		isDarkTheme,
		toggleTheme,
		setTheme,
		loadTheme,
	};
}
