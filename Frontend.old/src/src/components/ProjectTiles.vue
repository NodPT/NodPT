<template>
  <div class="project-tiles">
    <h5 class="mb-3">Create New Project</h5>
    <div v-if="loading" class="text-center py-4">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Loading templates...</span>
      </div>
      <p class="text-muted mt-2">Loading templates...</p>
    </div>
    <div v-else class="row g-2">
      <div v-for="tile in projectTiles" :key="tile.id" class="col-6 col-md-3 col-lg-2"
        :class="{ 'opacity-50': tile.disabled }">
        <div class="tile-card card h-100 border-0 shadow-sm hover-effect cursor-pointer" @click="createProject(tile)">
          <div class="card-body text-center p-2">
            <div class="tile-icon mb-2">
              <i :class="tile.icon" class="fs-3"></i>
            </div>
            <h6 class="card-title fw-bold mb-1 small">{{ tile.name }}</h6>
            <p class="card-text text-muted" style="font-size: 0.7rem; line-height: 1.2;">{{ tile.description }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, inject, onMounted } from 'vue'
import { triggerEvent, EVENT_TYPES } from '../rete/eventBus'
import templateApiService from '../service/templateApiService'

export default {
  name: 'ProjectTiles',
  props: {
    selectionMode: {
      type: Boolean,
      default: false
    }
  },
  emits: ['tile-selected'],
  setup(props, { emit }) {
    const toast = inject('toast')
    const api = inject('api')
    const projectTiles = ref([])
    const loading = ref(true)

    // Set API for template service
    templateApiService.setApi(api)

    // Fetch templates from API
    const loadTemplates = async () => {
      try {
        loading.value = true
        const templates = await templateApiService.getTemplates()
        if (!templates || !Array.isArray(templates)) {
          throw new Error('Invalid templates data received from server.')
        }
        // Map templates to tiles format
        projectTiles.value = templates
          .filter(t => t.IsActive)
          .map(template => ({
            id: template.Id,
            category: template.Category,
            name: template.Name,
            description: template.Description,
            icon: template.Icon || 'bi bi-file-earmark text-secondary',
            disabled: false
          }))

        // Add placeholder tiles for future template types (using negative IDs to avoid conflicts)
        const additionalTiles = [
          {
            id: -1,
            category: 'music',
            name: 'Music Composer',
            description: 'Compose melodies, arrange tracks, and produce music',
            icon: 'bi bi-music-note text-info',
            disabled: true
          },
          {
            id: -2,
            category: 'video',
            name: 'Video Creator',
            description: 'Edit videos, create animations, and visual content',
            icon: 'bi bi-camera-video text-warning',
            disabled: true
          },
          {
            id: -3,
            category: 'data',
            name: 'Data Analysis',
            description: 'Process, analyze, and visualize data insights',
            icon: 'bi bi-graph-up text-danger',
            disabled: true
          },
          {
            id: -4,
            category: 'ai',
            name: 'AI Assistant',
            description: 'Build intelligent bots and AI-powered applications',
            icon: 'bi bi-robot text-purple',
            disabled: true
          },
          {
            id: -5,
            category: 'design',
            name: 'Design Studio',
            description: 'Create graphics, UI/UX designs, and visual assets',
            icon: 'bi bi-palette text-pink',
            disabled: true
          },
          {
            id: -6,
            category: 'automation',
            name: 'Automation',
            description: 'Automate tasks, workflows, and business processes',
            icon: 'bi bi-gear text-secondary',
            disabled: true
          },
          {
            id: -7,
            category: 'research',
            name: 'Research',
            description: 'Gather information, analyze trends, and generate reports',
            icon: 'bi bi-search text-dark',
            disabled: true
          }
        ]

        projectTiles.value.push(...additionalTiles)
      } catch (error) {
        console.error('Error loading templates:', error)
        const errorMessage = error && (error.message || String(error))
        toast.alert(`Failed to load templates: ${errorMessage || 'Please try again.'}`)
      } finally {
        loading.value = false
      }
    }

    onMounted(() => {
      loadTemplates()
    })

    const createProject = (tile) => {

      if (tile.disabled) {
        toast.info('This project type is coming soon!')
        return
      }

      console.log('Project tile selected:', tile)

      // If in selection mode, emit event to parent (NewProjectModal)
      if (props.selectionMode) {
        emit('tile-selected', tile)
        return
      }

      // When not in selection mode (i.e., on Project.vue page),
      // trigger an event to open the new project modal with pre-selected tile
      triggerEvent(EVENT_TYPES.OPEN_NEW_PROJECT_MODAL, tile)
    }

    return {
      projectTiles,
      createProject,
      loading
    }
  }
}
</script>
