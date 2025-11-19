<template>
  <div class="project-tiles">
    <h5 class="mb-3">Create New Project</h5>
    <div class="row g-2">
      <div v-for="tile in projectTiles" :key="tile.id" class="col-6 col-md-3 col-lg-2"
        :class="{ 'opacity-50': tile.disabled }">
        <div class="tile-card card h-100 border-0 shadow-sm hover-effect cursor-pointer" @click="createProject(tile)">
          <div class="card-body text-center p-2">
            <div class="tile-icon mb-2">
              <i :class="tile.icon" class="fs-3"></i>
            </div>
            <h6 class="card-title fw-bold mb-1 small">{{ tile.title }}</h6>
            <p class="card-text text-muted" style="font-size: 0.7rem; line-height: 1.2;">{{ tile.description }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, inject } from 'vue'
import { triggerEvent, EVENT_TYPES } from '../rete/eventBus'

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
    const projectTiles = ref([
      {
        id: 1,
        name: 'coding',
        title: 'Coding',
        description: 'Build applications, APIs, and automation workflows',
        icon: 'bi bi-code-slash text-primary'
      },
      {
        id: 2,
        name: 'writer',
        title: 'Novel Writer',
        description: 'Create stories, books, and creative content',
        icon: 'bi bi-pen text-success',
        disabled: true
      },
      {
        id: 3,
        name: 'music',
        title: 'Music Composer',
        description: 'Compose melodies, arrange tracks, and produce music',
        icon: 'bi bi-music-note text-info', disabled: true
      },
      {
        id: 4,
        name: 'video',
        title: 'Video Creator',
        description: 'Edit videos, create animations, and visual content',
        icon: 'bi bi-camera-video text-warning', disabled: true
      },
      {
        id: 5,
        name: 'data',
        title: 'Data Analysis',
        description: 'Process, analyze, and visualize data insights',
        icon: 'bi bi-graph-up text-danger', disabled: true
      },
      {
        id: 6,
        name: 'ai',
        title: 'AI Assistant',
        description: 'Build intelligent bots and AI-powered applications',
        icon: 'bi bi-robot text-purple', disabled: true
      },
      {
        id: 7,
        name: 'design',
        title: 'Design Studio',
        description: 'Create graphics, UI/UX designs, and visual assets',
        icon: 'bi bi-palette text-pink', disabled: true
      },
      {
        id: 8,
        name: 'automation',
        title: 'Automation',
        description: 'Automate tasks, workflows, and business processes',
        icon: 'bi bi-gear text-secondary', disabled: true
      },
      {
        id: 9,
        name: 'research',
        title: 'Research',
        description: 'Gather information, analyze trends, and generate reports',
        icon: 'bi bi-search text-dark', disabled: true
      }
    ])

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
      createProject
    }
  }
}
</script>
