<template>
	<div class="node" :class="{ selected: data.selected, locked: data.locked }" :style="nodeStyles()"
		data-testid="node">
		<div class="title w-100" :class="['title-' + getNodeType()]" data-testid="title">
			<span> {{ data.label }}</span>
			<i :class="statusIconClass" :style="{ color: statusColor }" class="status-icon ms-2"
				v-show="status !== 'off'"></i>
			<i class="bi bi-lock-fill lock-icon ms-2" v-show="data.locked" title="Locked"></i>
		</div>

		<!-- Large Status Icon in the middle -->
		<!-- <div class="status-icon-container"></div> -->

		<!-- Outputs -->
		<div class="output" v-for="([key, output], index) in outputs()" :key="'output' + key + seed"
			:data-testid="'output-' + key">
			<div class="output-title" data-testid="output-title">{{ output.label }}</div>
			<div class="output-socket">
				<Ref :data="{ type: 'socket', side: 'output', key: key, nodeId: data.id, payload: output.socket }"
					:emit="emit" data-testid="output-socket" />
			</div>
		</div>

		<!-- Controls -->
		<Ref v-for="([key, control], index) in controls()" :key="'control' + key + seed" :data-testid="'control-' + key"
			:emit="emit" :data="{ type: 'control', payload: control }" />

		<!-- Inputs -->
		<div class="input" v-for="([key, input], index) in inputs()" :key="'input' + key + seed"
			:data-testid="'input-' + key">
			<div class="input-socket">
				<Ref :data="{ type: 'socket', side: 'input', key: key, nodeId: data.id, payload: input.socket }"
					:emit="emit" data-testid="input-socket" />
			</div>
			<div class="input-title" v-show="!input.control || !input.showControl" data-testid="input-title">
				{{ input.label }}
			</div>
		</div>

		<!-- Refresh Icon in bottom-right corner
		<div class="refresh-icon-container">
			<i class="bi bi-arrow-clockwise" @click="refreshNode"></i>
		</div> -->
	</div>
</template>

<script>
import Ref from '../Ref.vue';
import { listenEvent, removeListener, EVENT_TYPES, triggerEvent } from '../rete/eventBus';

function sortByIndex(entries) {
	entries.sort((a, b) => {
		const ai = (a[1] && a[1].index) || 0;
		const bi = (b[1] && b[1].index) || 0;
		return ai - bi;
	});
	return entries;
}

export default {
	components: { Ref },
	props: ['data', 'emit', 'seed'],
	data() {
		return {
			// Default status is 'idle'
			status: this.data.status || 'idle',
		};
	},
	computed: {
		statusIconClass() {
			// Map status to corresponding Bootstrap icon class
			const iconMap = {
				idle: 'bi bi-circle',
				thinking: 'bi bi-hourglass-split animated',
				working: 'bi bi-gear animated',
				completed: 'bi bi-check-circle-fill',
				error: 'bi bi-exclamation-triangle-fill',
				off: '', // Empty class for hiding the icon
			};
			return iconMap[this.status] || iconMap.idle;
		},
		statusColor() {
			// Map status to corresponding colors
			const colorMap = {
				idle: '#ccc3b9',
				thinking: '#b0a999',
				working: '#ddd0be',
				completed: '#f9f6ee',
				error: '#c87ca1',
				off: 'transparent', // Transparent color for hiding the icon
			};
			return colorMap[this.status] || colorMap.idle;
		},
	},
	methods: {
		nodeStyles() {
			// Determine background color based on node type
			const nodeTypeColors = {
				panel: '#4d4d4d', // panelNode
				director: '#333333',
				manager: '#272727',
				inspector: '#1b1b1b',
				agent: '#010101',
			};

			// Create darker border colors (30% darker)
			const nodeBorderColors = {
				// panel: '#100202', // darker panelNode
				// director: '#150626',
				// manager: '#230c3b',
				// inspector: '#321751',
				// agent: '#402066',
			};

			// Get the node type
			const nodeType = this.getNodeType();

			return {
				width: Number.isFinite(this.data.width) ? `${this.data.width}px` : '',
				height: Number.isFinite(this.data.height) ? `${this.data.height}px` : '',
				// Add additional padding when status is off
				// paddingTop: this.status === 'off' ? '10px' : '',
				// Apply background color based on node type
				background: nodeTypeColors[nodeType] || 'rgba(15, 15, 15, 0.55)',
				// Apply border color based on node type
				// border: `1px solid #555 !important`,
			};
		},
		inputs() {
			return sortByIndex(Object.entries(this.data.inputs));
		},
		controls() {
			return sortByIndex(Object.entries(this.data.controls));
		},
		outputs() {
			return sortByIndex(Object.entries(this.data.outputs));
		},
		// Method to get node type
		getNodeType() {
			// First check if nodeType is directly available in the data
			if (this.data && this.data.nodeType) {
				return this.data.nodeType.toLowerCase();
			}
			// If not, try to detect from id
			else if (this.data && this.data.id) {
				const id = this.data.id.toLowerCase();
				if (id.includes('panel')) {
					return 'panel';
				} else if (id.includes('director')) {
					return 'director';
				} else if (id.includes('manager')) {
					return 'manager';
				} else if (id.includes('inspector')) {
					return 'inspector';
				}
			}
			return 'agent'; // Default
		},
		// Method to change the node status
		setStatus(newStatus) {
			if (['idle', 'thinking', 'working', 'completed', 'error', 'off'].includes(newStatus)) {
				this.status = newStatus;
				// Also update in the node data for persistence
				if (this.data) {
					this.data.status = newStatus;
				}
			}
		},
		// Method to handle refresh click
		refreshNode() {
			// Trigger the refresh event via eventBus with the node data
			triggerEvent(EVENT_TYPES.NODE_REFRESH, {
				nodeId: this.data.id,
				nodeData: this.data,
			});
			// Optional visual feedback
			this.setStatus('thinking');
			setTimeout(() => {
				this.setStatus('idle');
			}, 1000);
		},
	},
	// Set the initial status when component is created
	created() {
		// Check if this is a panel node, if so set status to 'off' by default
		if (this.data && this.data.id && (this.data.id.includes('panel_node') || this.data.id.startsWith('panel_'))) {
			this.status = 'off';
			if (this.data) {
				this.data.status = 'off';
			}
		}
		// Otherwise, if the node has a status in its data, use it
		else if (this.data && this.data.status) {
			this.status = this.data.status;
		}
	},
	mounted() {
		// Listen for status change events from the event bus
		this.unsubscribe = listenEvent(EVENT_TYPES.NODE_STATUS_CHANGE, (payload) => {
			// Only respond to events for this node
			if (payload && payload.nodeId === this.data.id) {
				this.setStatus(payload.status);
			}
		});
	},
	beforeUnmount() {
		// Clean up event listeners
		if (this.unsubscribe) {
			this.unsubscribe();
		}
	},
};
</script>
