# API Plugin Usage Examples

This file contains practical examples of using the API plugin in different scenarios.

## Example 1: Simple Component Using Composition API

```vue
<template>
  <div class="project-list">
    <h2>My Projects</h2>
    <ul v-if="projects.length">
      <li v-for="project in projects" :key="project.id">
        {{ project.name }}
      </li>
    </ul>
    <p v-else>No projects found</p>
  </div>
</template>

<script>
import { ref, inject, onMounted } from 'vue';

export default {
  name: 'ProjectList',
  setup() {
    const api = inject('api');
    const projects = ref([]);
    
    const loadProjects = async () => {
      try {
        // API call returns data directly, no need for .data
        projects.value = await api.get('/projects');
      } catch (error) {
        console.error('Failed to load projects:', error);
      }
    };
    
    onMounted(() => {
      loadProjects();
    });
    
    return { projects };
  }
};
</script>
```

## Example 2: Component Using Options API

```vue
<template>
  <div class="create-project">
    <input v-model="projectName" placeholder="Project name" />
    <button @click="createProject">Create</button>
  </div>
</template>

<script>
export default {
  name: 'CreateProject',
  data() {
    return {
      projectName: ''
    };
  },
  methods: {
    async createProject() {
      try {
        const newProject = await this.$api.post('/projects', {
          name: this.projectName
        });
        console.log('Project created:', newProject);
        this.projectName = '';
      } catch (error) {
        console.error('Failed to create project:', error);
      }
    }
  }
};
</script>
```

## Example 3: Service Class Using API Plugin

```javascript
// src/service/myApiService.js
class MyApiService {
  constructor() {
    this.api = null;
  }
  
  // Called by the plugin in main.js
  setApi(apiInstance) {
    this.api = apiInstance;
  }
  
  async getItems(userId) {
    if (!this.api) {
      throw new Error('API plugin not initialized');
    }
    // Returns data directly
    return await this.api.get(`/items/user/${userId}`);
  }
  
  async createItem(itemData) {
    if (!this.api) {
      throw new Error('API plugin not initialized');
    }
    return await this.api.post('/items', itemData);
  }
  
  async updateItem(itemId, updates) {
    if (!this.api) {
      throw new Error('API plugin not initialized');
    }
    return await this.api.put(`/items/${itemId}`, updates);
  }
  
  async deleteItem(itemId) {
    if (!this.api) {
      throw new Error('API plugin not initialized');
    }
    return await this.api.delete(`/items/${itemId}`);
  }
}

export default new MyApiService();
```

Then in `src/plugins/api-plugin.js`, import and initialize:

```javascript
import myApiService from '../service/myApiService';

export default {
  install(app, options = {}) {
    // ... existing plugin code ...
    
    // Initialize your service
    myApiService.setApi(exposed);
  }
};
```

## Example 4: Advanced Usage with Custom Headers

```javascript
import { inject } from 'vue';

export default {
  setup() {
    const api = inject('api');
    
    const uploadFile = async (file) => {
      const formData = new FormData();
      formData.append('file', file);
      
      return await api.post('/upload', formData, {
        headers: {
          'Content-Type': 'multipart/form-data'
        }
      });
    };
    
    const downloadFile = async (fileId) => {
      return await api.fetch(`/files/${fileId}`, {
        method: 'GET',
        responseType: 'blob'
      });
    };
    
    return { uploadFile, downloadFile };
  }
};
```

## Example 5: Using with Error Handling and Loading State

```vue
<template>
  <div>
    <div v-if="loading">Loading...</div>
    <div v-else-if="error">Error: {{ error }}</div>
    <div v-else>
      <h2>Projects</h2>
      <ul>
        <li v-for="project in projects" :key="project.id">
          {{ project.name }}
        </li>
      </ul>
    </div>
  </div>
</template>

<script>
import { ref, inject, onMounted } from 'vue';

export default {
  setup() {
    const api = inject('api');
    const projects = ref([]);
    const loading = ref(false);
    const error = ref(null);
    
    const loadProjects = async () => {
      loading.value = true;
      error.value = null;
      
      try {
        projects.value = await api.get('/projects');
      } catch (err) {
        error.value = err.message;
        // Handle specific error codes
        if (err.response?.status === 401) {
          console.log('Unauthorized - redirect to login');
        } else if (err.response?.status === 403) {
          console.log('Forbidden - insufficient permissions');
        }
      } finally {
        loading.value = false;
      }
    };
    
    onMounted(() => {
      loadProjects();
    });
    
    return { projects, loading, error };
  }
};
</script>
```

## Example 6: Existing projectApiService Integration

The `projectApiService` has been updated to use the API plugin:

```javascript
import projectApiService from '@/service/projectApiService';

// In any component (Composition API)
import { inject, onMounted } from 'vue';

export default {
  setup() {
    onMounted(async () => {
      try {
        // Service internally uses the API plugin
        const projects = await projectApiService.getProjectsByUser(123);
        console.log(projects);
      } catch (error) {
        console.error(error);
      }
    });
  }
};
```

## Example 7: Query Parameters

```javascript
import { inject } from 'vue';

export default {
  setup() {
    const api = inject('api');
    
    // GET with query parameters
    const searchProjects = async (searchTerm, status) => {
      // Becomes: /projects?search=term&status=active
      return await api.get('/projects', {
        search: searchTerm,
        status: status
      });
    };
    
    // GET with multiple filters
    const getFilteredProjects = async () => {
      return await api.get('/projects', {
        page: 1,
        limit: 10,
        sortBy: 'createdAt',
        order: 'desc'
      });
    };
    
    return { searchProjects, getFilteredProjects };
  }
};
```

## Example 8: Direct Axios Instance Access

For advanced use cases where you need full control:

```javascript
import { inject } from 'vue';

export default {
  setup() {
    const api = inject('api');
    
    // Access the underlying axios instance
    const customRequest = async () => {
      const response = await api.axios({
        method: 'GET',
        url: '/custom-endpoint',
        headers: {
          'X-Custom-Header': 'value'
        },
        transformResponse: [(data) => {
          // Custom data transformation
          return JSON.parse(data);
        }]
      });
      
      // With direct axios, you need to access .data
      return response.data;
    };
    
    return { customRequest };
  }
};
```

## Notes

1. **Token Management**: The Bearer token is automatically attached to all requests. No need to manually set Authorization headers.

2. **Error Handling**: All methods throw errors on failure. Always use try-catch blocks.

3. **Return Values**: All convenience methods (`get`, `post`, `put`, `delete`, `fetch`) return `response.data` directly. The `axios` property returns the full axios response.

4. **Base URL**: All URLs are relative to the configured baseURL (from VITE_API_BASE_URL env variable).

5. **Timeout**: Default timeout is 15 seconds. Configure in main.js if needed.
