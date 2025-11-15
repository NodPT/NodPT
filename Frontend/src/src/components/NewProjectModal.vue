<template>
  <div class="modal fade" id="newProjectModal" tabindex="-1" aria-labelledby="newProjectModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-lg">
      <div class="modal-content">
        <div class="modal-header border-secondary">
          <h5 class="modal-title" id="newProjectModalLabel">
            <i class="bi bi-file-earmark-plus me-2"></i>{{ modalTitle }}
          </h5>
          <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"
            @click="resetModal"></button>
        </div>
        <div class="modal-body">
          <!-- Step 1: Select Template -->
          <div v-if="currentStep === 1">
            <ProjectTiles :selectionMode="true" @tile-selected="selectTemplate" />
          </div>

          <!-- Step 2: Enter Project Name -->
          <div v-if="currentStep === 2" class="project-name-form">
            <div class="mb-4 text-center">
              <div class="selected-template-icon mb-3">
                <i :class="selectedTemplate.icon" class="fs-1"></i>
              </div>
              <h6 class="text-muted">{{ selectedTemplate.title }} Project</h6>
            </div>
            <div class="mb-3">
              <label for="projectNameInput" class="form-label">Project Name</label>
              <input type="text" class="form-control form-control-lg  border-secondary" id="projectNameInput"
                v-model="projectName" @keyup.enter="createProject" placeholder="Enter project name..."
                ref="projectNameInput" autofocus>
              <div class="form-text text-muted">
                Give your project a descriptive name
              </div>
            </div>
          </div>
        </div>
        <div class="modal-footer border-secondary">
          <button type="button" class="btn btn-secondary" data-bs-dismiss="modal" @click="resetModal"
            :disabled="isCreating">Cancel</button>
          <button v-if="currentStep === 2" type="button" class="btn btn-primary" @click="createProject"
            :disabled="!projectName.trim() || isCreating">
            <span v-if="isCreating">
              <span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
              Creating...
            </span>
            <span v-else>
              <i class="bi bi-check-lg me-1"></i>Create Project
            </span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, computed, nextTick, inject, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { Modal } from 'bootstrap'
import projectApiService from '../service/projectApiService'
import ProjectTiles from './ProjectTiles.vue'
import { listenEvent, EVENT_TYPES } from '../rete/eventBus'

export default {
  name: 'NewProjectModal',
  components: {
    ProjectTiles
  },
  setup() {
    const router = useRouter()
    const toast = inject('toast')
    const api = inject('api')
    projectApiService.setApi(api)
    const currentStep = ref(1)
    const selectedTemplate = ref(null)
    const projectName = ref('')
    const projectNameInput = ref(null)
    const isCreating = ref(false)

    const modalTitle = computed(() => {
      return currentStep.value === 1 ? 'Create New Project' : 'Name Your Project'
    })

    const selectTemplate = (tile) => {
      selectedTemplate.value = tile
      currentStep.value = 2

      // Focus on the input field after the DOM updates
      nextTick(() => {
        if (projectNameInput.value) {
          projectNameInput.value.focus()
        }
      })
    }

    const resetModal = () => {
      currentStep.value = 1
      selectedTemplate.value = null
      projectName.value = ''
    }

    const createProject = async () => {
      if (!projectName.value.trim() || isCreating.value) {
        return
      }

      try {
        isCreating.value = true

        // Create project DTO matching backend structure
        const projectDto = {
          Name: projectName.value.trim(),
          Description: `${selectedTemplate.value.title} project`,
          IsActive: true,
          CreatedAt: new Date().toISOString(),
          UpdatedAt: new Date().toISOString(),
          TemplateId: selectedTemplate.value.id
        }

        // Call API to create project
        const createdProject = await projectApiService.createProject(projectDto)

        if (!createdProject || !createdProject.Id) {
          isCreating.value = false;
          toast.alert('Failed to create project. Please try again.');
          console.error('Created project is invalid:', createdProject);
          return;
        }

        // Close the modal
        const modalElement = document.getElementById('newProjectModal')
        const modal = Modal.getInstance(modalElement)
        if (modal) {
          modal.hide()
        }

        // Show success message
        toast.success(`Project "${createdProject.Name}" created successfully`)

        // Navigate to editor with the created project ID and name
        router.push({
          name: 'MainEditor',
          query: {
            projectId: createdProject.Id,
            projectName: createdProject.Name,
            isNewProject: 'true'
          }
        })

        // Reset modal for next time
        setTimeout(() => {
          resetModal()
          isCreating.value = false
        }, 500)
      } catch (error) {
        console.error('Error creating project:', error)
        toast.alert('Failed to create project. Please try again.')
        isCreating.value = false
      }
    }

    const openModalWithTile = (tile) => {
      // Open the modal
      const modalElement = document.getElementById('newProjectModal')
      if (modalElement) {
        const modal = new Modal(modalElement)
        modal.show()

        // Pre-select the tile and go directly to step 2
        selectTemplate(tile)
      }
    }

    // Listen for event to open modal with pre-selected tile
    onMounted(() => {
      const unsubscribe = listenEvent(EVENT_TYPES.OPEN_NEW_PROJECT_MODAL, openModalWithTile)

      // Cleanup on unmount
      onBeforeUnmount(() => {
        unsubscribe()
      })
    })

    return {
      currentStep,
      selectedTemplate,
      projectName,
      projectNameInput,
      modalTitle,
      selectTemplate,
      resetModal,
      createProject,
      isCreating
    }
  }
}
</script>
