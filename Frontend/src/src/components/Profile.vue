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

                <div class="mb-4">
                  <label class="form-label text-white">Bio (Optional)</label>
                  <textarea v-model="bio" class="form-control profile-input" rows="3"
                    placeholder="Tell us about yourself..."></textarea>
                </div>

                <div class="mb-4">
                  <label class="form-label text-white">Company (Optional)</label>
                  <input v-model="company" type="text" class="form-control profile-input"
                    placeholder="Your company name" />
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
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { auth } from '../firebase';

const router = useRouter();
const DisplayName = ref('');
const bio = ref('');
const company = ref('');

onMounted(() => {
  // Pre-fill with user's existing display name if available
  if (auth.currentUser?.DisplayName) {
    DisplayName.value = auth.currentUser.DisplayName;
  }
});

async function onSubmit() {
  try {
    // TODO: Save profile data to backend when implemented
    console.log('Profile data:', {
      DisplayName: DisplayName.value,
      bio: bio.value,
      company: company.value
    });

    // For now, just route to the project page
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
