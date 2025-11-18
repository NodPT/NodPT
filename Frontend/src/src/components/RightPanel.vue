<template>
	<div class="right-panel">
		<button type="button" class="btn btn-sm close-panel-btn" @click="handleClose" aria-label="Close panel">
			<i class="bi bi-x-lg"></i>
		</button>
		<ul class="nav nav-tabs" id="rightPanelTabs" role="tablist">
			<li class="nav-item" role="presentation" data-bs-toggle="tooltip" title="AI Chat">
				<button class="nav-link active d-flex align-items-center" id="chat-tab" data-bs-toggle="tab"
					data-bs-target="#chat" type="button" role="tab" aria-controls="chat" aria-selected="true"
					data-bs-placement="bottom" title="AI Chat">
					<i class="bi bi-chat-dots me-1"></i>
				</button>
			</li>

			<!-- review-tab -->
			<li class="nav-item" role="presentation" data-bs-toggle="tooltip" title="Review">
				<button class="nav-link d-flex align-items-center" id="review-tab" data-bs-toggle="tab"
					data-bs-target="#review" type="button" role="tab" aria-controls="review" aria-selected="false"
					data-bs-placement="bottom" title="Review">
					<i class="bi bi-eye me-1"></i>
				</button>
			</li>

			<!-- properties-tab -->
			<li class="nav-item" role="presentation" data-bs-toggle="tooltip" title="Properties">
				<button class="nav-link d-flex align-items-center" id="properties-tab" data-bs-toggle="tab"
					data-bs-target="#properties" type="button" role="tab" aria-controls="properties"
					aria-selected="false" data-bs-placement="bottom" title="Properties">
					<i class="bi bi-gear me-1"></i>
				</button>
			</li>

			<!-- File Explorer Tab -->
			<li class="nav-item" role="presentation" data-bs-toggle="tooltip" title="File Explorer">
				<button class="nav-link d-flex align-items-center" id="files-tab" data-bs-toggle="tab"
					data-bs-target="#files" type="button" role="tab" aria-controls="files" aria-selected="false"
					data-bs-placement="bottom" title="File Explorer">
					<i class="bi bi-folder2 me-1"></i>
				</button>
			</li>

			<!-- log Tab -->
			<li class="nav-item" role="presentation" data-bs-toggle="tooltip" title="Logs">
				<button class="nav-link d-flex align-items-center" id="logs-tab" data-bs-toggle="tab"
					data-bs-target="#logs" type="button" role="tab" aria-controls="logs" aria-selected="false"
					data-bs-placement="bottom" title="Logs">
					<i class="bi bi-list-ul me-1"></i>
				</button>
			</li>
		</ul>
		<div class="tab-content p-3">
			<!-- AI Chat Tab -->
			<div class="tab-pane fade show active" id="chat" role="tabpanel" aria-labelledby="chat-tab">
				<DeepChatComponent />
			</div>

			<!-- Review Tab -->
			<!-- <div class="tab-pane fade" id="review" role="tabpanel" aria-labelledby="review-tab">
				<Review />
			</div> -->


			<!-- Properties Tab -->
			<div class="tab-pane fade" id="properties" role="tabpanel" aria-labelledby="properties-tab">
				<div class="properties-container">
					<div class="node-info mb-3">
						<h6>{{ propertiesData.name }}</h6>
						<small class="text-muted">{{ propertiesData.description }}</small>
					</div>

					<div v-if="propertiesData.nodeId" class="node-details">
						<!-- Basic Info -->
						<div class="mb-3">
							<label class="form-label">Node ID</label>
							<input type="text" class="form-control" :value="propertiesData.nodeId" disabled />
						</div>
						<div class="mb-3">
							<label class="form-label">Node Type</label>
							<input type="text" class="form-control" :value="propertiesData.type" disabled />
						</div>
						<div class="mb-3">
							<label class="form-label">Status</label>
							<span
								:class="['badge', propertiesData.status === 'active' ? 'bg-success' : 'bg-secondary']">
								{{ propertiesData.status }}
							</span>
						</div>

						<!-- Dynamic Properties -->
						<div v-for="(prop, key) in propertiesData.properties" :key="key" class="mb-3">
							<label class="form-label">{{ prop.label }}</label>
							<input v-if="prop.type === 'text' || prop.type === 'number'" :type="prop.type"
								v-model="prop.value" class="form-control" :min="prop.min" :max="prop.max"
								:required="prop.required" />
							<select v-else-if="prop.type === 'select'" v-model="prop.value" class="form-select"
								:required="prop.required">
								<option v-for="option in prop.options" :key="option" :value="option">{{ option }}
								</option>
							</select>
							<textarea v-else-if="prop.type === 'textarea'" v-model="prop.value" class="form-control"
								rows="4" style="font-family: 'Courier New', monospace"
								:required="prop.required"></textarea>
							<div v-else-if="prop.type === 'checkbox'" class="form-check">
								<input v-model="prop.value" class="form-check-input" type="checkbox"
									:id="`prop-${key}`" />
								<label class="form-check-label" :for="`prop-${key}`"> Enable </label>
							</div>
						</div>

						<!-- Performance Info -->
						<div v-if="propertiesData.performance" class="performance-info mt-4">
							<h6>Performance Metrics</h6>
							<div class="row">
								<div v-for="(value, key) in propertiesData.performance" :key="key" class="col-6 mb-2">
									<small class="text-muted d-block">{{ formatMetricLabel(key) }}</small>
									<strong>{{ value }}</strong>
								</div>
							</div>
						</div>

						<div class="mt-3">
							<button @click="applyChanges" type="button" class="btn btn-primary me-2">Apply
								Changes</button>
							<button @click="resetProperties" type="button"
								class="btn btn-outline-secondary">Reset</button>
						</div>
					</div>
					<div v-else class="text-center text-muted">Select a node to view its properties</div>
				</div>
			</div>

			<!-- File Explorer Tab -->
			<div class="tab-pane fade" id="files" role="tabpanel" aria-labelledby="files-tab">
				<FileExplorer />
			</div>





			<!-- Logs Tab -->
			<div class="tab-pane fade" id="logs" role="tabpanel" aria-labelledby="logs-tab">
				<div class="logs-container">
					<div class="d-flex justify-content-between align-items-center mb-2">
						<div class="form-check">
							<input v-model="autoScroll" class="form-check-input" type="checkbox" id="autoScroll" />
							<label class="form-check-label" for="autoScroll">Auto-scroll</label>
						</div>
						<button @click="clearLogs" class="btn btn-sm btn-outline-danger">Clear</button>
					</div>
					<div class="logs-display" ref="logsContainer">
						<div v-for="log in logsData" :key="log.id" :class="['log-entry', `log-${log.level}`]">
							<span class="log-time">{{ formatTime(log.timestamp) }}</span>
							<span :class="['log-level', `level-${log.level}`]">{{ log.level.toUpperCase() }}</span>
							<span class="log-message">{{ log.message }}</span>
						</div>
					</div>
				</div>
			</div>

		</div>
	</div>
</template>

<script>
import { ref, reactive, watch, onMounted, nextTick, onUnmounted, inject } from 'vue';
import { triggerEvent, listenEvent, EVENT_TYPES } from '../rete/eventBus';
import Chat from './Chat.vue';
import DeepChatComponent from './DeepChatComponent.vue';
import Review from './Review.vue';
import FileExplorer from './FileExplorer.vue';
import nodeApiService from '../service/nodeApiService';

export default {
	name: 'RightPanel',
	components: {
		Chat,
		DeepChatComponent,
		Review,
		FileExplorer,
	},
	props: {
		selectedNode: {
			type: Object,
			default: null,
		},
	},
	setup(props) {
		// Inject api
		const api = inject('api');
		nodeApiService.setApi(api);
		
		// Reactive data for each tab (excluding chat which is now a separate component)
		const logsData = ref([]);
		const propertiesData = reactive({
			nodeId: '',
			name: 'No node selected',
			type: 'none',
			description: 'Select a node to view its properties',
			status: 'idle',
			properties: {},
			performance: null
		});

		// UI state
		const autoScroll = ref(true);

		// Refs for DOM elements
		const logsContainer = ref(null);

		// Data cache to avoid repeated fetches
		const dataCache = reactive({
			logs: {},
			properties: {},
		});

		// (dragging removed) Right panel is fixed to the right side of the screen

		// Fetch data from JSON files
		const fetchData = async (type, nodeKey = 'default') => {
			const cacheKey = `${type}_${nodeKey}`;

			// Return cached data if available
			if (dataCache[type][nodeKey]) {
				return dataCache[type][nodeKey];
			}

			try {
				const response = await fetch(`/data/${type}-data.json`);
				if (!response.ok) {
					throw new Error(`Failed to fetch ${type} data`);
				}
				const data = await response.json();

				// Cache the entire data object
				dataCache[type] = data;

				// Return specific node data or default
				return data[nodeKey] || data.default || {};
			} catch (error) {
				console.error(`Error fetching ${type} data:`, error);
				return getDefaultData(type);
			}
		};

		// Fallback default data
		const getDefaultData = (type) => {
			switch (type) {
				case 'logs':
					return [{ id: 1, level: 'info', message: 'No logs available', timestamp: new Date().toISOString() }];
				case 'properties':
					return { nodeId: '', name: 'No node selected', type: 'none', description: 'Select a node to view its properties', properties: {} };
				default:
					return {};
			}
		};

		// Load data for specific node or default
		const loadTabData = async (nodeKey) => {
			const currentKey = nodeKey || 'default';

			try {
				// Load logs data
				logsData.value = await fetchData('logs', currentKey);

				// Load properties data
				const propertiesResult = await fetchData('properties', currentKey);
				Object.assign(propertiesData, propertiesResult);

				// Auto-scroll logs if enabled
				await nextTick();
				scrollToBottom();
			} catch (error) {
				console.error('Error loading tab data:', error);
			}
		};

		// Load node data from API
		const loadNodeData = async (nodeId) => {
			if (!nodeId) {
				// Reset to default
				Object.assign(propertiesData, {
					nodeId: '',
					name: 'No node selected',
					type: 'none',
					description: 'Select a node to view its properties',
					status: 'idle',
					properties: {},
					performance: null
				});
				return;
			}

			try {
				// Fetch node data from API (with localStorage caching)
				const nodeData = await nodeApiService.getNode(nodeId);

				if (nodeData) {
					// Update properties with node data
					Object.assign(propertiesData, {
						nodeId: nodeData.Id,
						name: nodeData.Name || 'Unnamed Node',
						type: nodeData.NodeType || 'Default',
						description: `Node Level: ${nodeData.Level}`,
						status: nodeData.Status || 'idle',
						properties: nodeData.Properties || {},
						performance: null // Can be extended later
					});

					// Trigger event to notify DeepChatComponent about node selection
					triggerEvent(EVENT_TYPES.NODE_SELECTED, {
						id: nodeData.Id,
						name: nodeData.Name,
						level: nodeData.Level,
						nodeData: nodeData
					});
				}
			} catch (error) {
				console.error('Error loading node data:', error);
				// Set default data on error
				Object.assign(propertiesData, {
					nodeId: nodeId,
					name: 'Error loading node',
					type: 'unknown',
					description: 'Failed to load node data',
					status: 'error',
					properties: {},
					performance: null
				});
			}
		};

		// Initialize tooltips


		// Utility functions
		const formatTime = (timestamp) => {
			if (!timestamp) return '';
			const date = new Date(timestamp);
			return date.toLocaleString();
		};

		const formatMetricLabel = (key) => {
			return key.replace(/([A-Z])/g, ' $1').replace(/^./, (str) => str.toUpperCase());
		};

		const scrollToBottom = () => {
			if (autoScroll.value) {
				if (logsContainer.value) {
					logsContainer.value.scrollTop = logsContainer.value.scrollHeight;
				}
			}
		};

		// Logs functions
		const clearLogs = () => {
			logsData.value = [];
		};

		// Properties functions
		const applyChanges = () => {
			console.log('Applying property changes:', propertiesData);
			// In a real app, this would save to backend
		};

		const resetProperties = async () => {
			const nodeKey = props.selectedNode?.name || 'default';
			const freshData = await fetchData('properties', nodeKey);
			Object.assign(propertiesData, freshData);
		};

		// // Watch for selectedNode changes
		// watch(() => props.selectedNode, (newNode) => {
		//   const nodeKey = newNode?.name || 'default'
		//   console.log('Loading data for node:', nodeKey, newNode)
		//   loadTabData(nodeKey)
		// }, { immediate: true })

		// Watch for selectedNode changes to load node data from API
		watch(() => props.selectedNode, (newNode) => {
			if (newNode && newNode.id) {
				loadNodeData(newNode.id);
			} else {
				loadNodeData(null);
			}
		}, { immediate: true });

		// handleClose triggers the same event Footer uses to toggle the panel
		const handleClose = () => {
			triggerEvent(EVENT_TYPES.TOGGLE_RIGHT_PANEL);
		};

		// Load initial data on mount
		onMounted(() => {
			loadTabData('default');

			// nothing to init for a fixed-right anchored panel
		});

		// Cleanup on unmount
		onUnmounted(() => {
			// no-op
		});

		return {
			// Data
			logsData,
			propertiesData,
			autoScroll,

			// Refs
			logsContainer,

			// Methods
			formatTime,
			formatMetricLabel,
			clearLogs,
			applyChanges,
			resetProperties,
			handleClose,
		};
	},
};
</script>

<style scoped>
/* Ensure tab buttons have proper default text color */
.right-panel .nav-tabs .nav-link {
	color: var(--bs-body-color, #212529);
	background-color: transparent;
}

/* Active tab styling */
.right-panel .nav-tabs .nav-link.active {
	color: var(--bs-primary, #0d6efd);
	background-color: var(--bs-body-bg, #ffffff);
	border-color: var(--bs-border-color, #dee2e6);
}

/* Hover state for inactive tabs */
.right-panel .nav-tabs .nav-link:hover:not(.active) {
	color: var(--bs-primary, #0d6efd);
	border-color: transparent;
}

/* Close button styling */
.close-panel-btn {
	position: absolute;
	top: 0.5rem;
	right: 0.5rem;
	z-index: 10;
	background: transparent;
	border: none;
	padding: 0.25rem 0.5rem;
}

.close-panel-btn:hover {
	background-color: rgba(0, 0, 0, 0.05);
	border-radius: 0.25rem;
}
</style>
