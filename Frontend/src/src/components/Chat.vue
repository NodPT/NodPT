<template>
	<div class="chat-container">
		<div class="chat-messages" ref="chatMessages">
			<div v-for="message in chatData.messages" :key="message.id"
				:class="['message', message.type === 'ai' ? 'ai-message' : 'user-message']">
				<div class="message-content">{{ message.content }}</div>
				<!-- Copy button available for both AI and user messages -->
				<div v-if="message.type !== 'ai'"
					class="message-controls d-flex justify-content-start align-items-center mt-1">
					<button @click="copyMessage(message)"
						:class="['btn', 'btn-sm btn-light', message._copied ? 'btn-success shadow-none' : 'btn-secondary shadow-none', 'me-1']"
						:disabled="isLoading" :title="message._copied ? 'Copied' : 'Copy to clipboard'">
						<i :class="message._copied ? 'bi bi-check-lg' : 'bi bi-clipboard'"></i>
					</button>
				</div>
				<div class="message-actions" v-if="message.type === 'ai'">
					<!-- Chat response buttons -->
					<div class="chat-response-buttons">
						<button @click="regenerateMessage(message)" class="btn btn-sm  me-1 shadow-none"
							:disabled="isLoading" title="Regenerate response">
							<i class="bi bi-arrow-clockwise"></i>
						</button>
						<button @click="likeMessage(message)"
							:class="['btn', 'btn-sm', message.liked ? 'btn-success shadow-none' : 'shadow-none', 'me-1']"
							:disabled="isLoading" title="Like this response">
							<i class="bi bi-hand-thumbs-up"></i>
						</button>
						<button @click="dislikeMessage(message)"
							:class="['btn', 'btn-sm', message.disliked ? 'btn-danger shadow-none' : 'shadow-none', 'me-1']"
							:disabled="isLoading" title="Dislike this response">
							<i class="bi bi-hand-thumbs-down"></i>
						</button>
					</div>
					<!-- Mark as Solution button -->
					<button v-if="!message.markedAsSolution" @click="markAsSolution(message)"
						class="btn btn-sm  ms-2 shadow-none" :disabled="isLoading" title="Mark as Solution">
						<i class="bi bi-check2-square"></i>
					</button>
					<span v-else class="badge bg-success fs-bold ms-2">
						<i class="bi bi-check2-square me-1"></i>
					</span>
				</div>
				<div class="message-time">
					{{ formatTime(message.timestamp) }}
				</div>
			</div>
		</div>
		<div class="chat-input fixed-bottom">
			<div class="input-group">
				<input v-model="newMessage" @keyup.enter="sendMessage" type="text" class="form-control"
					placeholder="Type your message..." :disabled="isLoading" />
				<button @click="sendMessage" class="btn btn-primary shadow-none" :disabled="isLoading">
					<span v-if="isLoading" class="spinner-border spinner-border-sm me-1"></span>
					Send
				</button>
				<button @click="triggerStartRequest" class="btn btn-success ms-2 shadow-none" title="Start AI Request">
					<i class="bi bi-rocket-fill"></i>
				</button>
			</div>
		</div>
	</div>
</template>

<script>
import { ref, reactive, inject, onMounted, nextTick, onBeforeUnmount } from 'vue';
import { eventBus, listenEvent, EVENT_TYPES } from '../rete/eventBus.js';
import chatApiService from '../service/chatApiService.js';
import signalRService from '../service/signalRService.js';

export default {
	name: 'Chat',
	setup() {
		// Inject API plugin
		const api = inject('api');
		chatApiService.setApi(api);

		// Reactive data for chat
		const chatData = reactive({ messages: [] });
		const newMessage = ref('');
		const isLoading = ref(false);

		// Refs for DOM elements
		const chatMessages = ref(null);

		const eventListeners = [];

		const createDefaultAiMessage = () => ({
			id: Date.now(),
			type: 'ai',
			content: 'How can I help you today?',
			timestamp: new Date().toISOString(),
			markedAsSolution: false,
			liked: false,
			disliked: false,
		});

		const resetChatMessages = async () => {
			chatData.messages = [createDefaultAiMessage()];
			newMessage.value = '';
			isLoading.value = false;

			await nextTick();
			scrollToBottom();
		};

		// Data cache to avoid repeated fetches
		const dataCache = reactive({
			chat: {},
		});

		// Current node context (for API calls)
		const currentNodeId = ref(null);

		// Fetch data from JSON files (fallback for offline mode)
		const fetchData = async (type, nodeKey = 'default') => {
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
			if (type === 'chat') {
				return { messages: [createDefaultAiMessage()] };
			}
			return {};
		};

		// Load chat data
		const loadChatData = async (nodeKey) => {
			const currentKey = nodeKey || 'default';

			try {
				// Try to load from API first, fallback to local data
				if (currentNodeId.value) {
					const apiMessages = await chatApiService.getPersistedMessagesByNodeId(currentNodeId.value);
					chatData.messages = apiMessages.map(msg => ({
						id: msg.oid,
						type: msg.sender === 'user' ? 'user' : 'ai',
						content: msg.message,
						timestamp: msg.timestamp,
						markedAsSolution: msg.markedAsSolution,
						liked: msg.liked || false,
						disliked: msg.disliked || false
					}));
				} else {
					// Fallback to local data
					const chatResult = await fetchData('chat', currentKey);
					Object.assign(chatData, chatResult);
				}

				if (!chatData.messages || chatData.messages.length === 0) {
					await resetChatMessages();
				} else {
					// Auto-scroll chat
					await nextTick();
					scrollToBottom();
				}
			} catch (error) {
				console.error('Error loading chat data:', error);
				// Load fallback data
				const chatResult = await fetchData('chat', currentKey);
				Object.assign(chatData, chatResult);
				if (!chatData.messages || chatData.messages.length === 0) {
					await resetChatMessages();
				} else {
					await nextTick();
					scrollToBottom();
				}
			}
		};

		// Utility functions
		const formatTime = (timestamp) => {
			if (!timestamp) return '';
			const date = new Date(timestamp);
			return date.toLocaleString();
		};

		const scrollToBottom = () => {
			if (chatMessages.value) {
				chatMessages.value.scrollTop = chatMessages.value.scrollHeight;
			}
		};

		// Helper function to add AI response to chat
		const addAiResponseToChat = (response) => {
			if (response.aiResponse) {
				const aiMessage = {
					id: response.aiResponse.id,
					type: 'ai',
					content: response.aiResponse.message,
					timestamp: response.aiResponse.timestamp,
					markedAsSolution: response.aiResponse.markedAsSolution,
					liked: response.aiResponse.liked || false,
					disliked: response.aiResponse.disliked || false
				};
				chatData.messages.push(aiMessage);
				scrollToBottom();
			}
		};

		// Chat functions
		const sendMessage = async () => {
			if (!newMessage.value.trim() || isLoading.value) return;

			const userMessageContent = newMessage.value;
			newMessage.value = '';
			isLoading.value = true;

			try {
				// Add user message to UI immediately
				const userMessage = {
					id: Date.now(),
					type: 'user',
					content: userMessageContent,
					timestamp: new Date().toISOString(),
				};
				chatData.messages.push(userMessage);
				scrollToBottom();

				// Get SignalR connection ID
				const connectionId = signalRService.getConnectionId();
				
				// If we have a current node ID, submit to the backend for AI processing
				if (currentNodeId.value) {
					try {
						// Submit chat message with nodeId for backend processing
						await chatApiService.submitChatMessage({
							nodeId: currentNodeId.value,
							message: userMessageContent,
							connectionId: connectionId
						});
						
						// The AI response will be received via SignalR
						// For now, we just acknowledge the submission
						console.log('Chat message submitted for AI processing');
					} catch (submitError) {
						console.error('Error submitting chat for AI processing:', submitError);
						// Fallback to old behavior if submit fails
						const response = await chatApiService.sendMessage({
							content: userMessageContent,
							nodeId: currentNodeId.value
						});
						addAiResponseToChat(response);
					}
				} else {
					// No node context, use old send behavior
					const response = await chatApiService.sendMessage({
						content: userMessageContent,
						nodeId: currentNodeId.value
					});
					addAiResponseToChat(response);
				}

			} catch (error) {
				console.error('Error sending message:', error);
				// Fallback to local simulation
				setTimeout(() => {
					const aiResponse = {
						id: Date.now() + 1,
						type: 'ai',
						content: "I'm having trouble connecting to the server, but I'm processing your request locally...",
						timestamp: new Date().toISOString(),
						markedAsSolution: false,
						liked: false,
						disliked: false
					};
					chatData.messages.push(aiResponse);
					scrollToBottom();
				}, 1000);
			} finally {
				isLoading.value = false;
			}
		};

		// Mark message as solution and get comprehensive response
		const markAsSolution = async (message) => {
			if (isLoading.value) return;

			isLoading.value = true;

			try {
				// Mark the message as solution
				message.markedAsSolution = true;

				// Get comprehensive solution from API
				const solutionResponse = await chatApiService.markAsSolution(currentNodeId.value);

				// Add comprehensive solution to chat
				const comprehensiveMessage = {
					id: solutionResponse.id,
					type: 'ai',
					content: solutionResponse.message,
					timestamp: solutionResponse.timestamp,
					markedAsSolution: true,
					liked: false,
					disliked: false
				};
				chatData.messages.push(comprehensiveMessage);
				scrollToBottom();

				// Call demo function from demo.js as specified in requirements
				try {
					// Import and call demo function - this needs to be dynamic
					const { createDemoNodes } = await import('../rete/demo.js');
					if (typeof createDemoNodes === 'function') {
						// Get editor manager from global context or event bus
						eventBus.emit('CALL_DEMO_FUNCTION', { action: 'createDemoNodes' });
						console.log('Demo function called after marking solution');
					}
				} catch (demoError) {
					console.error('Error calling demo function:', demoError);
				}

			} catch (error) {
				console.error('Error marking as solution:', error);
				// Fallback behavior
				message.markedAsSolution = true;
				const fallbackSolution = {
					id: Date.now() + 1,
					type: 'ai',
					content: "Here's a comprehensive solution: I've analyzed your request and recommend a structured approach to address your needs. This includes implementing best practices, optimizing performance, and ensuring maintainability.",
					timestamp: new Date().toISOString(),
					markedAsSolution: true,
					liked: false,
					disliked: false
				};
				chatData.messages.push(fallbackSolution);
				scrollToBottom();
			} finally {
				isLoading.value = false;
			}
		};

		// Like message handler
		const likeMessage = async (message) => {
			if (isLoading.value) return;

			try {
				await chatApiService.likeMessage(message.id);
				// Update local state
				message.liked = !message.liked;
				message.disliked = false; // Can't be both liked and disliked
			} catch (error) {
				console.error('Error liking message:', error);
				// Fallback: just update local state
				message.liked = !message.liked;
				message.disliked = false;
			}
		};

		// Dislike message handler
		const dislikeMessage = async (message) => {
			if (isLoading.value) return;

			try {
				await chatApiService.dislikeMessage(message.id);
				// Update local state
				message.disliked = !message.disliked;
				message.liked = false; // Can't be both liked and disliked
			} catch (error) {
				console.error('Error disliking message:', error);
				// Fallback: just update local state
				message.disliked = !message.disliked;
				message.liked = false;
			}
		};

		// Regenerate message handler
		const regenerateMessage = async (message) => {
			if (isLoading.value) return;

			isLoading.value = true;

			try {
				const newMessage = await chatApiService.regenerateMessage(message.id);

				// Add the new regenerated message to the chat
				const regeneratedMessage = {
					id: newMessage.id,
					type: 'ai',
					content: newMessage.message,
					timestamp: newMessage.timestamp,
					markedAsSolution: false,
					liked: false,
					disliked: false
				};
				chatData.messages.push(regeneratedMessage);
				scrollToBottom();
			} catch (error) {
				console.error('Error regenerating message:', error);
				// Fallback: create a local regenerated message
				const fallbackRegenerated = {
					id: Date.now() + Math.random(),
					type: 'ai',
					content: "[Regenerated] I apologize for the connection issue. Let me provide an alternative response: " + message.content.substring(0, 50) + "... (regenerated locally)",
					timestamp: new Date().toISOString(),
					markedAsSolution: false,
					liked: false,
					disliked: false
				};
				chatData.messages.push(fallbackRegenerated);
				scrollToBottom();
			} finally {
				isLoading.value = false;
			}
		};

		// Copy message content to clipboard with transient UI feedback
		const copyMessage = async (message) => {
			if (!message || !message.content) return;
			try {
				const text = message.content;
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

				// show temporary feedback on the message
				message._copied = true;
				setTimeout(() => { message._copied = false; }, 1600);
			} catch (err) {
				console.error('Copy failed', err);
			}
		};

		// Function to trigger START_Request event
		const triggerStartRequest = () => {
			const sampleData = JSON.stringify({
				type: 'START_Request',
				timestamp: new Date().toISOString(),
				data: {
					message: 'AI processing request initiated',
					context: 'chat_interface',
					requestId: Date.now()
				}
			});

			eventBus.emit('START_Request', sampleData);
			console.log('START_Request event triggered with data:', sampleData);
		};

		// Listen for node selection changes
		const handleNodeSelection = (nodeData) => {
			currentNodeId.value = nodeData.id;
			console.log('Node selected for chat context:', nodeData);
			// Reload chat data for this node
			loadChatData(nodeData.id);
		};

		const handleProjectContextChange = async () => {
			await resetChatMessages();
		};

		// Load initial data on mount
		onMounted(() => {
			loadChatData('default');

			// Listen for node selection events
			eventListeners.push(listenEvent(EVENT_TYPES.NODE_SELECTED, handleNodeSelection));
			eventListeners.push(listenEvent(EVENT_TYPES.PROJECT_CONTEXT_CHANGED, handleProjectContextChange));
		});

		onBeforeUnmount(() => {
			eventListeners.forEach((unsubscribe) => {
				if (typeof unsubscribe === 'function') {
					unsubscribe();
				}
			});
		});

		return {
			// Data
			chatData,
			newMessage,
			isLoading,

			// Refs
			chatMessages,

			// Methods
			formatTime,
			sendMessage,
			markAsSolution,
			likeMessage,
			dislikeMessage,
			regenerateMessage,
			copyMessage,
			triggerStartRequest,
		};
	},
};
</script>

<style scoped src="../assets/styles/chat.css"></style>
