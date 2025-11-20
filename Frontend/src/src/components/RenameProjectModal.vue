<template>
  <div class="modal fade" id="renameProjectModal" tabindex="-1" aria-labelledby="renameProjectModalLabel"
    aria-hidden="true">
    <div class="modal-dialog">
      <div class="modal-content">
        <div class="modal-header border-secondary">
          <h5 class="modal-title" id="renameProjectModalLabel">
            <i class="bi bi-pencil-square me-2"></i>Rename Project
          </h5>
          <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"
            @click="resetModal"></button>
        </div>
        <div class="modal-body">
          <p class="text-muted mb-3">Rename <strong>{{ currentProjectName }}</strong></p>
          <div class="mb-3">
            <label for="renameProjectNameInput" class="form-label">New Project Name</label>
            <input type="text" class="form-control  border-secondary" id="renameProjectNameInput"
              v-model="newProjectName" @keyup.enter="renameProject" placeholder="Enter new project name..."
              ref="projectNameInput" autofocus>
            <div class="form-text text-muted">
              Enter a new name for this project
            </div>
          </div>
        </div>
        <div class="modal-footer border-secondary">
          <button type="button" class="btn btn-secondary" data-bs-dismiss="modal" @click="resetModal" :disabled="isRenaming">Cancel</button>
          <button type="button" class="btn btn-primary" @click="renameProject" :disabled="!newProjectName.trim() || isRenaming">
            <span v-if="isRenaming">
              <span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
              Renaming...
            </span>
            <span v-else>
              <i class="bi bi-pencil-square me-1"></i>Rename Project
            </span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, nextTick, inject } from 'vue'
import { useRoute } from 'vue-router'
import { Modal } from 'bootstrap'
import { triggerEvent, EVENT_TYPES } from '../rete/eventBus'
import projectApiService from '../service/projectApiService'

export default {
  name: 'RenameProjectModal',
  setup() {
    const toast = inject('toast')
    const api = inject('api')
    projectApiService.setApi(api)
    const route = useRoute()
    const newProjectName = ref('')
    const projectNameInput = ref(null)
    const currentProjectName = ref('')
    const currentProjectId = ref(null)
    const isRenaming = ref(false)

    const resetModal = () => {
      newProjectName.value = ''
    }

    const renameProject = async () => {
      if (!newProjectName.value.trim() || isRenaming.value) {
        return
      }

      try {
        isRenaming.value = true

        // Call API to rename project
        if (currentProjectId.value) {
          const updatedProject = await projectApiService.updateProjectName(currentProjectId.value, newProjectName.value.trim());
          
          // Close the modal
          const modalElement = document.getElementById('renameProjectModal')
          const modal = Modal.getInstance(modalElement)
          if (modal) {
            modal.hide()
          }

          // Update the project name in the TopBar
          triggerEvent(EVENT_TYPES.PROJECT_NAME_UPDATE, newProjectName.value.trim())

          // Show success message
          toast.success(`Project renamed to "${newProjectName.value.trim()}"`)
        }

        // Reset modal for next time
        setTimeout(() => {
          resetModal()
          isRenaming.value = false
        }, 500)

      } catch (error) {
        console.error('Error renaming project:', error)
        toast.alert('Failed to rename project. Please try again.')
        isRenaming.value = false
      }
    }

    // Listen for modal show event to get current project name
    const initModal = () => {
      const modalElement = document.getElementById('renameProjectModal')
      if (modalElement) {
        modalElement.addEventListener('show.bs.modal', () => {
          // Get current project name and ID from route or default
          currentProjectName.value = route.query.projectName || 'Untitled Project'
          currentProjectId.value = route.query.projectId ? parseInt(route.query.projectId) : null
          newProjectName.value = currentProjectName.value

          // Focus on input after modal is shown
          nextTick(() => {
            if (projectNameInput.value) {
              projectNameInput.value.focus()
              projectNameInput.value.select()
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
      newProjectName,
      projectNameInput,
      currentProjectName,
      resetModal,
      renameProject,
      isRenaming
    }
  }
}
</script>
