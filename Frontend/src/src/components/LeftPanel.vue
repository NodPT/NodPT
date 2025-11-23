<template>
	<div class="left-panel w-100 h-100" :data-theme="isDarkTheme ? 'dark' : 'light'">
		<div class="rete-container w-100 h-100">
			<div ref="reteCanvas" class="rete-canvas background w-100 h-100"></div>
			<!-- Minimap is rendered by the plugin, not by a Vue component -->
			<div v-if="minimapVisible" class="rete-minimap"></div>
		</div>
	</div>
</template>

<script>
import { onMounted, ref, onBeforeUnmount } from 'vue';
import { EditorManager } from '../rete/editorManager';
import { listenEvent, EVENT_TYPES } from '../rete/eventBus';

export default {
	name: 'LeftPanel',
	components: {
		// Minimap component removed
	},
	props: {
		minimapVisible: {
			type: Boolean,
			default: false,
		},
		isDarkTheme: {
			type: Boolean,
			default: false,
		},
	},
	setup(props, { emit }) {
		// Define canvas reference and editor manager
		const reteCanvas = ref(null);
		const editorManager = new EditorManager();

		// Event listeners array for cleanup
		const eventListeners = [];

		// Search event handler
		const handleSearchNodes = (payload) => {
			const { searchTerm, callback } = payload;
			const results = editorManager.searchNodes(searchTerm);

			// Call the callback with results and editor manager reference
			if (typeof callback === 'function') {
				callback(results, editorManager);
			}
		};

		// Initialize editor on mount
		onMounted(async () => {
			try {
				await editorManager.initializeEditor(reteCanvas.value, emit);
				// Demo nodes creation removed - will be called from chat when solution is marked

				// Set up search event listener
				eventListeners.push(
					listenEvent(EVENT_TYPES.SEARCH_NODES, handleSearchNodes)
				);
			} catch (error) {
				console.error('Failed to initialize Rete editor:', error);
			}
		});

		// Cleanup on unmount
		onBeforeUnmount(() => {
			// Clean up event listeners
			eventListeners.forEach(unsubscribe => {
				if (typeof unsubscribe === 'function') {
					unsubscribe();
				}
			});

			// Clean up editor manager
			editorManager.cleanup();
		});

		// Expose method to add a new node
		const addNode = async (name = 'New Node') => {
			return await editorManager.addNode(name, reteCanvas.value);
		};

		// Expose method to delete a node
		const deleteNode = async (nodeId) => {
			return await editorManager.deleteNode(nodeId);
		};

		// Expose method to group nodes
		const groupNodes = async (nodeIds, groupName = 'Group') => {
			return await editorManager.groupNodes(nodeIds, groupName);
		};

		// Expose method to ungroup nodes
		const ungroupNodes = async (groupId) => {
			return await editorManager.ungroupNodes(groupId);
		};

		// Get the node manager for accessing node data
		const getNodeManager = () => {
			return editorManager;
		};

		// Expose method to lock a node
		const lockNode = async (nodeId) => {
			return await editorManager.lockNode(nodeId);
		};

		// Expose method to unlock a node
		const unlockNode = async (nodeId) => {
			return await editorManager.unlockNode(nodeId);
		};

		const resetNodes = async (initialNodes = []) => {
			if (typeof editorManager.resetNodes === 'function') {
				return await editorManager.resetNodes(initialNodes);
			}

			return [];
		};

		return {
			reteCanvas,
			addNode, // Expose the addNode method to parent component
			deleteNode, // Expose the deleteNode method to parent component
			groupNodes, // Expose the groupNodes method to parent component
			ungroupNodes, // Expose the ungroupNodes method to parent component
			getNodeManager, // Expose the getNodeManager method to parent component
			lockNode, // Expose the lockNode method to parent component
			unlockNode, // Expose the unlockNode method to parent component
			resetNodes,
		};
	},
};
</script>
