<template>
	<footer class="navbar studio-footer p-0 fixed-bottom">
		<div class="container-fluid d-flex align-items-center py-1">
			<!-- Node Status -->
			<div class="d-flex align-items-center me-3">
				<span v-if="selectedNode" class="badge text-bg-primary me-2"> <i class="bi bi-node-plus"></i> {{
					selectedNode.label }} </span>
				<span class="badge text-bg-secondary me-2"> <i class="bi bi-diagram-3"></i> {{ totalNodes }} </span>
			</div>

			<!-- Build Progress -->
			<div class="progress flex-grow-1 mx-2" style="height: 10px">
				<div class="progress-bar bg-info" role="progressbar" :style="{ width: progress + '%' }"
					:aria-valuenow="progress" aria-valuemin="0" aria-valuemax="100"></div>
			</div>

			<!-- SignalR Connection Status -->
			<div class="d-flex align-items-center ms-2">
				<span class="badge" :class="connectionStatusClass" :title="connectionStatusTitle" 
					@click="handleSignalRClick" :style="isQAMode ? 'cursor: pointer;' : ''">
					<i class="bi" :class="connectionStatusIcon"></i>
				</span>
			</div>

			<!-- Arrange Nodes Button -->
			<button class="btn btn-outline-secondary btn-sm ms-2" @click="handleArrangeNodes"
				title="Auto Arrange Nodes">
				<i class="bi bi-grid-3x3"></i>
			</button>

			<!-- Minimap Toggle -->
			<button class="btn btn-outline-secondary btn-sm ms-2" @click="handleToggleMinimap" title="Toggle Minimap">
				<i class="bi" :class="minimapVisible ? 'bi-eye-slash' : 'bi-eye'"></i>
			</button>
		</div>
	</footer>
</template>

<script>
import { triggerEvent, listenEvent, EVENT_TYPES } from '../rete/eventBus';

export default {
	name: 'BottomBar',
	props: {
		selectedNode: {
			type: String,
			default: '',
		},
		totalNodes: {
			type: Number,
			default: 0,
		},
		progress: {
			type: Number,
			default: 10,
		},
		minimapVisible: {
			type: Boolean,
			default: false,
		},
		connectionStatus: {
			type: String,
			default: 'disconnected',
		},
	},
	data() {
		return {
			cleanupFns: [],
			isQAMode: false,
		};
	},
	mounted() {
		// Check if running in QA environment
		if (import.meta.env && import.meta.env.VITE_ENV === 'QA') {
			this.isQAMode = true;
		}
	},
	computed: {
		connectionStatusClass() {
			const statusMap = {
				'connected': 'text-bg-success',
				'connecting': 'text-bg-warning',
				'reconnecting': 'text-bg-warning',
				'disconnected': 'text-bg-secondary'
			};
			return statusMap[this.connectionStatus] || 'text-bg-secondary';
		},
		connectionStatusIcon() {
			const iconMap = {
				'connected': 'bi-wifi',
				'connecting': 'bi-hourglass-split',
				'reconnecting': 'bi-arrow-repeat',
				'disconnected': 'bi-wifi-off'
			};
			return iconMap[this.connectionStatus] || 'bi-wifi-off';
		},
		connectionStatusTitle() {
			const titleMap = {
				'connected': 'SignalR: Connected',
				'connecting': 'SignalR: Connecting...',
				'reconnecting': 'SignalR: Reconnecting...',
				'disconnected': 'SignalR: Disconnected'
			};
			const baseTitle = titleMap[this.connectionStatus] || 'SignalR: Disconnected';
			return this.isQAMode ? `${baseTitle} (Click to toggle)` : baseTitle;
		},
	},
	methods: {
		handleArrangeNodes() {
			triggerEvent(EVENT_TYPES.ARRANGE_NODES);
		},
		handleToggleMinimap() {
			triggerEvent(EVENT_TYPES.TOGGLE_MINIMAP);
		},
		handleSignalRClick() {
			// Only allow manual toggle in QA mode
			if (this.isQAMode) {
				triggerEvent(EVENT_TYPES.SIGNALR_TOGGLE_CONNECTION);
			}
		},
	},
	beforeUnmount() {
		this.cleanupFns.forEach((unsubscribe) => {
			if (typeof unsubscribe === 'function') {
				unsubscribe();
			}
		});
		this.cleanupFns.length = 0;
	},
};
</script>
