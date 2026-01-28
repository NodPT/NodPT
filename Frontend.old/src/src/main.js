import { createApp } from 'vue';
import App from './App.vue';
import router from './router';
import toastPlugin from './service/plugins';
import ApiPlugin from './plugins/api-plugin';
import { listenEvent, EVENT_TYPES } from './rete/eventBus';
import 'bootstrap/dist/css/bootstrap.min.css'; // ensure Bootstrap CSS is loaded for components like dropdowns
import 'bootstrap/dist/js/bootstrap.bundle.min.js';
import 'bootstrap-icons/font/bootstrap-icons.css';
import './assets/styles/rete.css';
// Import Font Awesome for status icons
import '@fortawesome/fontawesome-free/css/all.min.css';

// // Register Service Worker for PWA
// if ('serviceWorker' in navigator) {
//   window.addEventListener('load', () => {
//     navigator.serviceWorker.register('/service-worker.js')
//       .then((registration) => {
//         console.log('ServiceWorker registered: ', registration);

//         // Check for updates
//         registration.addEventListener('updatefound', () => {
//           const newWorker = registration.installing;
//           newWorker.addEventListener('statechange', () => {
//             if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
//               // New service worker available, prompt user to refresh
//               if (confirm('A new version is available. Reload to update?')) {
//                 newWorker.postMessage({ type: 'SKIP_WAITING' });
//                 window.location.reload();
//               }
//             }
//           });
//         });
//       })
//       .catch((err) => {
//         console.log('ServiceWorker registration failed: ', err);
//       });
//   });
// }

// Setup global auth lifecycle listeners
listenEvent(EVENT_TYPES.AUTH_REQUIRES_RELOGIN, () => {
  // Redirect to login page when relogin is required
  router.push({ name: 'Login' });
});

listenEvent(EVENT_TYPES.AUTH_SIGNED_OUT, () => {
  // Redirect to login page when user signs out
  router.push({ name: 'Login' });
});

const app = createApp(App);
app.use(router);
app.use(toastPlugin);
app.use(ApiPlugin);
app.mount('#app');
