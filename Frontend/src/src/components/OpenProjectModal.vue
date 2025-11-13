<template>
  <div class="modal fade" id="openProjectModal" tabindex="-1" aria-labelledby="openProjectModalLabel"
    aria-hidden="true" ref="modalElement">
    <div class="modal-dialog modal-lg">
      <div class="modal-content">
        <div class="modal-header border-secondary">
          <h5 class="modal-title" id="openProjectModalLabel">
            <i class="bi bi-folder2-open me-2"></i>Open Recent Project
          </h5>
          <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
        </div>
        <div class="modal-body">
          <RecentProjects ref="recentProjectsRef" />
        </div>
        <div class="modal-footer border-secondary">
          <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, onBeforeUnmount } from 'vue'
import RecentProjects from './RecentProjects.vue'

export default {
  name: 'OpenProjectModal',
  components: {
    RecentProjects
  },
  setup() {
    const modalElement = ref(null)
    const recentProjectsRef = ref(null)

    const handleModalShown = () => {
      // Reload projects when modal is shown
      if (recentProjectsRef.value && typeof recentProjectsRef.value.loadProjects === 'function') {
        recentProjectsRef.value.loadProjects()
      }
    }

    onMounted(() => {
      if (modalElement.value) {
        modalElement.value.addEventListener('shown.bs.modal', handleModalShown)
      }
    })

    onBeforeUnmount(() => {
      if (modalElement.value) {
        modalElement.value.removeEventListener('shown.bs.modal', handleModalShown)
      }
    })

    return {
      modalElement,
      recentProjectsRef
    }
  }
}
</script>
