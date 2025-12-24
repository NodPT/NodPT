<template>
        <div class="app-container main-editor" :data-theme="isDarkTheme ? 'dark' : 'light'">
                <TopBar :is-dark-theme="isDarkTheme" />

                <div class="main-content main-editor-content position-relative">
                        <LeftPanel ref="leftPanelRef" :minimap-visible="minimapVisible" :is-dark-theme="isDarkTheme"
                                :class="['main-editor-left-panel', { 'is-hidden': !isLeftPanelVisible }]" />
                        <RightPanel :selected-node="selectedNode" :is-dark-theme="isDarkTheme"
                                :class="['main-editor-right-panel', { 'is-hidden': !isRightPanelVisible }]" />
                </div>

                <Footer :selected-node="selectedNode" :total-nodes="totalNodes" :progress="buildProgress"
                        :minimap-visible="minimapVisible" :connection-status="connectionStatus"
                        :is-dark-theme="isDarkTheme" />
        </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, watch, inject, nextTick } from 'vue';
import { useRoute } from 'vue-router';
import TopBar from '../components/TopBar.vue';
import Footer from '../components/Footer.vue';
import LeftPanel from '../components/LeftPanel.vue';
import RightPanel from '../components/RightPanel.vue';
import '../assets/styles/main-editor.css';
import '../assets/styles/main-editor-dark.css';
import '../assets/styles/components-dark.css';
import '../assets/styles/chat-dark.css';
import { triggerEvent, listenEvent, EVENT_TYPES } from '../rete/eventBus';
import signalRService from '../service/signalRService';
import projectApiService from '../service/projectApiService';
import { useTheme } from '../composables/useTheme';

const route = useRoute();
const api = inject('api');
const toast = inject('toast');
const { isDarkTheme, loadTheme } = useTheme();
projectApiService.setApi(api);
const minimapVisible = ref(false);
const isLeftPanelVisible = ref(true);
const isRightPanelVisible = ref(true);
const selectedNode = ref(null);
const totalNodes = ref(0);
const buildProgress = ref(0);
const leftPanelRef = ref(null);
const connectionStatus = ref('disconnected');
const cleanupFns = [];
const editorReady = ref(false);
const currentProjectId = ref(null);

const toggleMinimap = () => {
        minimapVisible.value = !minimapVisible.value;
};

const toggleRightPanel = () => {
        isRightPanelVisible.value = !isRightPanelVisible.value;
};

const updateProjectInfo = () => {
        const queryParams = route.query || {};

        if (queryParams.projectName) {
                triggerEvent(EVENT_TYPES.PROJECT_NAME_UPDATE, queryParams.projectName);
        }
};

const initializeProjectContext = async () => {
        const queryParams = route.query || {};
        const projectId = queryParams.projectId;

        if (!projectId) {
                return;
        }

        if (currentProjectId.value === projectId) {
                return;
        }

        if (!editorReady.value || !leftPanelRef.value || typeof leftPanelRef.value.resetNodes !== 'function') {
                return;
        }

        selectedNode.value = null;

        try {
                // Load project data from backend via GET /api/projects/{id}
                // Expected response format (ProjectDto):
                // {
                //   Id: number,
                //   Name: string,
                //   Nodes: [
                //     {
                //       Id: string (GUID),
                //       Name: string,
                //       NodeType: string (enum: "Director", "Manager", "Inspector", "Worker", etc.),
                //       Status: string,
                //       ...
                //     }
                //   ]
                // }
                const projectData = await projectApiService.getProject(projectId);

                let initialNodes = [];

                // Extract nodes from the project response
                // Backend returns nodes in ProjectDto.Nodes property as NodeDto[]
                if (projectData && projectData.Nodes && projectData.Nodes.length > 0) {
                        // Map backend nodes (NodeDto) to frontend node format
                        // Note: Backend uses PascalCase (C# convention), frontend uses camelCase
                        initialNodes = projectData.Nodes.map(node => {
                                // Defensive check for NodeType property
                                const nodeType = node.NodeType || 'Director'; // Default to Director if missing
                                const type = typeof nodeType === 'string' ? nodeType.toLowerCase() : 'director';
                                
                                return {
                                        id: node.Id,                              // GUID string from backend (REQUIRED)
                                        type: type,                               // Convert "Director" -> "director"
                                        name: node.Name,                          // Node display name
                                        inputs: 0,                                // Will be set by node type
                                        outputs: 1                                // Will be set by node type
                                };
                        });
                } else {
                        // No nodes found in project - this should not happen as backend creates default node
                        console.error('No nodes found in project. Backend should create a default Director node.');
                        toast.error('Project has no nodes. Please contact support.');
                        initialNodes = [];
                }

                const createdNodes = await leftPanelRef.value.resetNodes(initialNodes);

                // Select the first Director node to load its messages
                if (createdNodes && createdNodes.length > 0) {
                        const directorNode = createdNodes.find(n => n.type === 'director') || createdNodes[0];
                        if (directorNode) {
                                // Trigger node selection to load chat messages
                                setTimeout(() => {
                                        triggerEvent(EVENT_TYPES.NODE_SELECTED, {
                                                id: directorNode.id,
                                                name: directorNode.name,
                                                type: directorNode.type
                                        });
                                }, 100);
                        }
                }

                const nodeManager = leftPanelRef.value.getNodeManager ? leftPanelRef.value.getNodeManager() : null;
                if (nodeManager && nodeManager.editor && typeof nodeManager.editor.getNodes === 'function') {
                        try {
                                totalNodes.value = nodeManager.editor.getNodes().length;
                        } catch (error) {
                                totalNodes.value = createdNodes.length;
                        }
                } else {
                        totalNodes.value = createdNodes.length;
                }

                buildProgress.value = 0;

                currentProjectId.value = projectId;

                triggerEvent(EVENT_TYPES.PROJECT_CONTEXT_CHANGED, {
                        projectId,
                        projectName: queryParams.projectName || '',
                        isNewProject: queryParams.isNewProject === 'true',
                });
        } catch (error) {
                console.error('Error initializing project context:', error);
                // Show error to user - cannot create nodes without backend IDs
                toast.error(`Failed to load project: ${error.message || 'Unknown error'}`);
                
                // Don't create any nodes or set project context - all nodes must come from backend
                totalNodes.value = 0;
                buildProgress.value = 0;
                // Don't set currentProjectId to prevent inconsistent state
        }
};

const handleEditorReady = async (editorInfo) => {
        if (editorInfo?.nodeCount) {
                totalNodes.value = editorInfo.nodeCount;
        }

        updateProjectInfo();

        editorReady.value = true;

        await initializeProjectContext();

        // Removed demo node creation - projects are now created via API
        // and nodes should be loaded from the backend if project has existing data
};

const handleKeydown = (event) => {
        if (event.ctrlKey && event.key === 'z' && !event.shiftKey) {
                event.preventDefault();
                triggerEvent(EVENT_TYPES.UNDO);
        } else if ((event.ctrlKey && event.key === 'y') || (event.ctrlKey && event.shiftKey && event.key === 'Z')) {
                event.preventDefault();
                triggerEvent(EVENT_TYPES.REDO);
        }
};

const handleSignalRStatusChange = (status) => {
        connectionStatus.value = status;
};

const handleSignalRHello = (message) => {
        // Display the Hello message from the server
        toast.info(message);
};

const handleSignalRToggle = async () => {
        try {
                if (connectionStatus.value === 'connected' || connectionStatus.value === 'connecting') {
                        // Stop the connection
                        await signalRService.stop();
                } else {
                        // Start the connection
                        await signalRService.initialize();
                }
                connectionStatus.value = signalRService.getConnectionStatus();
        } catch (error) {
                console.error('Error toggling SignalR connection:', error);
        }
};

const handleNodeSelected = (nodeData) => {
        selectedNode.value = nodeData;
        // Broadcast selected node change to other components (e.g., TopBar)
        triggerEvent(EVENT_TYPES.SELECTED_NODE_CHANGED, nodeData);
};

const handleDeleteNode = () => {
        // Check if there's a selected node
        if (!selectedNode.value || !selectedNode.value.id) {
                console.warn('No node selected to delete');
                return;
        }

        // Prevent deletion of Director node
        if (selectedNode.value.type === 'director') {
                console.warn('Director node cannot be deleted');
                return;
        }

        // Get the editor manager from LeftPanel
        if (leftPanelRef.value && typeof leftPanelRef.value.getNodeManager === 'function') {
                const nodeManager = leftPanelRef.value.getNodeManager();
                if (nodeManager) {
                        // Delete the selected node
                        nodeManager.deleteNode(selectedNode.value.id);
                        // Clear the selected node
                        selectedNode.value = null;
                }
        }
};

// Select Director node as default when no node is selected
const selectDefaultDirectorNode = () => {
        if (!leftPanelRef.value) return;
        
        const nodeManager = leftPanelRef.value.getNodeManager ? leftPanelRef.value.getNodeManager() : null;
        if (!nodeManager || !nodeManager.nodes) return;
        
        // Find the Director node
        const directorNode = nodeManager.nodes.find(n => n.type === 'director');
        if (directorNode) {
                triggerEvent(EVENT_TYPES.NODE_SELECTED, {
                        id: directorNode.node.id,
                        name: directorNode.name,
                        type: directorNode.type
                });
        }
};

// Watch for selectedNode changes - select Director as default when no node is selected
watch(selectedNode, async (newValue) => {
        if (!newValue && editorReady.value) {
                // Use nextTick to ensure DOM updates are complete before selecting default node
                await nextTick();
                if (!selectedNode.value) {
                        selectDefaultDirectorNode();
                }
        }
});

// Watch for route query changes to update project info
watch(
        () => route.query,
        async () => {
                updateProjectInfo();
                await initializeProjectContext();
        },
        { deep: true }
);

onMounted(async () => {
        // Load theme
        loadTheme();

        cleanupFns.push(listenEvent(EVENT_TYPES.EDITOR_READY, handleEditorReady));
        cleanupFns.push(listenEvent(EVENT_TYPES.TOGGLE_MINIMAP, toggleMinimap));
        cleanupFns.push(listenEvent(EVENT_TYPES.TOGGLE_RIGHT_PANEL, toggleRightPanel));
        cleanupFns.push(listenEvent(EVENT_TYPES.SIGNALR_STATUS_CHANGED, handleSignalRStatusChange));
        cleanupFns.push(listenEvent(EVENT_TYPES.SIGNALR_HELLO_RECEIVED, handleSignalRHello));
        cleanupFns.push(listenEvent(EVENT_TYPES.SIGNALR_TOGGLE_CONNECTION, handleSignalRToggle));
        cleanupFns.push(listenEvent(EVENT_TYPES.NODE_SELECTED, handleNodeSelected));
        cleanupFns.push(listenEvent(EVENT_TYPES.DELETE_NODE, handleDeleteNode));
        window.addEventListener('keydown', handleKeydown);

        // Start SignalR connection when entering the editor
        try {
                await signalRService.initialize();
                connectionStatus.value = signalRService.getConnectionStatus();
        } catch (error) {
                console.error('Failed to initialize SignalR:', error);
                // Display error toast when connection fails
                toast.error(`SignalR connection failed: ${error.message || 'Unknown error'}`);
        }

        // Add beforeunload confirmation to prevent accidental page refresh
        const handleBeforeUnload = (event) => {
                // Stop SignalR connection gracefully
                signalRService.stop().catch(err => console.error('Error stopping SignalR:', err));

                // Show confirmation dialog
                event.preventDefault();
                event.returnValue = ''; // Chrome requires returnValue to be set
        };
        window.addEventListener('beforeunload', handleBeforeUnload);
        cleanupFns.push(() => window.removeEventListener('beforeunload', handleBeforeUnload));

        // Update project info on initial mount
        updateProjectInfo();
        initializeProjectContext();
});

onBeforeUnmount(async () => {
        window.removeEventListener('keydown', handleKeydown);
        cleanupFns.forEach((unsubscribe) => {
                if (typeof unsubscribe === 'function') {
                        unsubscribe();
                }
        });
        cleanupFns.length = 0;

        // Stop SignalR connection when leaving the editor
        try {
                await signalRService.stop();
        } catch (error) {
                console.error('Error stopping SignalR:', error);
        }
});
</script>
