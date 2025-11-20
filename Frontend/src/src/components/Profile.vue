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
                  <button class="btn btn-primary btn-lg profile-btn" type="submit">
                    <i class="fas fa-check me-2"></i>Complete Profile
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

const router = useRouter();
const api = inject('api');
const DisplayName = ref('');

onMounted(() => {
  // Pre-fill with user's existing display name if available
  if (auth.currentUser?.DisplayName) {
    DisplayName.value = auth.currentUser.DisplayName;
  }
});

async function onSubmit() {
  try {
    const user = auth.currentUser;
    if (!user) {
      alert('You must be logged in to update your profile.');
      return;
    }

    // Initialize API service
    userApiService.setApi(api);

    // Get Firebase UID
    const firebaseUid = user.uid;

    // Update profile via API
    await userApiService.updateProfile(firebaseUid, {
      DisplayName: DisplayName.value
    });

    // Route to the project page on success
    router.push({ name: 'Project' });
  } catch (error) {
    console.error('Error saving profile:', error);
    alert('Error saving profile. Please try again.');
  }
}

function onSkip() {
  router.push({ name: 'Project' });
}

function onDeleteAccount() {
  router.push({ name: 'DeleteAccount' });
}
</script>
