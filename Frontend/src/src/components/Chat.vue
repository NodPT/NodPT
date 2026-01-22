<template>
	<div class="chat-container">
		<div class="chat-messages" ref="chatMessages">
			<div v-for="message in chatData.messages" :key="message.id"
				:class="['message', message.type === 'ai' || message.type === 'thinking' ? 'ai-message' : 'user-message']">
				<!-- message content -->
				<div class="message-content" :class="[message.thinking ? 'thinking' : '']"
					v-html="renderMarkdown(message.content)"></div>
				<!-- Copy button available for user messages -->
				<div v-if="message.type !== 'ai' && message.type !== 'thinking'"
					class="message-controls d-flex justify-content-start align-items-center mt-1">
					<button @click="copyMessage(message)" class="action-btn" :disabled="isLoading"
						:title="message._copied ? 'Copied' : 'Copy to clipboard'">
						<i :class="['bi', message._copied ? 'bi-check-lg fw-bold' : 'bi-clipboard fw-bold']"></i>
					</button>
				</div>
				<div class="message-actions" v-if="message.type === 'ai' || message.type === 'thinking'">
					<!-- Chat response buttons -->
					<div class="chat-response-buttons" v-if="!message.thinking">
						<button @click="likeMessage(message)" class="action-btn" :disabled="isLoading"
							title="Like this response">
							<i :class="['bi', 'bi-hand-thumbs-up', 'fw-bold', message.Liked ? 'text-success' : '']"></i>
						</button>
						<button @click="dislikeMessage(message)" class="action-btn" :disabled="isLoading"
							title="Dislike this response">
							<i
								:class="['bi', 'bi-hand-thumbs-down', 'fw-bold', message.Disliked ? 'text-danger' : '']"></i>
						</button>
						<button @click="copyMessage(message)" class="action-btn" :disabled="isLoading"
							:title="message._copied ? 'Copied' : 'Copy to clipboard'">
							<i :class="['bi', message._copied ? 'bi-check-lg fw-bold' : 'bi-clipboard fw-bold']"></i>
						</button>
						<button v-if="!message.markedAsSolution" @click="buildSolution(message)" class="action-btn"
							:disabled="isLoading" title="Mark as solution">
							<i class="bi bi-check2-circle fw-bold"></i>
						</button>
						<span v-if="message.markedAsSolution" class="badge bg-success ms-2">
							<i class="bi bi-check2-square me-1 fw-bold"></i>
							<span>Solution</span>
						</span>
					</div>
				</div>
				<div class="message-time">
					{{ formatTime(message.timestamp) }}
				</div>
			</div>
		</div>
		<div class="chat-input-container">
			<div class="chat-input-wrapper">
				<textarea v-model="newMessage" @keydown.enter.exact="handleEnter" ref="messageTextarea"
					class="chat-textarea form-control" placeholder="Ask anything..." :disabled="isLoading"
					rows="1"></textarea>
				<button @click="sendMessage" class="btn btn-primary send-btn" :disabled="isSendDisabled">
					<span v-if="isLoading" class="spinner-border spinner-border-sm"></span>
					<i v-else class="bi fw-bold" :class="[thinking ? 'bi-square-fill' : 'bi-send']"></i>
				</button>
			</div>
		</div>
	</div>
</template>

<script>
import { ref, reactive, inject, onMounted, nextTick, onBeforeUnmount, watch, computed } from 'vue';
import { listenEvent, EVENT_TYPES, triggerEvent } from '../rete/eventBus.js';
import chatApiService from '../service/chatApiService.js';
import nodeApiService from '../service/nodeApiService.js';
import { marked } from 'marked';
import DOMPurify from 'dompurify';

export default {
	name: 'Chat',
	setup() {
		// Inject API plugin
		const api = inject('api');
		chatApiService.setApi(api);
		nodeApiService.setApi(api);

		// Reactive data for chat
		const chatData = reactive({ messages: [] });
		const newMessage = ref('');
		const isLoading = ref(false);
		const thinking = ref(false);

		// Computed properties
		const isSendDisabled = computed(() => isLoading.value || !newMessage.value.trim());

		// Refs for DOM elements
		const chatMessages = ref(null);
		const messageTextarea = ref(null);

		const eventListeners = [];

		// Constants
		const MAX_TEXTAREA_HEIGHT = 150; // pixels - matches CSS --chat-input-max-height

		// Configure marked for markdown rendering with security options
		marked.setOptions({
			breaks: true,
			gfm: true,
			headerIds: false,
			mangle: false,
		});

		// Render markdown content with sanitization
		// All content (user and AI) goes through DOMPurify to prevent XSS
		const renderMarkdown = (content) => {
			if (!content) return '';
			const rawHtml = marked(content);
			// DOMPurify removes all potentially dangerous HTML/JS
			// Only allow safe tags and attributes on specific elements
			return DOMPurify.sanitize(rawHtml, {
				ALLOWED_TAGS: ['p', 'br', 'strong', 'em', 'code', 'pre', 'a', 'ul', 'ol', 'li', 'blockquote'],
				ALLOWED_ATTR: {
					'a': ['href', 'target', 'rel']
				},
				ALLOWED_URI_REGEXP: /^(?:(?:https?|mailto):)/i,
				ADD_ATTR: ['target', 'rel'],
				FORBID_ATTR: ['style', 'onerror', 'onload'],
				transformTags: {
					'a': (el) => {
						const href = el.getAttribute('href') || '';
						// Only set target/rel for external links (http/https)
						if (/^https?:\/\//i.test(href)) {
							el.setAttribute('target', '_blank');
							el.setAttribute('rel', 'noopener noreferrer');
						}
						return el;
					}
				}
			});
		};

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

		// Auto-resize textarea based on content
		const autoResizeTextarea = () => {
			if (!messageTextarea.value) return;

			// Reset height to auto to get the correct scrollHeight
			messageTextarea.value.style.height = 'auto';

			// Set height based on scrollHeight, with max height constant
			const newHeight = Math.min(messageTextarea.value.scrollHeight, MAX_TEXTAREA_HEIGHT);
			messageTextarea.value.style.height = `${newHeight}px`;
		};

		// Handle Enter key - send on Enter, new line on Shift+Enter
		const handleEnter = (event) => {
			if (!event.shiftKey) {
				event.preventDefault();
				sendMessage();
			}
		};

		// Watch for message changes to auto-resize textarea
		watch(newMessage, () => {
			nextTick(() => {
				autoResizeTextarea();
			});
		});

		const removeThinkingMessage = () => {
			const thinkingIndex = chatData.messages.findIndex(msg => msg.thinking);
			if (thinkingIndex !== -1) {
				chatData.messages.splice(thinkingIndex, 1);
			}
		};

		// Chat functions	
		const sendMessage = async () => {

			if (thinking.value) {
				thinking.value = false;
				removeThinkingMessage();
				return;
			}


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

				// add a temporary AI message indicating thinking
				const aiThinkingMessage = {
					id: Date.now() + 1,
					type: 'ai',
					content: 'thinking...',
					timestamp: new Date().toISOString(),
					markedAsSolution: false,
					liked: false,
					disliked: false,
					thinking: true,
				};
				thinking.value = true;
				chatData.messages.push(aiThinkingMessage);
				scrollToBottom();

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

		const createManagerNodes = async (parentNodeId, managers) => {
			if (!parentNodeId || !Array.isArray(managers) || managers.length === 0) {
				return;
			}

			let projectId;
			try {
				const parentNode = await nodeApiService.getNode(parentNodeId);
				projectId = parentNode?.ProjectId ?? parentNode?.projectId;
			} catch (error) {
				console.error('Failed to load parent node for solution output:', error);
				return;
			}

			if (!projectId) {
				console.error('Project ID not found for solution output');
				return;
			}

			for (const manager of managers) {
				if (!manager) {
					continue;
				}

				const managerName = (manager.name || manager.Name || 'Manager').trim();
				const managerJob = manager.job || manager.Job;

				try {
					const createdNode = await nodeApiService.createNode({
						Name: managerName,
						MessageType: 'Discussion',
						ParentId: parentNodeId,
						ProjectId: parseInt(projectId, 10),
						Status: 'Active',
						Properties: managerJob ? { Job: managerJob } : {},
					});

					triggerEvent(EVENT_TYPES.NODE_CREATED_FROM_API, {
						nodeData: createdNode,
						parentId: parentNodeId,
					});
				} catch (error) {
					console.error('Failed to create manager node from solution output:', error);
				}
			}

			triggerEvent(EVENT_TYPES.ARRANGE_NODES);
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

		// Listen for node selection changes
		const handleNodeSelection = (nodeData) => {
			if (currentNodeId.value === nodeData.id) {
				// Same node selected, no action needed
				return;
			}
			currentNodeId.value = nodeData.id;
			console.log('Node selected for chat context:', nodeData);
			// Reload chat data for this node
			loadChatData(nodeData.id);
		};

		const handleProjectContextChange = async () => {
			await resetChatMessages();
		};

		// Handle AI response received from SignalR
		// This is triggered when Executor processes a chat message and sends response via Redis/SignalR
		const handleAIResponseReceived = (data) => {
			console.log('AI response received via SignalR:', data);

			// Check if this is a thinking/progress message
			if (data.messageType === 'thinking') {
				// Remove existing thinking message if any
				removeThinkingMessage();

				// Add or update the thinking message with UUID-like ID to prevent collisions
				// Note: type is 'thinking' to distinguish from actual AI replies
				const thinkingMessage = {
					id: 'thinking-' + Date.now().toString(36) + Math.random().toString(36).slice(2),
					type: 'thinking',
					content: data.content,
					timestamp: data.timestamp,
					thinking: true,
					markedAsSolution: false,
					Liked: false,
					Disliked: false,
				};

				chatData.messages.push(thinkingMessage);
				thinking.value = true;
				scrollToBottom();
				return;
			}

			// This is a regular AI response - remove any thinking messages
			removeThinkingMessage();
			thinking.value = false;
			isLoading.value = false;

			// Validate that the response is for the current node
			if (currentNodeId.value && data.nodeId && data.nodeId !== currentNodeId.value) {
				console.warn('Received AI response for different node, ignoring:', data.nodeId);
				return;
			}

			// Add the AI message to the chat
			const aiMessage = {
				id: data.messageId,
				type: 'ai',
				content: data.content,
				timestamp: data.timestamp,
				markedAsSolution: false,
				Liked: false,
				Disliked: false,
			};

			chatData.messages.push(aiMessage);
			scrollToBottom();
		};

		const handleSolutionOutputReceived = async (data) => {
			if (!data || !data.nodeId) {
				return;
			}

			if (!data.content) {
				console.warn('Solution output missing content');
				return;
			}

			let solutionPayload;
			try {
				solutionPayload = JSON.parse(data.content);
			} catch (error) {
				console.error('Failed to parse solution output JSON:', error);
				return;
			}

			if (solutionPayload?.message && data.messageId && data.nodeId === currentNodeId.value) {
				const targetMessage = chatData.messages.find(msg => msg.id === data.messageId);
				if (targetMessage) {
					targetMessage.content = solutionPayload.message;
				}
			}

			const managers = Array.isArray(solutionPayload?.managers) ? solutionPayload.managers : [];
			if (!managers.length) {
				return;
			}

			await createManagerNodes(data.nodeId, managers);
		};

		// Load initial data on mount
		onMounted(() => {
			loadChatData('default');

			// Listen for node selection events
			eventListeners.push(listenEvent(EVENT_TYPES.NODE_SELECTED, handleNodeSelection));
			eventListeners.push(listenEvent(EVENT_TYPES.PROJECT_CONTEXT_CHANGED, handleProjectContextChange));
			// Listen for AI responses from SignalR
			eventListeners.push(listenEvent(EVENT_TYPES.SIGNALR_AI_RESPONSE_RECEIVED, handleAIResponseReceived));
			eventListeners.push(listenEvent(EVENT_TYPES.SOLUTION_OUTPUT_RECEIVED, handleSolutionOutputReceived));
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
			thinking,
			// Refs
			chatMessages,
			messageTextarea,

			// Computed
			isSendDisabled,

			// Methods
			formatTime,
			sendMessage,
			buildSolution,
			likeMessage,
			dislikeMessage,
			copyMessage,
			renderMarkdown,
			handleEnter,
		};
	},
};
</script>

<style scoped src="../assets/styles/chat.css"></style>
