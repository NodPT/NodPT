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
                // Load project data from backend
                const projectData = await projectApiService.getProject(projectId);

                let initialNodes = [];

                // Use nodes from the project if available
                if (projectData && projectData.Nodes && projectData.Nodes.length > 0) {
                        // Map backend nodes to frontend node format
                        initialNodes = projectData.Nodes.map(node => ({
                                id: node.Id,
                                type: node.Level.toLowerCase(), // Convert level to lowercase type (Director -> director)
                                name: node.Name,
                                inputs: 0,
                                outputs: 1
                        }));
                } else {
                        // Fallback to default Director node if no nodes found
                        initialNodes = [
                                { type: 'director', name: 'Director', inputs: 0, outputs: 1 },
                        ];
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
                // Fallback to default behavior
                const initialNodes = [
                        { type: 'director', name: 'Director', inputs: 0, outputs: 1 },
                ];

                const createdNodes = await leftPanelRef.value.resetNodes(initialNodes);

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
};

const handleDeleteNode = () => {
        // Check if there's a selected node
        if (!selectedNode.value || !selectedNode.value.id) {
                console.warn('No node selected to delete');
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
