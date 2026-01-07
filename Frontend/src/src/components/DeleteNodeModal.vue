<template>
  <div class="modal fade" id="deleteNodeModal" tabindex="-1" aria-labelledby="deleteNodeModalLabel"
    aria-hidden="true">
    <div class="modal-dialog">
      <div class="modal-content">
        <div class="modal-header border-danger">
          <h5 class="modal-title text-danger" id="deleteNodeModalLabel">
            <i class="bi bi-trash me-2"></i>Delete Node
          </h5>
          <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"
            @click="resetModal"></button>
        </div>
        <div class="modal-body">
          <div class="alert alert-danger delete-warning mb-3" role="alert">
            <div class="d-flex align-items-start">
              <i class="bi bi-exclamation-triangle-fill me-2 mt-1"></i>
              <div>
                <strong>Warning:</strong> This action will permanently remove the node from the editor.
              </div>
            </div>
          </div>
          <p class="text-muted mb-3">
            You are about to delete <strong>{{ nodeName }}</strong>
          </p>
          <p v-if="errorMessage" class="text-danger">{{ errorMessage }}</p>
        </div>
        <div class="modal-footer border-danger">
          <button type="button" class="btn btn-secondary" data-bs-dismiss="modal" @click="resetModal"
            :disabled="isDeleting">Cancel</button>
          <button type="button" class="btn btn-danger" @click="deleteNode"
            :disabled="isDeleting">
            <span v-if="isDeleting">
              <span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
              Deleting...
            </span>
            <span v-else>
              <i class="bi bi-trash me-1"></i>Delete Node
            </span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, inject, onMounted, onBeforeUnmount } from 'vue'
import { Modal } from 'bootstrap'
import nodeApiService from '../service/nodeApiService'
import { listenEvent, triggerEvent, EVENT_TYPES } from '../rete/eventBus'

export default {
  name: 'DeleteNodeModal',
  setup() {
    const toast = inject('toast')
    const api = inject('api')
    nodeApiService.setApi(api)

    const nodeName = ref('')
    const nodeId = ref(null)
    const isDeleting = ref(false)
    const errorMessage = ref('')

    const resetModal = () => {
      errorMessage.value = ''
      isDeleting.value = false
    }

    const deleteNode = async () => {
      if (!nodeId.value || isDeleting.value) {
        return
      }

      isDeleting.value = true
      errorMessage.value = ''

      try {
        await nodeApiService.deleteNode(nodeId.value)

        // Trigger event to remove node from editor
        triggerEvent(EVENT_TYPES.NODE_DELETED, { nodeId: nodeId.value })

        toast.success(`Node "${nodeName.value}" deleted successfully`)

        // Close modal
        const modal = Modal.getInstance(document.getElementById('deleteNodeModal'))
        if (modal) {
          modal.hide()
        }

        resetModal()
      } catch (error) {
        console.error('Error deleting node:', error)
        const errorMsg = error.response?.data?.error || error.message || 'Unknown error'
        errorMessage.value = `Failed to delete node: ${errorMsg}`
        toast.error(errorMessage.value)
      } finally {
        isDeleting.value = false
      }
    }

    // Listen for delete node event
    const handleShowDeleteModal = (data) => {
      if (data && data.nodeId && data.nodeName) {
        nodeId.value = data.nodeId
        nodeName.value = data.nodeName

        // Show modal
        const modalElement = document.getElementById('deleteNodeModal')
        if (modalElement) {
          const modal = new Modal(modalElement)
          modal.show()
        }
      }
    }

    onMounted(() => {
      listenEvent(EVENT_TYPES.SHOW_DELETE_NODE_MODAL, handleShowDeleteModal)
    })

    return {
      nodeName,
      isDeleting,
      errorMessage,
      resetModal,
      deleteNode
    }
  }
}
</script>

<style scoped>
.delete-warning {
  border-left: 4px solid #dc3545;
}

.border-danger {
  border-color: #dc3545 !important;
}
</style>
