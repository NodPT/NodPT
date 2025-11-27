<template>
  <nav class="navbar studio-navbar fixed-top p-0" ref="topBar" :data-theme="isDarkTheme ? 'dark' : 'light'">
    <div class="container-fluid d-flex align-items-center justify-content-start">

      <!-- User Menu -->
      <div class="dropdown">

        <!-- Brand -->
        <a class="navbar-brand" href="#">
          <img class="rounded-2" src="/images/logo-small.png" alt="NodPT" />
        </a>

        <button class="btn btn-sm dropdown-toggle" type="button" :aria-expanded="openMenu === 'user'"
          @click="toggleMenu('user')">
          <img v-if="userPhotoUrl" :src="userPhotoUrl" alt="Profile" class="rounded-circle me-1"
            style="width: 24px; height: 24px; object-fit: cover;" @error="userPhotoUrl = ''" />
          <i v-else class="bi bi-person-circle me-1"></i>
        </button>
        <ul class="dropdown-menu dropdown-menu-end" :class="{ show: openMenu === 'user' }">
          <li><a class="dropdown-item" href="#" @click.prevent="userAction('profile')">Profile</a></li>
          <li><a class="dropdown-item" href="#" @click.prevent="userAction('settings')">Settings</a></li>
          <li><a class="dropdown-item" href="#" @click.prevent="userAction('help')">Help</a></li>
          <li>
            <hr class="dropdown-divider" />
          </li>
          <li><a class="dropdown-item text-danger" href="#" @click.prevent="userAction('logout')">Logout</a></li>
        </ul>
      </div>



      <!-- Project / Nodes / Search -->
      <div class="d-flex align-items-center gap-3" v-if="show_menu">
        <!-- Project Menu -->
        <div class="dropdown">
          <button class="btn btn-sm dropdown-toggle" type="button" :aria-expanded="openMenu === 'project'"
            @click="toggleMenu('project')">
            <i class="bi bi-folder2"></i> File
          </button>
          <ul class="dropdown-menu" :class="{ show: openMenu === 'project' }">
            <li><a class="dropdown-item" href="#" @click.prevent="projectAction('new')">New Project</a></li>
            <li><a class="dropdown-item" href="#" @click.prevent="projectAction('open')">Open Project</a></li>
            <li><a class="dropdown-item" href="#" @click.prevent="projectAction('save')">Save Project</a></li>
            <li>
              <hr class="dropdown-divider" />
            </li>
            <li><a class="dropdown-item" href="#" @click.prevent="projectAction('copy')">Copy Project</a></li>
            <li><a class="dropdown-item" href="#" @click.prevent="projectAction('rename')">Rename Project</a></li>
            <li><a class="dropdown-item text-danger" href="#" @click.prevent="projectAction('delete')">Delete
                Project</a></li>
            <li>
              <hr class="dropdown-divider" />
            </li>
            <li>
              <div class="d-flex px-3 pb-2">
                <button class="btn btn-sm me-2 flex-fill" @click="undoAction">
                  <i class="bi bi-arrow-counterclockwise me-1"></i> Undo
                </button>
                <button class="btn btn-sm flex-fill" @click="redoAction">
                  <i class="bi bi-arrow-clockwise me-1"></i> Redo
                </button>
              </div>
            </li>
          </ul>
        </div>

        <!-- Nodes Menu -->
        <div class="dropdown">
          <button class="btn btn-sm dropdown-toggle" type="button" :aria-expanded="openMenu === 'nodes'"
            @click="toggleMenu('nodes')">
            <i class="bi bi-diagram-3"></i> Nodes
          </button>
          <ul class="dropdown-menu" :class="{ show: openMenu === 'nodes' }">
            <li><a class="dropdown-item" href="#" @click.prevent="nodeAction('add')">Add Node</a></li>
            <li><a class="dropdown-item" :class="{ disabled: isDirectorSelected }" href="#" @click.prevent="nodeAction('delete')">Delete</a></li>
            <li>
              <hr class="dropdown-divider" />
            </li>
            <li><a class="dropdown-item" href="#" @click.prevent="nodeAction('lock')">Lock</a></li>
            <li><a class="dropdown-item" href="#" @click.prevent="nodeAction('unlock')">Unlock</a></li>
          </ul>
        </div>

        <!-- Search -->
        <button class="btn btn-sm" @click="toggleSearchPopup" title="Search nodes">
          <i class="bi bi-search"></i>
        </button>

        <!-- Project Name -->
        <span v-if="projectName" class="navbar-text text-muted ms-2">
          {{ projectName }}
        </span>
      </div>
    </div>
  </nav>

  <!-- Search Popup -->
  <div v-if="searchPopupVisible" class="search-popup position-fixed w-50 shadow-lg align-self-end rounded-3"
    role="dialog" aria-label="Search nodes" style="top: var(--navbar-height, 40px); z-index: 1050; right:10px;">
    <div class="container-fluid bg-white border-bottom p-2 rounded-3">
      <div class="d-flex align-items-center">
        <div class="input-group me-2">
          <input type="text" class="form-control" placeholder="Search nodes..." v-model="searchTerm"
            @keyup.enter="handleSearch" @keyup.escape="closeSearchPopup" ref="searchInput" autofocus />
          <button class="btn btn-outline-secondary" type="button" @click="clearSearch" v-if="searchTerm"
            title="Clear search">
            <i class="bi bi-x"></i>
          </button>
        </div>
        <button class="btn btn-outline-secondary btn-sm" @click="closeSearchPopup" title="Close search">
          <i class="bi bi-x-lg"></i>
        </button>
      </div>
    </div>
  </div>

  <!-- Modals -->
  <NewProjectModal />
  <OpenProjectModal />
  <CopyProjectModal />
  <RenameProjectModal />
  <DeleteProjectModal />
</template>

<script>
import { ref, onMounted, onBeforeUnmount, nextTick, inject } from 'vue';
import { triggerEvent, listenEvent, EVENT_TYPES } from '../rete/eventBus';
import { useRouter, useRoute } from 'vue-router';
import { logout } from '../service/authService';
import authApiService from '../service/authApiService';
import NewProjectModal from './NewProjectModal.vue';
import OpenProjectModal from './OpenProjectModal.vue';
import CopyProjectModal from './CopyProjectModal.vue';
import RenameProjectModal from './RenameProjectModal.vue';
import DeleteProjectModal from './DeleteProjectModal.vue';
import { Modal } from 'bootstrap';


export default {
  name: 'TopBar',
  components: {
    NewProjectModal,
    OpenProjectModal,
    CopyProjectModal,
    RenameProjectModal,
    DeleteProjectModal
  },
  props: {
    show_menu: {
      type: Boolean,
      default: true
    },
    isDarkTheme: {
      type: Boolean,
      default: false
    }
  },
  setup() {
    const router = useRouter();
    const route = useRoute();

    const searchTerm = ref('');
    const searchResults = ref([]);
    const currentSearchIndex = ref(0);
    const hasSearched = ref(false);
    const searchInput = ref(null);
    const topBar = ref(null);
    const openMenu = ref(null);
    const projectName = ref('');
    const searchPopupVisible = ref(false);
    const userPhotoUrl = ref('');
    const isDirectorSelected = ref(false);
    const api = inject('api');

    let editorManager = null;
    const eventListeners = [];

    const handleSearch = () => {
      if (!searchTerm.value.trim()) {
        clearSearch();
        return;
      }
      hasSearched.value = true;
      triggerEvent(EVENT_TYPES.SEARCH_NODES, {
        searchTerm: searchTerm.value.trim(),
        callback: (results, manager) => {
          editorManager = manager;
          searchResults.value = results;
          currentSearchIndex.value = 0;
          if (results.length > 0) focusCurrentResult();
        }
      });
    };

    const nextResult = () => {
      if (!searchResults.value.length) return;
      currentSearchIndex.value = (currentSearchIndex.value + 1) % searchResults.value.length;
      focusCurrentResult();
    };

    const previousResult = () => {
      if (!searchResults.value.length) return;
      currentSearchIndex.value =
        currentSearchIndex.value === 0
          ? searchResults.value.length - 1
          : currentSearchIndex.value - 1;
      focusCurrentResult();
    };

    const focusCurrentResult = () => {
      if (!searchResults.value.length) return;
      const currentNode = searchResults.value[currentSearchIndex.value];
      if (currentNode) {
        triggerEvent(EVENT_TYPES.SEARCH_FOCUS_NODE, { nodeId: currentNode.node.id });
      }
    };

    /**
   * Logout user
   * @returns {Promise<Object>} API response
   */
    const logoutApi = async () => {
      try {
        const response = await api.get(`/auth/logout`);

        // Clear user data on logout
        localStorage.removeItem('userData');
        sessionStorage.removeItem('userData');

        return response.data;
      } catch (error) {
        console.error('Failed to logout:', error);
        throw error;
      }
    }

    const clearSearch = () => {
      searchTerm.value = '';
      searchResults.value = [];
      currentSearchIndex.value = 0;
      hasSearched.value = false;
      triggerEvent(EVENT_TYPES.SEARCH_CLEAR);
    };

    const toggleSearchPopup = () => {
      searchPopupVisible.value = !searchPopupVisible.value;
      if (searchPopupVisible.value) nextTick(() => searchInput.value?.focus());
    };

    const closeSearchPopup = () => {
      searchPopupVisible.value = false;
      clearSearch();
    };

    const projectAction = (action) => {
      const modalMap = {
        new: 'newProjectModal',
        open: 'openProjectModal',
        copy: 'copyProjectModal',
        rename: 'renameProjectModal',
        delete: 'deleteProjectModal'
      };
      if (modalMap[action]) {
        const modal = new Modal(document.getElementById(modalMap[action]));
        modal.show();
        return;
      }
      triggerEvent(EVENT_TYPES.PROJECT_ACTION, action);
    };

    const nodeAction = (action) => {
      if (action === 'delete') {
        // Prevent deletion of Director node
        if (isDirectorSelected.value) return;
        triggerEvent(EVENT_TYPES.DELETE_NODE);
      }
      else triggerEvent(EVENT_TYPES.NODE_ACTION, action);
    };

    const aiAction = (action) => triggerEvent(EVENT_TYPES.AI_ACTION, action);

    const userAction = async (action) => {
      if (action === 'profile') return router.push({ name: 'Profile' });
      if (action === 'logout') {
        try {
          await logoutApi();
          await logout();
        } catch (e) {
          console.error('Logout error:', e);
          try { await logout(); } catch { }
        } finally {
          router.push({ name: 'Login' });
        }
        return;
      }
      triggerEvent(EVENT_TYPES.USER_ACTION, action);
    };

    const undoAction = () => triggerEvent(EVENT_TYPES.UNDO);
    const redoAction = () => triggerEvent(EVENT_TYPES.REDO);

    const toggleMenu = (menu) => (openMenu.value = openMenu.value === menu ? null : menu);
    const closeMenu = () => (openMenu.value = null);

    const handleClickOutside = (e) => {
      if (!topBar.value) return;
      if (!topBar.value.contains(e.target)) closeMenu();
    };

    onMounted(() => {
      document.addEventListener('click', handleClickOutside, true);
      if (route.query.projectName) projectName.value = route.query.projectName;

      // Load user photo URL
      const userData = authApiService.getUserData();
      if (userData && userData.PhotoUrl) {
        userPhotoUrl.value = userData.PhotoUrl;
      }

      const unsubscribeProjectName = listenEvent(
        EVENT_TYPES.PROJECT_NAME_UPDATE,
        (name) => (projectName.value = name)
      );
      eventListeners.push(unsubscribeProjectName);

      // Listen for selected node changes to disable Delete button for Director node
      const unsubscribeSelectedNode = listenEvent(
        EVENT_TYPES.SELECTED_NODE_CHANGED,
        (nodeData) => {
          isDirectorSelected.value = nodeData && nodeData.type === 'director';
        }
      );
      eventListeners.push(unsubscribeSelectedNode);
    });

    onBeforeUnmount(() => {
      eventListeners.forEach((unsub) => typeof unsub === 'function' && unsub());
      document.removeEventListener('click', handleClickOutside, true);
    });

    return {
      searchTerm,
      searchResults,
      currentSearchIndex,
      hasSearched,
      searchInput,
      topBar,
      projectName,
      searchPopupVisible,
      userPhotoUrl,
      isDirectorSelected,
      handleSearch,
      nextResult,
      previousResult,
      clearSearch,
      toggleSearchPopup,
      closeSearchPopup,
      projectAction,
      nodeAction,
      aiAction,
      userAction,
      undoAction,
      redoAction,
      openMenu,
      toggleMenu,
      closeMenu
    };
  }
};
</script>

<style scoped>
.navbar {
  background-color: var(--rete-panel, #ffffff);
  border-bottom: 1px solid var(--rete-border, #dee2e6);
  padding: 0.5rem 1rem;
}

.dropdown-menu.show {
  display: block;
}

.search-popup {
  animation: fadeIn 0.15s ease-in;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(-4px);
  }

  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>