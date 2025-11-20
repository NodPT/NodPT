import { ref } from 'vue';

// Shared theme state across all components
const isDarkTheme = ref(true);

export function useTheme() {
  // Load theme from localStorage on first use
  const loadTheme = () => {
    const savedTheme = localStorage.getItem('appTheme');
    isDarkTheme.value = savedTheme ? savedTheme === 'dark' : true;
  };

  // Toggle theme
  const toggleTheme = () => {
    isDarkTheme.value = !isDarkTheme.value;
    localStorage.setItem('appTheme', isDarkTheme.value ? 'dark' : 'light');
  };

  // Set theme explicitly
  const setTheme = (theme) => {
    isDarkTheme.value = theme === 'dark';
    localStorage.setItem('appTheme', theme);
  };

  return {
    isDarkTheme,
    toggleTheme,
    setTheme,
    loadTheme
  };
}
