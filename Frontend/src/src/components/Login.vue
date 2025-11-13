<template>
  <div class="container-fluid min-vh-100 d-flex align-items-center p-0">
    <div class="row gx-0 w-100">
      <!-- Left visual column (hidden on small screens) -->
      <div class="col-md-6 d-none d-md-flex bg-light align-items-center justify-content-center">
        <div class="container">
          <div class="row justify-content-center w-100">
            <div class="col-11 col-lg-9 p-5">
              <p class="lead text-muted mb-4">Create powerful AI workflows with visual node-based programming.</p>

              <ul class="list-unstyled text-muted">
                <li class="d-flex align-items-start mb-3">
                  <i class="fas fa-user-tie me-3 fs-4 text-primary"></i>
                  <div>
                    <div class="fw-semibold">Intelligent automation</div>
                    <small>Agents coordinating to solve complex tasks.</small>
                  </div>
                </li>
                <li class="d-flex align-items-start mb-3">
                  <i class="fas fa-project-diagram me-3 fs-4 text-primary"></i>
                  <div>
                    <div class="fw-semibold">Visual programming</div>
                    <small>Drag-and-drop node flows for any workflow.</small>
                  </div>
                </li>
                <li class="d-flex align-items-start">
                  <i class="fas fa-rocket me-3 fs-4 text-primary"></i>
                  <div>
                    <div class="fw-semibold">Rapid deployment</div>
                    <small>From prototype to production quickly.</small>
                  </div>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </div>

      <!-- Right login column -->
      <div class="col-12 col-md-6 d-flex align-items-center justify-content-center">
        <div class="container">
          <div class="row justify-content-center w-100">
            <div class="col-11 col-sm-10 col-md-9 col-lg-7 col-xl-6">
              <div class="card shadow-sm my-5">
                <div class="card-body p-4">
                  <a href="/" class="text-decoration-none">
                    <img src="/images/logo.png" alt="NodPT Logo" class="ms-0"
                      style="height: 40px; vertical-align: middle;" />
                  </a>
                  <h2 class="h4 fw-bold mb-1">Welcome Back</h2>
                  <p class="text-muted mb-3">Sign in to your account using your social network</p>

                  <div v-if="loginMessage" :class="['alert', loginMessageClass]" role="alert">
                    {{ loginMessage }}
                  </div>

                  <div class="d-grid gap-2 mb-3">
                    <button
                      class="btn btn-outline-primary btn-lg d-flex align-items-center justify-content-center gap-2"
                      @click="onGoogle" :disabled="isLoading" aria-label="Sign in with Google">
                      <span v-if="!isLoading"><i class="fab fa-google"></i></span>
                      <span v-else class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
                      <span>Continue with Google</span>
                    </button>

                    <!-- <button
                      class="btn btn-outline-secondary btn-lg d-flex align-items-center justify-content-center gap-2"
                      @click="onMicrosoft" :disabled="isLoading" aria-label="Sign in with Microsoft">
                      <span v-if="!isLoading"><i class="fab fa-microsoft"></i></span>
                      <span v-else class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
                      <span>Continue with Microsoft</span>
                    </button>

                    <button
                      class="btn btn-outline-primary btn-lg d-flex align-items-center justify-content-center gap-2"
                      @click="onFacebook" :disabled="isLoading" aria-label="Sign in with Facebook">
                      <span v-if="!isLoading"><i class="fab fa-facebook-f"></i></span>
                      <span v-else class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
                      <span>Continue with Facebook</span>
                    </button>
                   -->
                  </div>
                  <div class="form-check mb-3">
                    <input class="form-check-input" type="checkbox" v-model="rememberMe" id="rememberMe">
                    <label class="form-check-label" for="rememberMe">
                      Remember me for 30 days
                    </label>
                  </div>

                  <p class="small text-muted">By signing in, you agree to our <router-link to="/terms">Terms and
                      Conditions</router-link>.</p>
                </div>
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
import { loginWithGoogle, loginWithFacebook, loginWithMicrosoft, notifySignIn } from '../service/authService';
import authApiService from '../service/authApiService';
import { useRouter } from 'vue-router';
import { auth } from '../firebase';
import { storeToken } from '../service/tokenStorage';

const router = useRouter();

// Reactive state
const rememberMe = ref(false);
const isLoading = ref(false);
const loginMessage = ref('');
const loginMessageClass = ref('');

// Helper function to show messages
function showMessage(message, isError = false) {
  loginMessage.value = message;
  loginMessageClass.value = isError ? 'alert-danger' : 'alert-success';

  // Clear message after 5 seconds
  setTimeout(() => {
    loginMessage.value = '';
    loginMessageClass.value = '';
  }, 5000);
}



async function handleSocialLogin(loginFunction, providerName) {
  if (isLoading.value) return;

  isLoading.value = true;
  loginMessage.value = '';

  try {
    // Step 1: Authenticate with Firebase
    const result = await loginFunction();
    const user = result.user;

    console.log(`${user}`);

    // Step 2: Get Firebase ID token
    const FirebaseToken = await user.getIdToken();

    // Step 3: Send token to backend API for validation
    const apiResponse = await authApiService.login(FirebaseToken, rememberMe.value);

    // Show success message
    showMessage('Login successful! Redirecting...', false);

    // Step 4: Store tokens securely with obfuscation
    storeToken('FirebaseToken', FirebaseToken, rememberMe.value);
    if (apiResponse.AccessToken) {
      storeToken('AccessToken', apiResponse.AccessToken, rememberMe.value);
    }
    if (apiResponse.refreshToken && rememberMe.value) {
      storeToken('refreshToken', apiResponse.refreshToken, true);
    }

    // Step 5: Notify that user has signed in (triggers SignalR connection)
    notifySignIn();

    // Step 6: Navigate to project page
    setTimeout(() => {
      router.push({ name: 'Project' });
    }, 1000);

  } catch (err) {
    console.error(`${providerName} login error:`, err);

    // Show appropriate error message
    if (err.response && err.response.data && err.response.data.message) {
      showMessage(`Login failed: ${err.response.data.message}`, true);
    } else if (providerName === 'Google') {
      showMessage(`Login failed: ${err.message}`, true);
    } else {
      showMessage(`${providerName} login is not fully configured yet. Please use Google login.`, true);
    }
  } finally {
    isLoading.value = false;
  }
}

async function onGoogle() {
  await handleSocialLogin(loginWithGoogle, 'Google');
}

async function onMicrosoft() {
  await handleSocialLogin(loginWithMicrosoft, 'Microsoft');
}

async function onFacebook() {
  await handleSocialLogin(loginWithFacebook, 'Facebook');
}
</script>
