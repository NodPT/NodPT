// Lightweight, framework-agnostic theme hook for the ThickClient renderer.
// The original `Frontend/` version of this file used Vue's `ref` for shared
// state, but ThickClient is a Riot.js app and does not depend on Vue.
//
// This module exposes the same `{ isDarkTheme, toggleTheme, setTheme,
// loadTheme }` surface used by callers, but `isDarkTheme` is a plain object
// with a `value` property instead of a Vue ref - so existing call sites that
// read `isDarkTheme.value` keep working without modification.

const isDarkTheme = { value: true };

function applyBodyTheme() {
	if (typeof document !== 'undefined' && document.body) {
		document.body.setAttribute('data-theme', isDarkTheme.value ? 'dark' : 'light');
	}
}

export function useTheme() {
	const loadTheme = () => {
		const savedTheme = typeof localStorage !== 'undefined'
			? localStorage.getItem('appTheme')
			: null;
		isDarkTheme.value = savedTheme ? savedTheme === 'dark' : true;
		applyBodyTheme();
	};

	const toggleTheme = () => {
		isDarkTheme.value = !isDarkTheme.value;
		try { localStorage.setItem('appTheme', isDarkTheme.value ? 'dark' : 'light'); } catch (_) {}
		applyBodyTheme();
	};

	const setTheme = (theme) => {
		isDarkTheme.value = theme === 'dark';
		try { localStorage.setItem('appTheme', theme); } catch (_) {}
		applyBodyTheme();
	};

	return {
		isDarkTheme,
		toggleTheme,
		setTheme,
		loadTheme,
	};
}
