<template>
	<div class="deepchat-container">
		<deep-chat ref="deepChatRef" :request="requestConfig" :initialMessages="initialMessages"
			:messageStyles="messageStyles" :auxiliaryStyle="auxiliaryStyle" :style="chatStyle"></deep-chat>
	</div>
</template>

<script>
import { ref, onMounted, onBeforeUnmount, inject, watch } from 'vue';
import signalRService from '../service/signalRService';
import { listenEvent, EVENT_TYPES } from '../rete/eventBus';
import chatMessageApiService from '../service/chatMessageApiService';
import chatApiService from '../service/chatApiService';

export default {
	name: 'DeepChatComponent',
	setup() {
		const api = inject('api');
		chatMessageApiService.setApi(api);
		chatApiService.setApi(api);

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
			height: window.innerHeight - 136 + 'px',
			// borderRadius: '8px',
			border: '0',
			padding: '0',
			// backgroundColor: '#1a1a2e',
			// color: '#e8eaf6'
		};

		// Message styles for dark theme
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
						backgroundColor: '#4fc3f7',
						color: 'white'
					}
				},
				ai: {
					bubble: {
						backgroundColor: '#667eea',
						color: 'white'
					}
				}
			}
		};

		// Auxiliary style to add custom HTML for action buttons
		const auxiliaryStyle = `
			<style>
				.deep-chat-button-panel {
					display: flex;
					gap: 8px;
					margin-top: 8px;
					padding-left: 4px;
				}
				.deep-chat-message-button {
					background: none;
					border: none;
					padding: 4px 8px;
					cursor: pointer;
					font-size: 14px;
					color: #666;
					transition: all 0.2s ease;
					border-radius: 4px;
				}
				.deep-chat-message-button:hover {
					background-color: rgba(0, 0, 0, 0.05);
					color: #333;
				}
				.deep-chat-message-button i {
					font-size: 16px;
				}
				.btn-like:hover { color: #4caf50; }
				.btn-dislike:hover { color: #f44336; }
				.btn-solution:hover { color: #2196f3; }
				.btn-copy:hover { color: #ff9800; }
			</style>
		`;

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

		// Message action handlers
		const handleLike = async (messageId) => {
			try {
				await chatApiService.likeMessage(messageId);
				console.log('Message liked:', messageId);
			} catch (error) {
				console.error('Error liking message:', error);
			}
		};

		const handleDislike = async (messageId) => {
			try {
				await chatApiService.dislikeMessage(messageId);
				console.log('Message disliked:', messageId);
			} catch (error) {
				console.error('Error disliking message:', error);
			}
		};

		const handleMarkAsSolution = async (messageId) => {
			try {
				await chatApiService.markAsSolution(messageId, currentNodeId.value);
				console.log('Message marked as solution:', messageId);
				// Update the message UI to show it's marked as solution
				if (deepChatRef.value) {
					// Find and update the message
					const messages = deepChatRef.value.getMessages?.() || [];
					const message = messages.find(m => m._id === messageId);
					if (message) {
						message.markedAsSolution = true;
					}
				}
			} catch (error) {
				console.error('Error marking message as solution:', error);
			}
		};

		const handleCopy = async (text) => {
			try {
				if (navigator.clipboard && navigator.clipboard.writeText) {
					await navigator.clipboard.writeText(text);
				} else {
					const el = document.createElement('textarea');
					el.value = text;
					el.setAttribute('readonly', '');
					el.style.position = 'absolute';
					el.style.left = '-9999px';
					document.body.appendChild(el);
					el.select();
					document.execCommand('copy');
					document.body.removeChild(el);
				}
				console.log('Message copied to clipboard');
			} catch (error) {
				console.error('Error copying message:', error);
			}
		};

		// Function to add action buttons to AI messages
		const addActionButtonsToMessages = () => {
			if (!deepChatRef.value) return;

			// Use MutationObserver to watch for new messages
			const observer = new MutationObserver((mutations) => {
				mutations.forEach((mutation) => {
					mutation.addedNodes.forEach((node) => {
						if (node.nodeType === 1 && node.classList && node.classList.contains('ai-message')) {
							addButtonsToMessage(node);
						} else if (node.querySelectorAll) {
							// Check children for AI messages
							const aiMessages = node.querySelectorAll('.ai-message, [class*="ai"]');
							aiMessages.forEach((msg) => {
								addButtonsToMessage(msg);
							});
						}
					});
				});
			});

			// Observe the deep-chat component
			if (deepChatRef.value.$el || deepChatRef.value) {
				const targetNode = deepChatRef.value.$el || deepChatRef.value;
				observer.observe(targetNode, {
					childList: true,
					subtree: true
				});
			}

			// Also add buttons to existing messages
			setTimeout(() => {
				const container = deepChatRef.value.$el || deepChatRef.value;
				if (container && container.shadowRoot) {
					const messages = container.shadowRoot.querySelectorAll('[class*="ai"], [role="ai"]');
					messages.forEach(addButtonsToMessage);
				}
			}, 500);
		};

		const addButtonsToMessage = (messageElement) => {
			// Check if buttons already exist
			if (messageElement.querySelector('.message-actions-panel')) {
				return;
			}

			// Get message ID and text
			const messageId = messageElement.getAttribute('data-id') || messageElement.id;
			const messageText = messageElement.textContent || '';
			if (!messageId) {
				console.warn('DeepChatComponent: Could not find messageId for message element, skipping action buttons.', messageElement);
				return;
			}

			// Create buttons container
			const buttonsContainer = document.createElement('div');
			buttonsContainer.className = 'message-actions-panel';
			buttonsContainer.style.cssText = 'display: flex; gap: 8px; margin-top: 8px; padding-left: 4px;';

			// Create buttons
			const buttons = [
				{
					class: 'btn-like',
					icon: 'bi-hand-thumbs-up',
					title: 'Like',
					handler: () => handleLike(messageId)
				},
				{
					class: 'btn-dislike',
					icon: 'bi-hand-thumbs-down',
					title: 'Dislike',
					handler: () => handleDislike(messageId)
				},
				{
					class: 'btn-solution',
					icon: 'bi-check2-square',
					title: 'Mark as Solution',
					handler: () => handleMarkAsSolution(messageId)
				},
				{
					class: 'btn-copy',
					icon: 'bi-clipboard',
					title: 'Copy',
					handler: () => handleCopy(messageText)
				}
			];

			buttons.forEach(({ class: btnClass, icon, title, handler }) => {
				const button = document.createElement('button');
				button.className = `deep-chat-message-button ${btnClass}`;
				button.title = title;
				button.innerHTML = `<i class="bi ${icon}"></i>`;
				button.onclick = handler;
				buttonsContainer.appendChild(button);
			});

			// Append buttons to message
			messageElement.appendChild(buttonsContainer);
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

			// Setup action buttons for messages
			setTimeout(() => {
				addActionButtonsToMessages();
			}, 1000);
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
			chatStyle,
			auxiliaryStyle
		};
	}
};
</script>

<style scoped>
.deepchat-container {
	width: 100%;
	height: calc(100% - 42px);
	display: flex;
	flex-direction: column;
}

/* Message action buttons */
:deep(.message-actions-panel) {
	display: flex;
	gap: 8px;
	margin-top: 8px;
	padding-left: 4px;
}

:deep(.deep-chat-message-button) {
	background: none;
	border: none;
	padding: 4px 8px;
	cursor: pointer;
	font-size: 14px;
	color: #666;
	transition: all 0.2s ease;
	border-radius: 4px;
}

:deep(.deep-chat-message-button:hover) {
	background-color: rgba(0, 0, 0, 0.05);
	color: #333;
}

:deep(.deep-chat-message-button i) {
	font-size: 16px;
}

:deep(.btn-like:hover) {
	color: #4caf50;
}

:deep(.btn-dislike:hover) {
	color: #f44336;
}

:deep(.btn-solution:hover) {
	color: #2196f3;
}

:deep(.btn-copy:hover) {
	color: #ff9800;
}
</style>
