<template>
  <div class="min-vh-100 d-flex align-items-center profile-bg">
    <div class="container">
      <div class="row justify-content-center">
        <div class="col-md-8 col-lg-6">
          <div class="card profile-card">
            <div class="card-body p-5">
              <div class="text-center mb-4">
                <h2 class="card-title text-white mb-2">Complete Your Profile</h2>
                <p class="text-light opacity-75">Tell us a bit about yourself to get started</p>
              </div>

              <form @submit.prevent="onSubmit">
                <div class="mb-4">
                  <label class="form-label text-white">Display Name</label>
                  <input v-model="DisplayName" type="text" class="form-control profile-input"
                    placeholder="Enter your display name" required />
                </div>

                <div class="d-grid gap-3">
                  <button class="btn btn-primary btn-lg profile-btn" type="submit" :disabled="isSaving">
                    <span v-if="!isSaving">
                      <i class="fas fa-check me-2"></i>Complete Profile
                    </span>
                    <span v-else>
                      <span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                      Saving...
                    </span>
                  </button>

                  <button class="btn btn-outline-light btn-lg" type="button" @click="onSkip">
                    Skip for Now
                  </button>
                </div>
              </form>

              <div class="text-center mt-4">
                <button class="btn btn-link text-danger p-0" @click="onDeleteAccount">
                  <i class="fas fa-trash me-1"></i>Delete Account
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, inject } from 'vue';
import { useRouter } from 'vue-router';
import { auth } from '../firebase';
import userApiService from '../service/userApiService';

// Inject API plugin
const api = inject('api');
userApiService.setApi(api);

const router = useRouter();
const DisplayName = ref('');
const isSaving = ref(false);

onMounted(() => {
  // Pre-fill with user's existing display name if available
  if (auth.currentUser?.displayName) {
    DisplayName.value = auth.currentUser.displayName;
  }
});

async function onSubmit() {
  if (!DisplayName.value.trim()) {
    alert('Please enter a display name');
    return;
  }

  isSaving.value = true;
  try {
    // Check if user is authenticated
    if (!auth.currentUser) {
      throw new Error('User not authenticated');
    }

    // Call the WebAPI to update the user profile
    // No need to send firebaseUid - backend gets it from JWT token
    await userApiService.updateProfile({
      DisplayName: DisplayName.value
    });

    console.log('Profile updated successfully');
    
    // Navigate to the project page
    router.push({ name: 'Project' });
  } catch (error) {
    console.error('Error saving profile:', error);
    alert('Error saving profile. Please try again.');
  } finally {
    isSaving.value = false;
  }
}

function onSkip() {
  router.push({ name: 'Project' });
}

function onDeleteAccount() {
  router.push({ name: 'DeleteAccount' });
}
</script>
