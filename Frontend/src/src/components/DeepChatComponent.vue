<template>
	<div class="deepchat-container">
		<deep-chat
			ref="deepChatRef"
			:request="requestConfig"
			:initialMessages="initialMessages"
			:messageStyles="messageStyles"
			:style="chatStyle"
		></deep-chat>
	</div>
</template>

<script>
import { ref, onMounted, onBeforeUnmount, inject } from 'vue';
import signalRService from '../service/signalRService';
import { listenEvent, EVENT_TYPES } from '../rete/eventBus';

export default {
	name: 'DeepChatComponent',
	setup() {
		const deepChatRef = ref(null);
		const api = inject('api');
		const eventListeners = [];
		
		// SignalR connection ID
		const connectionId = ref('');
		
		// Current project and node context
		const currentProjectId = ref('');
		const currentNodeLevel = ref('manager');
		
		// Get user ID from localStorage or context
		const userId = ref(localStorage.getItem('userId') || 'anonymous');

		// Chat style configuration
		const chatStyle = {
			width: '100%',
			height: '600px',
			borderRadius: '8px'
		};

		// Message styles
		const messageStyles = {
			default: {
				shared: {
					bubble: {
						maxWidth: '100%',
						backgroundColor: 'unset',
						marginTop: '10px',
						marginBottom: '10px'
					}
				},
				user: {
					bubble: {
						backgroundColor: '#007bff',
						color: 'white'
					}
				},
				ai: {
					bubble: {
						backgroundColor: '#f1f1f1',
						color: 'black'
					}
				}
			}
		};

		// Initial messages
		const initialMessages = ref([
			{
				role: 'ai',
				text: 'Hello! How can I help you today?'
			}
		]);

		// Request configuration for DeepChat
		const requestConfig = ref({
			url: `${import.meta.env.VITE_API_BASE_URL || 'http://localhost:5015'}/api/chat/submit`,
			method: 'POST',
			headers: {
				'Content-Type': 'application/json'
			},
			additionalBodyProps: {
				UserId: userId.value,
				ConnectionId: '',
				ProjectId: currentProjectId.value,
				NodeLevel: currentNodeLevel.value
			}
		});

		// Setup SignalR connection and listeners
		const setupSignalR = async () => {
			try {
				// Wait for SignalR to be connected
				if (signalRService.connectionStatus !== 'connected') {
					await signalRService.initialize();
				}

				// Get connection ID
				connectionId.value = signalRService.connection?.connectionId || '';
				
				// Update request config with connection ID
				if (requestConfig.value.additionalBodyProps) {
					requestConfig.value.additionalBodyProps.ConnectionId = connectionId.value;
				}

				// Listen for AI responses from SignalR
				signalRService.on('ReceiveAIResponse', (response) => {
					console.log('Received AI response:', response);
					
					// Add message to DeepChat
					if (deepChatRef.value && response.content) {
						deepChatRef.value.addMessage({
							role: 'ai',
							text: response.content
						});
					}
				});

				console.log('DeepChat SignalR setup complete. ConnectionId:', connectionId.value);
			} catch (error) {
				console.error('Error setting up SignalR for DeepChat:', error);
			}
		};

		// Handle project context changes
		const handleProjectContextChange = (data) => {
			if (data && data.projectId) {
				currentProjectId.value = data.projectId;
				if (requestConfig.value.additionalBodyProps) {
					requestConfig.value.additionalBodyProps.ProjectId = data.projectId;
				}
			}
		};

		// Handle node selection changes
		const handleNodeSelection = (nodeData) => {
			if (nodeData && nodeData.level) {
				currentNodeLevel.value = nodeData.level;
				if (requestConfig.value.additionalBodyProps) {
					requestConfig.value.additionalBodyProps.NodeLevel = nodeData.level;
				}
			}
		};

		onMounted(async () => {
			// Import DeepChat component dynamically
			await import('deep-chat');
			
			// Setup SignalR
			await setupSignalR();

			// Listen for project and node changes
			eventListeners.push(
				listenEvent(EVENT_TYPES.PROJECT_CONTEXT_CHANGED, handleProjectContextChange)
			);
			eventListeners.push(
				listenEvent(EVENT_TYPES.NODE_SELECTED, handleNodeSelection)
			);
		});

		onBeforeUnmount(() => {
			// Clean up SignalR listeners
			signalRService.off('ReceiveAIResponse');
			
			// Clean up event listeners
			eventListeners.forEach((unsubscribe) => {
				if (typeof unsubscribe === 'function') {
					unsubscribe();
				}
			});
		});

		return {
			deepChatRef,
			requestConfig,
			initialMessages,
			messageStyles,
			chatStyle
		};
	}
};
</script>

<style scoped>
.deepchat-container {
	width: 100%;
	height: 100%;
	display: flex;
	flex-direction: column;
}
</style>
