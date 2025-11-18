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
import { ref, onMounted, onBeforeUnmount, inject, watch } from 'vue';
import signalRService from '../service/signalRService';
import { listenEvent, EVENT_TYPES } from '../rete/eventBus';
import chatMessageApiService from '../service/chatMessageApiService';

export default {
	name: 'DeepChatComponent',
	setup() {
		const api = inject('api');
		chatMessageApiService.setApi(api);
		
		const deepChatRef = ref(null);
		const eventListeners = [];
		
		// SignalR connection ID
		const connectionId = ref('');
		
		// Current project and node context
		const currentProjectId = ref('');
		const currentNodeId = ref('');
		const currentNodeLevel = ref('manager');
		
		// Get user ID from localStorage or context
		const userId = ref(localStorage.getItem('userId') || 'anonymous');
		
		// Track if messages are being loaded
		const isLoadingMessages = ref(false);

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
			},
			// Custom handler to show thinking indicator and wait for SignalR response
			handler: async (body, signals) => {
				try {
					// Add thinking message immediately
					const thinkingMessageId = `thinking_${Date.now()}`;
					
					if (deepChatRef.value) {
						deepChatRef.value.addMessage({
							role: 'ai',
							text: '🤔 Thinking...',
							_id: thinkingMessageId
						});
					}

					// Send request to backend
					const response = await fetch(requestConfig.value.url, {
						method: 'POST',
						headers: requestConfig.value.headers,
						body: JSON.stringify({
							...body,
							...requestConfig.value.additionalBodyProps,
							NodeId: currentNodeId.value
						})
					});

					const data = await response.json();
					
					// Store the chat ID for SignalR response matching
					if (data && data.messageId) {
						// The actual AI response will come via SignalR
						// We keep the thinking message until SignalR updates it
						console.log('Chat submitted, waiting for AI response via SignalR. MessageId:', data.messageId);
					}

					// Return empty to prevent deep-chat from adding its own message
					return { text: '' };
				} catch (error) {
					console.error('Error submitting chat:', error);
					return { text: 'Sorry, there was an error processing your request.' };
				}
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
					
					// Check if this is a streaming response
					if (response && response.chatId && response.content) {
						// Find the message bubble with thinking icon and update it
						streamTextToChat(response.chatId, response.content);
					} else if (deepChatRef.value && response.content) {
						// Fallback: just add the message
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

		// Stream text word by word to chat
		const streamTextToChat = async (chatId, fullText) => {
			if (!deepChatRef.value || !fullText) {
				return;
			}

			// Add a thinking message first
			const thinkingMessage = {
				role: 'ai',
				text: '🤔 Thinking...',
				_id: chatId
			};

			deepChatRef.value.addMessage(thinkingMessage);

			// Wait a bit to show the thinking icon
			await new Promise(resolve => setTimeout(resolve, 500));

			// Split text into words for streaming effect
			const words = fullText.split(' ');
			let currentText = '';

			// Stream words one by one
			for (let i = 0; i < words.length; i++) {
				currentText += (i > 0 ? ' ' : '') + words[i];

				// Update the message efficiently
				if (deepChatRef.value) {
					try {
						// If deep-chat provides an updateMessage method, use it
						if (typeof deepChatRef.value.updateMessage === 'function') {
							deepChatRef.value.updateMessage(chatId, { text: currentText });
						} else {
							// Otherwise, batch updates every 5 words to reduce flicker
							if (i % 5 === 0 || i === words.length - 1) {
								// Remove and re-add only the last message
								// Remove last message
								deepChatRef.value.removeMessage?.(chatId);
								// Add updated message
								deepChatRef.value.addMessage({
									role: 'ai',
									text: currentText,
									_id: chatId
								});
							}
						}
					} catch (error) {
						// If update fails, just continue
						console.warn('Error updating streaming message:', error);
					}
				}

				// Wait between words for streaming effect (adjust speed as needed)
				await new Promise(resolve => setTimeout(resolve, 50)); // 50ms per word
			}

			// Store the final message
			const finalMessage = {
				role: 'ai',
				text: fullText,
				_id: chatId
			};

			// Update initialMessages to include the final version
			const existingIndex = initialMessages.value.findIndex(m => m._id === chatId);
			if (existingIndex >= 0) {
				initialMessages.value[existingIndex] = finalMessage;
			} else {
				initialMessages.value.push(finalMessage);
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
		const handleNodeSelection = async (nodeData) => {
			if (nodeData && nodeData.id) {
				currentNodeId.value = nodeData.id;
				
				if (nodeData.level) {
					currentNodeLevel.value = nodeData.level;
					if (requestConfig.value.additionalBodyProps) {
						requestConfig.value.additionalBodyProps.NodeLevel = nodeData.level;
					}
				}
				
				// Load chat messages for this node
				await loadChatMessages(nodeData.id);
			}
		};

		// Load chat messages for a node
		const loadChatMessages = async (nodeId) => {
			if (!nodeId || isLoadingMessages.value) {
				return;
			}

			isLoadingMessages.value = true;

			try {
				// Get messages from API (with localStorage caching)
				const messages = await chatMessageApiService.getMessagesByNode(nodeId);

				// Clear current messages in DeepChat
				if (deepChatRef.value) {
					// Reset messages
					initialMessages.value = [];
				}

				if (messages && messages.length > 0) {
					// Convert API messages to DeepChat format
					const deepChatMessages = messages.map(msg => ({
						role: msg.Sender === 'user' ? 'user' : 'ai',
						text: msg.Message,
						_id: msg.Oid // Store original ID for reference
					}));

					initialMessages.value = deepChatMessages;

					// Update DeepChat component if it's mounted
					if (deepChatRef.value) {
						deepChatMessages.forEach(msg => {
							deepChatRef.value.addMessage(msg);
						});
					}
				} else {
					// No messages found, create first chat
					await createFirstChat(nodeId);
				}
			} catch (error) {
				console.error('Error loading chat messages:', error);
				// On error, still try to create first chat
				await createFirstChat(nodeId);
			} finally {
				isLoadingMessages.value = false;
			}
		};

		// Create first chat for a node
		const createFirstChat = async (nodeId) => {
			if (!nodeId) {
				return;
			}

			try {
				const response = await chatMessageApiService.createFirstChat(nodeId, userId.value);

				// Add the initial AI message to the chat
				if (deepChatRef.value && response) {
					const aiMessage = {
						role: 'ai',
						text: 'Hello! I\'m here to help you with this node. What would you like to work on?'
					};

					initialMessages.value = [aiMessage];
					deepChatRef.value.addMessage(aiMessage);
				}
			} catch (error) {
				console.error('Error creating first chat:', error);
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
