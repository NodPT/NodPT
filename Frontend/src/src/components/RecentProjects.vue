<template>
  <div class="recent-projects">
    <h5 class="mb-3">Recent Projects</h5>
    <div class="project-list">
      <div v-for="project in recentProjects" :key="project.Id"
        class="project-item d-flex align-items-center mb-2 py-2 px-1 hover-shadow cursor-pointer"
        @click="openProject(project)">
        <div class="project-icon me-3">
          <i :class="getCategoryIcon(project.TemplateName)" class="fs-4"></i>
        </div>
        <div class="project-details flex-grow-1">
          <h6 class="mb-0 fw-bold">{{ project.Name }}</h6>
          <small class="text-muted">{{ formatDate(project.UpdatedAt) }}</small>
        </div>
      </div>
      <div v-if="recentProjects.length === 0" class="text-center text-muted py-4">
        <i class="bi bi-folder2-open fs-1 mb-2"></i>
        <p>No recent projects found</p>
      </div>

      <div v-if="error" class="text-center text-danger py-4">
        <i class="bi bi-folder2-open fs-1 mb-2"></i>
        <p>Internal Server Error</p>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, inject } from 'vue'
import { useRouter } from 'vue-router'
import { Modal } from 'bootstrap'
import authApiService from '../service/authApiService'
import projectApiService from '../service/projectApiService'

export default {
  name: 'RecentProjects',
  setup() {
    const router = useRouter()
    const recentProjects = ref([])
    const error = ref(false);
    const api = inject('api');
    projectApiService.setApi(api);


    const getCategoryIcon = (templateName) => {
      if (!templateName) return 'bi bi-file-earmark text-secondary'

      const lowerTemplate = templateName.toLowerCase()
      const icons = {
        coding: 'bi bi-code-slash text-primary',
        writer: 'bi bi-pen text-success',
        music: 'bi bi-music-note text-info',
        video: 'bi bi-camera-video text-warning',
        default: 'bi bi-file-earmark text-secondary'
      }

      // Map template names to icon categories
      if (lowerTemplate.includes('cod') || lowerTemplate.includes('dev') || lowerTemplate.includes('program')) {
        return icons.coding
      } else if (lowerTemplate.includes('writ') || lowerTemplate.includes('text') || lowerTemplate.includes('document')) {
        return icons.writer
      } else if (lowerTemplate.includes('music') || lowerTemplate.includes('audio') || lowerTemplate.includes('sound')) {
        return icons.music
      } else if (lowerTemplate.includes('video') || lowerTemplate.includes('film') || lowerTemplate.includes('media')) {
        return icons.video
      }

      return icons.default
    }

    const formatDate = (dateString) => {
      if (!dateString) return ''

      // Parse the date string and convert to local time
      const date = new Date(dateString)
      const now = new Date()
      const diffTime = Math.abs(now - date)
      const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24))

      if (diffDays === 1) return 'Yesterday'
      if (diffDays < 7) return `${diffDays} days ago`
      if (diffDays < 30) return `${Math.ceil(diffDays / 7)} weeks ago`
      return date.toLocaleDateString()
    }

    const openProject = (project) => {
      // Close the modal if it's open
      const modalElement = document.getElementById('openProjectModal')
      if (modalElement) {
        const modal = Modal.getInstance(modalElement)
        if (modal) {
          modal.hide()
        }
      }

      // Navigate to editor with project parameters
      router.push({
        name: 'MainEditor',
        query: {
          projectId: project.Id,
          projectName: project.Name,
          templateName: project.TemplateName,
          isNewProject: 'false'
        }
      })
    }

    const loadProjects = async () => {
      try {
        // Get user data from authentication service
        const userData = authApiService.getUserData()

        if (!userData || !userData.FirebaseUid) {
          console.warn('No user data found, cannot load projects')
          return
        }

        // Fetch projects from API
        const projects = await projectApiService.getProjectsByUser(userData.FirebaseUid)

        // Filter active projects and sort by UpdatedAt (most recent first)
        recentProjects.value = projects
          .filter(p => p.IsActive)
          .sort((a, b) => new Date(b.UpdatedAt) - new Date(a.UpdatedAt))

        console.log('Recent projects loaded:', recentProjects.value)

        error.value = false; // Reset error state on success
      } catch (error) {
        console.error('Failed to load recent projects:', error)
        recentProjects.value = [];
        error.value = true;
      }
    }

    onMounted(() => {
      loadProjects()
    })

    return {
      error,
      recentProjects,
      getCategoryIcon,
      formatDate,
      openProject,
      loadProjects
    }
  }
}
</script>
