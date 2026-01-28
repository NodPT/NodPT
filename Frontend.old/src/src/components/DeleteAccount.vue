<template>
  <div class="min-vh-100 d-flex align-items-center delete-bg">
    <div class="container">
      <div class="row justify-content-center">
        <div class="col-md-6 col-lg-5">
          <div class="card delete-card">
            <div class="card-body p-5">
              <div class="text-center mb-4">
                <div class="warning-icon mb-3">
                  <i class="fas fa-exclamation-triangle"></i>
                </div>
                <h2 class="card-title text-white mb-2">Delete Account</h2>
                <p class="text-light opacity-75">
                  This action cannot be undone. All your data will be permanently deleted.
                </p>
              </div>

              <div class="alert alert-danger delete-warning mb-4" role="alert">
                <div class="d-flex align-items-start">
                  <i class="fas fa-info-circle me-2 mt-1"></i>
                  <div>
                    <strong>What will be deleted:</strong>
                    <ul class="mb-0 mt-2 ps-3">
                      <li>Your profile information</li>
                      <li>All your projects and workflows</li>
                      <li>Your account settings</li>
                      <li>All associated data</li>
                    </ul>
                  </div>
                </div>
              </div>

              <form @submit.prevent="onConfirmDelete">
                <div class="mb-4">
                  <label class="form-label text-white">
                    Type <strong>"DELETE"</strong> to confirm:
                  </label>
                  <input v-model="confirmationText" type="text" class="form-control delete-input"
                    placeholder="Type DELETE to confirm" required />
                </div>

                <div class="d-grid gap-3">
                  <button class="btn btn-danger btn-lg delete-btn" type="submit"
                    :disabled="confirmationText !== 'DELETE'">
                    <i class="fas fa-trash me-2"></i>
                    {{ isDeleting ? 'Deleting Account...' : 'Delete My Account' }}
                  </button>

                  <button class="btn btn-secondary btn-lg" type="button" @click="onCancel" :disabled="isDeleting">
                    Cancel
                  </button>
                </div>
              </form>

              <div class="text-center mt-4">
                <small class="text-light opacity-50">
                  Need help? <a href="#" class="text-info">Contact Support</a>
                </small>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { auth } from '../firebase';
import { logout } from '../service/authService';

const router = useRouter();
const confirmationText = ref('');
const isDeleting = ref(false);

async function onConfirmDelete() {
  if (confirmationText.value !== 'DELETE') {
    alert('Please type "DELETE" to confirm account deletion.');
    return;
  }

  isDeleting.value = true;

  try {
    // TODO: Call backend API to delete user account when implemented
    console.log('Deleting user account:', auth.currentUser?.uid);

    // For now, just logout and show success message
    await logout();
    alert('Account deletion requested. You will be redirected to the login page.');
    router.push({ name: 'Login' });
  } catch (error) {
    console.error('Error deleting account:', error);
    alert('Error deleting account. Please try again or contact support.');
  } finally {
    isDeleting.value = false;
  }
}

function onCancel() {
  router.push({ name: 'Profile' });
}
</script>
