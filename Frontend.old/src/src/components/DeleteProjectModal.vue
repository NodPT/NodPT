<template>
  <div class="modal fade" id="deleteProjectModal" tabindex="-1" aria-labelledby="deleteProjectModalLabel"
    aria-hidden="true">
    <div class="modal-dialog">
      <div class="modal-content">
        <div class="modal-header border-danger">
          <h5 class="modal-title text-danger" id="deleteProjectModalLabel">
            <i class="bi bi-trash me-2"></i>Delete Project
          </h5>
          <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"
            @click="resetModal"></button>
        </div>
        <div class="modal-body">
          <div class="alert alert-danger delete-warning mb-3" role="alert">
            <div class="d-flex align-items-start">
              <i class="bi bi-exclamation-triangle-fill me-2 mt-1"></i>
              <div>
                <strong>Warning:</strong> This action cannot be undone. All project data will be permanently deleted.
              </div>
            </div>
          </div>
          <p class="text-muted mb-3">You are about to delete <strong>{{ currentProjectName }}</strong></p>
          <div class="mb-3">
            <label for="deleteProjectNameInput" class="form-label">
              Type <strong>{{ currentProjectName }}</strong> to confirm deletion:
            </label>
            <input type="text" class="form-control  border-danger" id="deleteProjectNameInput"
              v-model="confirmProjectName" @keyup.enter="deleteProject"
              :placeholder="`Type ${currentProjectName} to confirm`" ref="projectNameInput" autofocus>
            <div class="form-text text-muted">
              This will permanently delete the project
            </div>
          </div>
        </div>
        <div class="modal-footer border-danger">
          <button type="button" class="btn btn-secondary" data-bs-dismiss="modal" @click="resetModal"
            :disabled="isDeleting">Cancel</button>
          <button type="button" class="btn btn-danger" @click="deleteProject"
            :disabled="confirmProjectName !== currentProjectName || isDeleting">
            <span v-if="isDeleting">
              <span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
              Deleting...
            </span>
            <span v-else>
              <i class="bi bi-trash me-1"></i>Delete Project
            </span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, nextTick, inject } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Modal } from 'bootstrap'
import projectApiService from '../service/projectApiService'

export default {
  name: 'DeleteProjectModal',
  setup() {
    const toast = inject('toast')
    const api = inject('api')
    projectApiService.setApi(api)
    const route = useRoute()
    const router = useRouter()
    const confirmProjectName = ref('')
    const projectNameInput = ref(null)
    const currentProjectName = ref('')
    const currentProjectId = ref(null)
    const isDeleting = ref(false)

    const resetModal = () => {
      confirmProjectName.value = ''
    }

    const deleteProject = async () => {
      if (confirmProjectName.value !== currentProjectName.value || isDeleting.value) {
        return
      }

      try {
        isDeleting.value = true
        let deletedProject = null

        // Call API to delete project
        if (currentProjectId.value) {
          deletedProject = await projectApiService.deleteProject(currentProjectId.value);
          if (!deletedProject) {
            console.error('Project deletion failed');
            toast.alert('Failed to delete project. Please try again.')
          }
        }

        // Close the modal
        const modalElement = document.getElementById('deleteProjectModal')
        const modal = Modal.getInstance(modalElement)
        if (modal) {
          modal.hide()
        }

        if (deletedProject) { // If project was successfully deleted
          // Show success message and redirect to projects page
          toast.success(`Project "${currentProjectName.value}" has been deleted`)

          // Redirect to projects/landing page
          router.push({ name: 'Project' })
        }

        // Reset modal for next time
        setTimeout(() => {
          resetModal()
          isDeleting.value = false
        }, 500)

      } catch (error) {
        console.error('Error deleting project:', error)
        toast.alert('Failed to delete project. Please try again.')
        isDeleting.value = false
      }
    }

    // Listen for modal show event to get current project name
    const initModal = () => {
      const modalElement = document.getElementById('deleteProjectModal')
      if (modalElement) {
        modalElement.addEventListener('show.bs.modal', () => {
          // Get current project name and ID from route or default
          currentProjectName.value = route.query.projectName || 'Untitled Project'
          currentProjectId.value = route.query.projectId ? parseInt(route.query.projectId) : null
          confirmProjectName.value = ''

          // Focus on input after modal is shown
          nextTick(() => {
            if (projectNameInput.value) {
              projectNameInput.value.focus()
            }
          })
        })
      }
    }

    // Initialize modal event listener when component mounts
    if (typeof window !== 'undefined') {
      setTimeout(initModal, 100)
    }

    return {
      confirmProjectName,
      projectNameInput,
      currentProjectName,
      resetModal,
      deleteProject,
      isDeleting
    }
  }
}
</script>
