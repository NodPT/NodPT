<template>
  <div class="container-fluid vh-100 d-flex flex-column">
    <div class="row flex-grow-1">
      <div class="col-12">
        <div class="tnc-container p-4">
          <div class="d-flex justify-content-between align-items-center mb-4">
            <h1 class="h2">Terms and Conditions</h1>
            <router-link to="/" class="btn btn-outline-primary">
              <i class="fas fa-arrow-left"></i> Back to Home
            </router-link>
          </div>

          <div class="tnc-content">
            <div class="mb-4">
              <p class="text-muted">Last updated: <strong>{{ lastUpdated }}</strong></p>
            </div>

            <section class="mb-4">
              <h3>1. Agreement to Terms</h3>
              <p>
                By accessing and using NodPT ("Service"), you accept and agree to be bound by the terms and provision of
                this agreement.
                If you do not agree to abide by the above, please do not use this service.
              </p>
            </section>

            <section class="mb-4">
              <h3>2. Description of Service</h3>
              <p>
                NodPT is a visual AI-assisted workflow editor that allows users to create, edit, and manage node-based
                workflows.
                The service includes features for project management, AI assistance, and collaborative workflow
                development.
              </p>
            </section>

            <section class="mb-4">
              <h3>3. User Accounts</h3>
              <p>
                When you create an account with us, you must provide information that is accurate, complete, and current
                at all times.
                You are responsible for safeguarding the password and for all activities that occur under your account.
              </p>
            </section>

            <section class="mb-4">
              <h3>4. Acceptable Use</h3>
              <p>You may not use our service:</p>
              <ul>
                <li>For any unlawful purpose or to solicit others to perform unlawful acts</li>
                <li>To violate any international, federal, provincial, or state regulations, rules, laws, or local
                  ordinances</li>
                <li>To infringe upon or violate our intellectual property rights or the intellectual property rights of
                  others</li>
                <li>To harass, abuse, insult, harm, defame, slander, disparage, intimidate, or discriminate</li>
                <li>To submit false or misleading information</li>
              </ul>
            </section>

            <section class="mb-4">
              <h3>5. Privacy Policy</h3>
              <p>
                Your privacy is important to us. We collect and use your information in accordance with our Privacy
                Policy.
                By using our service, you consent to the collection and use of information as outlined in our Privacy
                Policy.
              </p>
            </section>

            <section class="mb-4">
              <h3>6. Intellectual Property Rights</h3>
              <p>
                The service and its original content, features, and functionality are and will remain the exclusive
                property of NodPT and its licensors.
                The service is protected by copyright, trademark, and other laws.
              </p>
            </section>

            <section class="mb-4">
              <h3>7. User Content</h3>
              <p>
                You retain ownership of any content you create using our service. However, by using our service, you
                grant us a license to use,
                modify, publicly perform, publicly display, reproduce, and distribute such content solely for the
                purpose of providing the service.
              </p>
            </section>

            <section class="mb-4">
              <h3>8. Service Availability</h3>
              <p>
                We strive to provide continuous service availability, but cannot guarantee uninterrupted access.
                We reserve the right to modify, suspend, or discontinue the service at any time without notice.
              </p>
            </section>

            <section class="mb-4">
              <h3>9. Limitation of Liability</h3>
              <p>
                In no event shall NodPT, nor its directors, employees, partners, agents, suppliers, or affiliates, be
                liable for any indirect,
                incidental, special, consequential, or punitive damages, including without limitation, loss of profits,
                data, use, goodwill,
                or other intangible losses, resulting from your use of the service.
              </p>
            </section>

            <section class="mb-4">
              <h3>10. Termination</h3>
              <p>
                We may terminate or suspend your account and bar access to the service immediately, without prior notice
                or liability,
                under our sole discretion, for any reason whatsoever, including without limitation if you breach the
                Terms.
              </p>
            </section>

            <section class="mb-4">
              <h3>11. Changes to Terms</h3>
              <p>
                We reserve the right, at our sole discretion, to modify or replace these Terms at any time.
                If a revision is material, we will provide at least 30 days notice prior to any new terms taking effect.
              </p>
            </section>

            <section class="mb-4">
              <h3>12. Contact Information</h3>
              <p>
                If you have any questions about these Terms and Conditions, please contact us at:
              </p>
              <ul class="list-unstyled">
                <li><i class="fas fa-envelope"></i> Email: support@NodPT.com</li>
                <li><i class="fas fa-globe"></i> Website: https://NodPT.com</li>
              </ul>
            </section>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'

const lastUpdated = ref(new Date().toLocaleDateString('en-US', {
  year: 'numeric',
  month: 'long',
  day: 'numeric'
}))
</script>
