<template>
  <div class="modal fade" id="addNodeModal" tabindex="-1" aria-labelledby="addNodeModalLabel" aria-hidden="true">
    <div class="modal-dialog">
      <div class="modal-content">
        <div class="modal-header border-secondary">
          <h5 class="modal-title" id="addNodeModalLabel">
            <i class="bi bi-plus-circle me-2"></i>Add New Node
          </h5>
          <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"
            @click="resetModal"></button>
        </div>
        <div class="modal-body">
          <div class="mb-3">
            <label for="nodeNameInput" class="form-label">Node Name</label>
            <input type="text" class="form-control form-control-lg border-secondary" id="nodeNameInput"
              v-model="nodeName" @keyup.enter="createNode" placeholder="Enter node name..."
              ref="nodeNameInput" autofocus>
            <div class="form-text text-muted">
              Node will be created as a child of the currently selected node
            </div>
          </div>
          <div v-if="hierarchyInfo" class="alert alert-info mb-0">
            <i class="bi bi-info-circle me-2"></i>{{ hierarchyInfo }}
          </div>
          <div v-if="errorMessage" class="alert alert-danger mb-0 mt-2">
            <i class="bi bi-exclamation-triangle me-2"></i>{{ errorMessage }}
          </div>
        </div>
        <div class="modal-footer border-secondary">
          <button type="button" class="btn btn-secondary" data-bs-dismiss="modal" @click="resetModal"
            :disabled="isCreating">Cancel</button>
          <button type="button" class="btn btn-primary" @click="createNode"
            :disabled="!nodeName.trim() || isCreating || !canAddChild">
            <span v-if="isCreating">
              <span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
              Creating...
            </span>
            <span v-else>
              <i class="bi bi-check-lg me-1"></i>Create Node
            </span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, computed, nextTick, inject, onMounted, onBeforeUnmount } from 'vue'
import { Modal } from 'bootstrap'
import nodeApiService from '../service/nodeApiService'
import { listenEvent, triggerEvent, EVENT_TYPES } from '../rete/eventBus'

export default {
  name: 'AddNodeModal',
  setup() {
    const toast = inject('toast')
    const api = inject('api')
    nodeApiService.setApi(api)

    const nodeName = ref('')
    const nodeNameInput = ref(null)
    const isCreating = ref(false)
    const errorMessage = ref('')
    const selectedNode = ref(null)

    // Node hierarchy map: parent type -> allowed child types
    const hierarchyMap = {
      'director': { child: 'Manager', childType: 'manager' },
      'manager': { child: 'Inspector', childType: 'inspector' },
      'inspector': { child: 'Worker', childType: 'worker' },
      'worker': null // Workers cannot have children
    }

    const canAddChild = computed(() => {
      if (!selectedNode.value) return false
      const nodeType = selectedNode.value.type?.toLowerCase()
      return hierarchyMap[nodeType] !== null
    })

    const hierarchyInfo = computed(() => {
      if (!selectedNode.value) return 'Please select a node first'
      
      const nodeType = selectedNode.value.type?.toLowerCase()
      const hierarchy = hierarchyMap[nodeType]
      
      if (hierarchy) {
        return `Creating a ${hierarchy.child} node as child of ${selectedNode.value.name || 'selected node'}`
      } else {
        return `${selectedNode.value.name || 'This node'} (${selectedNode.value.type}) cannot have child nodes`
      }
    })

    const resetModal = () => {
      nodeName.value = ''
      errorMessage.value = ''
      isCreating.value = false
    }

    const createNode = async () => {
      if (!nodeName.value.trim()) {
        errorMessage.value = 'Node name is required'
        return
      }

      if (!selectedNode.value) {
        errorMessage.value = 'No node selected'
        return
      }

      if (!canAddChild.value) {
        errorMessage.value = 'Selected node cannot have children'
        return
      }

      isCreating.value = true
      errorMessage.value = ''

      try {
        const nodeType = selectedNode.value.type?.toLowerCase()
        const hierarchy = hierarchyMap[nodeType]

        // Get project ID from route or selected node
        const urlParams = new URLSearchParams(window.location.search)
        const projectId = urlParams.get('projectId')

        if (!projectId) {
          throw new Error('Project ID not found')
        }

        // Prepare node data
        const nodeData = {
          Name: nodeName.value.trim(),
          NodeType: hierarchy.child, // Use PascalCase for backend
          MessageType: 'Discussion',
          ParentId: selectedNode.value.id,
          ProjectId: parseInt(projectId),
          Status: 'Active',
          Properties: {}
        }

        // Create node via API
        const createdNode = await nodeApiService.createNode(nodeData)

        toast.success(`Node "${nodeName.value}" created successfully`)

        // Trigger event to add node to editor
        triggerEvent(EVENT_TYPES.NODE_CREATED_FROM_API, {
          nodeData: createdNode,
          parentId: selectedNode.value.id
        })

        // Close modal
        const modal = Modal.getInstance(document.getElementById('addNodeModal'))
        if (modal) {
          modal.hide()
        }

        resetModal()
      } catch (error) {
        console.error('Error creating node:', error)
        errorMessage.value = error.response?.data?.error || error.message || 'Failed to create node'
        toast.error(`Failed to create node: ${errorMessage.value}`)
      } finally {
        isCreating.value = false
      }
    }

    // Listen for selected node changes
    const unsubscribeSelectedNode = listenEvent(
      EVENT_TYPES.SELECTED_NODE_CHANGED,
      (nodeData) => {
        selectedNode.value = nodeData
      }
    )

    onMounted(() => {
      // Focus on input when modal is shown
      const modalElement = document.getElementById('addNodeModal')
      if (modalElement) {
        modalElement.addEventListener('shown.bs.modal', () => {
          nextTick(() => {
            if (nodeNameInput.value) {
              nodeNameInput.value.focus()
            }
          })
        })
      }
    })

    onBeforeUnmount(() => {
      if (typeof unsubscribeSelectedNode === 'function') {
        unsubscribeSelectedNode()
      }
    })

    return {
      nodeName,
      nodeNameInput,
      isCreating,
      errorMessage,
      hierarchyInfo,
      canAddChild,
      resetModal,
      createNode
    }
  }
}
</script>

<style scoped>
.form-control:focus {
  border-color: var(--bs-primary);
  box-shadow: 0 0 0 0.25rem rgba(var(--bs-primary-rgb), 0.25);
}
</style>
