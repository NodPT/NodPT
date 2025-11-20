<template>
  <div class="project-page" :data-theme="isDarkTheme ? 'dark' : 'light'">

    <TopBar :show_menu="false" />

    <!-- Main Content -->
    <div class="main-content container-fluid py-5">
      <div class="row g-4">
        <!-- Left Column: Recent Projects -->
        <div class="col-lg-4">
          <div class="recent-projects-container h-100 card">
            <div class="card-body">
              <RecentProjects />
            </div>
          </div>
        </div>

        <!-- Right Column: Project Templates -->
        <div class="col-lg-8">
          <div class="project-tiles-container h-100">
            <div class="card h-100">
              <div class="card-body">
                <ProjectTiles />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { onMounted } from 'vue'
import TopBar from '../components/TopBar.vue'
import RecentProjects from '../components/RecentProjects.vue'
import ProjectTiles from '../components/ProjectTiles.vue'
import { listenEvent, EVENT_TYPES } from '../rete/eventBus'
import { useRouter } from 'vue-router'
import { useTheme } from '../composables/useTheme'

export default {
  name: 'Project',
  components: {
    TopBar,
    RecentProjects,
    ProjectTiles
  },
  setup() {
    const router = useRouter()
    const { isDarkTheme, loadTheme } = useTheme()

    const handleProjectAction = (action) => {
      console.log('Project action received:', action)

      switch (action) {
        case 'new':
          // Already on the landing page, scroll to tiles or do nothing
          console.log('New project - staying on landing page')
          break
        case 'open':
          // Scroll to or highlight recent projects
          console.log('Open project - focusing on recent projects')
          const recentProjectsElement = document.querySelector('.recent-projects-container')
          if (recentProjectsElement) {
            recentProjectsElement.scrollIntoView({ behavior: 'smooth' })
          }
          break
        case 'save':
          console.log('Save not applicable on landing page')
          break
        case 'export':
          console.log('Export not applicable on landing page')
          break
        case 'build':
          console.log('Build not applicable on landing page')
          break
        case 'run':
          console.log('Run not applicable on landing page')
          break
        case 'publish':
          console.log('Publish not applicable on landing page')
          break
        default:
          console.log('Unknown project action:', action)
      }
    }

    onMounted(() => {
      // Load theme
      loadTheme()

      // Listen for project actions from TopBar
      const unsubscribeProjectAction = listenEvent(EVENT_TYPES.PROJECT_ACTION, handleProjectAction)

      // Cleanup listeners when component unmounts
      return () => {
        unsubscribeProjectAction()
      }
    })

    return {
      handleProjectAction,
      isDarkTheme
    }
  }
}
</script>

<style>
@import '../assets/styles/components-dark.css';
</style>
