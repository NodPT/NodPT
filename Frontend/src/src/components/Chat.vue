<template>
	<div class="chat-container">
		<div class="chat-messages" ref="chatMessages">
			<div v-for="message in chatData.messages" :key="message.id"
				:class="['message', message.type === 'ai' ? 'ai-message' : 'user-message']">
				<div class="message-content">{{ message.content }}</div>
				<!-- Copy button available for user messages -->
				<div v-if="message.type !== 'ai'"
					class="message-controls d-flex justify-content-start align-items-center mt-1">
					<button @click="copyMessage(message)"
						class="action-btn"
						:disabled="isLoading" :title="message._copied ? 'Copied' : 'Copy to clipboard'">
						<i :class="['bi', message._copied ? 'bi-check-lg fw-bold' : 'bi-clipboard fw-bold']"></i>
					</button>
				</div>
				<div class="message-actions" v-if="message.type === 'ai'">
					<!-- Chat response buttons -->
					<div class="chat-response-buttons">
						<button @click="likeMessage(message)"
							class="action-btn"
							:disabled="isLoading" title="Like this response">
							<i :class="['bi', 'bi-hand-thumbs-up', 'fw-bold', message.Liked ? 'text-success' : '']"></i>
						</button>
						<button @click="dislikeMessage(message)"
							class="action-btn"
							:disabled="isLoading" title="Dislike this response">
							<i :class="['bi', 'bi-hand-thumbs-down', 'fw-bold', message.Disliked ? 'text-danger' : '']"></i>
						</button>
						<button @click="copyMessage(message)"
							class="action-btn"
							:disabled="isLoading" :title="message._copied ? 'Copied' : 'Copy to clipboard'">
							<i :class="['bi', message._copied ? 'bi-check-lg fw-bold' : 'bi-clipboard fw-bold']"></i>
						</button>
					</div>
					<!-- Build Solution button - shown when message content contains solution=true -->
					<button v-if="hasSolution(message) && !message.markedAsSolution" @click="buildSolution(message)"
						class="action-btn ms-2" :disabled="isLoading" title="Build Solution">
						<i class="bi bi-tools fw-bold"></i>
						<span class="ms-1">Build Solution</span>
					</button>
					<span v-if="message.markedAsSolution" class="badge bg-success ms-2">
						<i class="bi bi-check2-square me-1 fw-bold"></i>
						<span>Solution</span>
					</span>
				</div>
				<div class="message-time">
					{{ formatTime(message.timestamp) }}
				</div>
			</div>
		</div>
		<div class="chat-input">
			<div class="input-group">
				<input v-model="newMessage" @keyup.enter="sendMessage" type="text" class="form-control"
					placeholder="Type your message..." :disabled="isLoading" />
				<button @click="sendMessage" class="btn btn-primary" :disabled="isLoading">
					<span v-if="isLoading" class="spinner-border spinner-border-sm me-1"></span>
					<i v-else class="bi bi-send fw-bold"></i>
				</button>
			</div>
		</div>
	</div>
</template>

<script>
import { ref, reactive, inject, onMounted, nextTick, onBeforeUnmount } from 'vue';
import { eventBus, listenEvent, EVENT_TYPES } from '../rete/eventBus.js';
import chatApiService from '../service/chatApiService.js';

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
				// Only load from API if we have a nodeId
				if (currentNodeId.value) {
					const apiMessages = await chatApiService.getMessagesByNodeId(currentNodeId.value);
					chatData.messages = apiMessages.map(msg => ({
						id: msg.Id,
						type: msg.Sender === 'user' ? 'user' : 'ai',
						content: msg.Message,
						timestamp: msg.Timestamp,
						markedAsSolution: msg.MarkedAsSolution,
						Liked: msg.Liked || false,
						Disliked: msg.Disliked || false
					}));
				} else {
					// No node selected, show default message
					chatData.messages = [createDefaultAiMessage()];
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
				// Load default message on error
				await resetChatMessages();
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

		// Chat functions
		const sendMessage = async () => {
			if (!newMessage.value.trim() || isLoading.value) return;

			// Validate that we have a nodeId
			if (!currentNodeId.value) {
				console.error('Cannot send message: No node selected');
				alert('Please select a node first');
				return;
			}

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

				// Send to API (will be queued to Redis for AI processing)
				const response = await chatApiService.sendMessage({
					content: userMessageContent,
					nodeId: currentNodeId.value
				});

				// Update the user message ID with the saved message ID
				if (response.userMessage) {
					userMessage.id = response.userMessage.Id;
				}

				// AI response will come through SignalR, not in HTTP response
				console.log('Message sent and queued for AI processing:', response);

			} catch (error) {
				console.error('Error sending message:', error);
				// Show error to user
				const errorMessage = {
					id: Date.now() + 1,
					type: 'ai',
					content: "Sorry, I'm having trouble processing your request. Please try again later.",
					timestamp: new Date().toISOString(),
					markedAsSolution: false,
					liked: false,
					disliked: false
				};
				chatData.messages.push(errorMessage);
				scrollToBottom();
			} finally {
				isLoading.value = false;
			}
		};

		// Check if message content contains solution=true
		const hasSolution = (message) => {
			if (!message || !message.content) return false;
			// Check if message content contains solution=true (case-insensitive)
			return /solution\s*=\s*true/i.test(message.content);
		};

		// Build solution from AI message
		const buildSolution = async (message) => {
			if (isLoading.value) return;

			isLoading.value = true;

			try {
				// Call the mark as solution API
				await chatApiService.markAsSolution(message.id, currentNodeId.value);
				
				// Update UI to reflect the change
				message.markedAsSolution = true;

				console.log('Solution built for message:', message.id);

			} catch (error) {
				console.error('Error building solution:', error);
				// Revert UI change on error
				message.markedAsSolution = false;
				alert('Failed to build solution. Please try again.');
			} finally {
				isLoading.value = false;
			}
		};

		// Like message handler
		const likeMessage = async (message) => {
			if (isLoading.value) return;

			try {
				const result = await chatApiService.likeMessage(message.id);
				// Update local state with server response
				message.Liked = result.Liked;
				message.Disliked = result.Disliked;
			} catch (error) {
				console.error('Error liking message:', error);
				alert('Failed to like message. Please try again.');
			}
		};

		// Dislike message handler
		const dislikeMessage = async (message) => {
			if (isLoading.value) return;

			try {
				const result = await chatApiService.dislikeMessage(message.id);
				// Update local state with server response
				message.Liked = result.Liked;
				message.Disliked = result.Disliked;
			} catch (error) {
				console.error('Error disliking message:', error);
				alert('Failed to dislike message. Please try again.');
			}
		};

		// Regenerate message handler (not implemented - will use Redis queue)
		const regenerateMessage = async (message) => {
			if (isLoading.value) return;

			alert('Message regeneration is not yet implemented. This feature will queue a new AI request.');
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
			buildSolution,
			hasSolution,
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
