<template>
	<div class="app-container main-editor">
		<TopBar />

		<div class="main-content main-editor-content position-relative">
			<LeftPanel ref="leftPanelRef" :minimap-visible="minimapVisible"
				:class="['main-editor-left-panel', { 'is-hidden': !isLeftPanelVisible }]" />
			<RightPanel :selected-node="selectedNode"
				:class="['main-editor-right-panel', { 'is-hidden': !isRightPanelVisible }]" />
		</div>

		<Footer :selected-node="selectedNode" :total-nodes="totalNodes" :progress="buildProgress"
			:minimap-visible="minimapVisible" :connection-status="connectionStatus" />
	</div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, watch } from 'vue';
import { useRoute } from 'vue-router';
import TopBar from '../components/TopBar.vue';
import Footer from '../components/Footer.vue';
import LeftPanel from '../components/LeftPanel.vue';
import RightPanel from '../components/RightPanel.vue';
import '../assets/styles/main-editor.css';
import { triggerEvent, listenEvent, EVENT_TYPES } from '../rete/eventBus';
import signalRService from '../service/signalRService';

const route = useRoute();
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
	cleanupFns.push(listenEvent(EVENT_TYPES.EDITOR_READY, handleEditorReady));
	cleanupFns.push(listenEvent(EVENT_TYPES.TOGGLE_MINIMAP, toggleMinimap));
	cleanupFns.push(listenEvent(EVENT_TYPES.TOGGLE_RIGHT_PANEL, toggleRightPanel));
	cleanupFns.push(listenEvent(EVENT_TYPES.SIGNALR_STATUS_CHANGED, handleSignalRStatusChange));
	cleanupFns.push(listenEvent(EVENT_TYPES.NODE_SELECTED, handleNodeSelected));
	cleanupFns.push(listenEvent(EVENT_TYPES.DELETE_NODE, handleDeleteNode));
	window.addEventListener('keydown', handleKeydown);

	// SignalR connection is now managed by auth lifecycle
	// It will automatically start when auth:signed-in event is triggered
	// and stop when auth:signed-out or auth:requires-relogin is triggered
	connectionStatus.value = signalRService.getConnectionStatus();

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
