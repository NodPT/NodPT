// Toast notification plugin for Vue 3
// Provides alert, info, and warn methods with Bootstrap toast styling

class ToastService {
  constructor() {
    this.toastContainer = null;
    this.toastCounter = 0;
  }

  init() {
    // Create toast container if it doesn't exist
    if (!this.toastContainer) {
      this.toastContainer = document.createElement('div');
      this.toastContainer.className = 'toast-container position-fixed top-0 start-50 translate-middle-x p-3';
      this.toastContainer.style.zIndex = '9999';
      document.body.appendChild(this.toastContainer);
    }
  }

  showToast(message, type = 'info') {
    this.init();

    // Define colors and icons based on type
    const config = {
      success: {
        bgClass: 'bg-success',
        textClass: 'text-white',
        icon: 'bi-check-circle-fill',
        title: 'Success'
      },
      alert: {
        bgClass: 'bg-danger',
        textClass: 'text-white',
        icon: 'bi-exclamation-triangle-fill',
        title: 'Error'
      },
      error: {
        bgClass: 'bg-danger',
        textClass: 'text-white',
        icon: 'bi-exclamation-triangle-fill',
        title: 'Error'
      },
      info: {
        bgClass: 'bg-info',
        textClass: 'text-white',
        icon: 'bi-check-circle-fill',
        title: 'Info'
      },
      warn: {
        bgClass: 'bg-warning',
        textClass: 'text-dark',
        icon: 'bi-exclamation-circle-fill',
        title: 'Warning'
      }
    };

    const typeConfig = config[type] || config.info;
    this.toastCounter++;
    const toastId = `toast-${this.toastCounter}`;

    // Create toast element
    const toastElement = document.createElement('div');
    toastElement.id = toastId;
    toastElement.className = `toast ${typeConfig.bgClass} ${typeConfig.textClass}`;
    toastElement.setAttribute('role', 'alert');
    toastElement.setAttribute('aria-live', 'assertive');
    toastElement.setAttribute('aria-atomic', 'true');

    toastElement.innerHTML = `
      <div class="toast-header ${typeConfig.bgClass} ${typeConfig.textClass} border-0">
        <i class="bi ${typeConfig.icon} me-2"></i>
        <strong class="me-auto">${typeConfig.title}</strong>
        <button type="button" class="btn-close ${typeConfig.textClass === 'text-white' ? 'btn-close-white' : ''}" data-bs-dismiss="toast" aria-label="Close"></button>
      </div>
      <div class="toast-body">
        ${message}
      </div>
    `;

    this.toastContainer.appendChild(toastElement);

    // Initialize and show the toast using Bootstrap
    // Import Bootstrap Toast dynamically if not available on window
    import('bootstrap').then((bootstrap) => {
      const Toast = bootstrap.Toast || window.bootstrap?.Toast;
      if (Toast) {
        const bsToast = new Toast(toastElement, {
          autohide: true,
          delay: 3000
        });
        bsToast.show();
      }
    });

    // Remove toast element from DOM after it's hidden
    toastElement.addEventListener('hidden.bs.toast', () => {
      toastElement.remove();
    });
  }

  alert(message) {
    this.showToast(message, 'alert');
  }

  info(message) {
    this.showToast(message, 'info');
  }

  warn(message) {
    this.showToast(message, 'warn');
  }

  success(message) {
    this.showToast(message, 'success');
  }

  error(message) {
    this.showToast(message, 'error');
  }
}

// Create singleton instance
const toastService = new ToastService();
window.$toast = toastService;

// Vue plugin
export default {
  install(app) {
    // Make toast service available globally
    app.config.globalProperties.$toast = toastService;

    // Also provide it for Composition API
    app.provide('toast', toastService);
  }
};

// Export the service for direct use
export { toastService };
