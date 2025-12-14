import * as Rete from 'rete';

import { ClassicPreset } from 'rete';
import { triggerEvent, listenEvent, EVENT_TYPES } from './eventBus.js';
import { SimpleNode, NODE_TYPES } from './SimpleNode.js';
// Import Rete.js and required plugins
import { VuePlugin, Presets } from 'rete-vue-plugin';
import { AreaExtensions } from 'rete-area-plugin';
import { AreaPlugin } from 'rete-area-plugin';
import { ConnectionPlugin, Presets as ConnectionPresets } from 'rete-connection-plugin';
import { MinimapPlugin } from 'rete-minimap-plugin';
import { ContextMenuPlugin, Presets as ContextMenuPresets } from 'rete-context-menu-plugin';
import { HistoryPlugin } from 'rete-history-plugin';
import { AutoArrangePlugin, Presets as ArrangePresets } from 'rete-auto-arrange-plugin';
import { ArrangeAppliers } from 'rete-auto-arrange-plugin';
import { ScopesPlugin, Presets as ScopesPresets } from 'rete-scopes-plugin';
import CustomNode from '../components/CustomNode.vue';
/**
 * Editor manager for handling Rete.js editor lifecycle and initialization
 */
export class EditorManager {
	constructor() {
		this.editor = null;
		this.area = null;
		this.historyPlugin = null;
		this.minimapPlugin = null;
		this.director = null;
		this.groups = [];
		this.nodes = [];
	}

	/**
	 * Initialize the Rete editor with the given container
	 * @param {HTMLElement} container - The DOM container element
	 * @param {Function} emit - Vue emit function for parent communication
	 * @returns {Promise<Object>} Editor instances
	 */
	async initializeEditor(container, emit = null) {
		if (!container) {
			console.warn('Canvas element not available');
			throw new Error('Container element is required');
		}

		try {
			// Create editor
			// For Rete.js 2.x, we need to initialize with an ID
			// Generate a unique ID for the editor to avoid conflicts
			const editorId = `demo@0.1.0_${Date.now()}`;

			// Create editor instance
			const editor = new Rete.NodeEditor(editorId);

			// Create area plugin
			const area = new AreaPlugin(container);

			// Create Vue plugin with proper preset
			const render = new VuePlugin();

			// Use the classic preset for node rendering (default Rete.js node)
			render.addPreset(
				Presets.classic.setup({
					customize: {
						node() {
							return CustomNode;
						},
					},
				}),
			);

			// Create other plugins
			const connectionPlugin = new ConnectionPlugin();
			// Add connection preset for proper socket connections
			connectionPlugin.addPreset(ConnectionPresets.classic.setup());

			const minimapPlugin = new MinimapPlugin();
			const contextMenuPlugin = new ContextMenuPlugin({
				items(context, plugin) {
					if (context === 'root') {
						return {
							searchBar: true,
							list: [
								{ label: 'New Node', key: '1', handler: () => console.log('Add node from context menu') },
								{
									label: 'Collection',
									key: '1',
									handler: () => null,
									subitems: [{ label: 'Subitem', key: '1', handler: () => console.log('Subitem') }],
								},
							],
						};
					}

					return {
						list: [{ label: 'Delete', key: '1', handler: () => console.log('Delete') }],
					};
				},
			});

			// using scope
			const scopes = new ScopesPlugin();
			scopes.addPreset(ScopesPresets.classic.setup());

			// Register plugins with editor and area
			editor.use(area);
			area.use(render);
			area.use(connectionPlugin);
			area.use(minimapPlugin);
			// area.use(contextMenuPlugin);
			area.use(scopes);

			render.addPreset(Presets.minimap.setup({ size: 200 }));
			render.addPreset(Presets.contextMenu.setup());

			// Use auto arrange plugin
			this.arrange = new AutoArrangePlugin();
			this.arrange.addPreset(ArrangePresets.classic.setup());
			area.use(this.arrange);

			// Additionally, extensions offer various capabilities, like enabling the user to select nodes.
			const selector = AreaExtensions.selector();
			const accumulating = AreaExtensions.accumulateOnCtrl();
			AreaExtensions.selectableNodes(area, selector, { accumulating });

			// Add support for connecting nodes
			// AreaExtensions.simpleNodesOrder(area);
			AreaExtensions.showInputControl(area);

			// Add history plugin for undo/redo functionality
			let historyPlugin = null;
			historyPlugin = new HistoryPlugin();
			area.use(historyPlugin);

			// Disable Rete's default zoom behavior to allow native scrolling
			// Scrolling is enabled via CSS (overflow: auto on container and min-width/min-height on canvas)
			// area.addPipe((context) => {
			// 	// Block zoom events to prevent zoom-on-wheel behavior
			// 	if (context.type === 'zoom') {
			// 		return; // Don't process zoom events
			// 	}
			// 	return context;
			// });

			// // Listen for node creation to set panel nodes to 'off' status
			// editor.addPipe((context) => {
			// 	if (context.type === 'nodecreated') {
			// 		const node = context.data;
			// 		// Check if this is a panel node (has panel_node in the ID)
			// 		if (node && node.id && node.id.includes('panel_node')) {
			// 			// Set the status to 'off' to hide the icon
			// 			node.status = 'off';
			// 			setTimeout(() => {
			// 				triggerEvent(EVENT_TYPES.NODE_STATUS_CHANGE, {
			// 					nodeId: node.id,
			// 					status: 'off'
			// 				});
			// 				area.update('node', node.id);
			// 			}, 100);
			// 		}
			// 	}
			// 	return context;
			// });

			// // Listen for node add events
			// editor.addPipe((context) => {
			// 	if (context.type === 'nodecreated') {
			// 		console.log('Node created event:', context);
			// 		area.update();
			// 	}
			// 	return context;
			// });

			// // Listen for connection events
			// connectionPlugin.addPipe((context) => {
			// 	if (context.type === 'connectioncreate') {
			// 		console.log('Connection created:', context.data);
			// 	} else if (context.type === 'connectionremove') {
			// 		console.log('Connection removed:', context.data);
			// 	} else if (context.type === 'connectionpick') {
			// 		console.log('Connection pick (started dragging):', context.data);
			// 	} else if (context.type === 'connectiondrop') {
			// 		console.log('Connection drop (finished dragging):', context.data);
			// 	}
			// 	return context;
			// });

			this.editor = editor;
			this.area = area;
			this.historyPlugin = historyPlugin;
			this.minimapPlugin = minimapPlugin;
			this.scopes = scopes;

			// Setup node selection handler
			this.area.addPipe((context) => {
				if (context.type === 'nodetranslated') {
				} else if (context.type === 'nodepicked') {
					// Handle node picked event
					const pickedNode = this.editor.getNode(context.data.id);
					const nodeId = pickedNode.id;

					// Trigger node selected event via eventBus
					triggerEvent(EVENT_TYPES.NODE_SELECTED, {
						id: nodeId,
						name: pickedNode.label,
						data: {
							label: pickedNode.label,
							inputs: Object.keys(pickedNode.inputs || {}),
							outputs: Object.keys(pickedNode.outputs || {}),
						},
					});
				}
				return context;
			});

			// listen to events
			listenEvent(EVENT_TYPES.UNDO, this.undo.bind(this));
			listenEvent(EVENT_TYPES.REDO, this.redo.bind(this));
			listenEvent(EVENT_TYPES.ARRANGE_NODES, this.arrangeNodes.bind(this));
			listenEvent(EVENT_TYPES.ZOOM_FIT, this.zoomFit.bind(this));
			listenEvent(EVENT_TYPES.NODE_REFRESH, this.handleNodeRefresh.bind(this));
			listenEvent(EVENT_TYPES.SEARCH_FOCUS_NODE, this.handleFocusNode.bind(this));
			listenEvent(EVENT_TYPES.TOGGLE_MINIMAP, this.toggleMinimap.bind(this));
			listenEvent(EVENT_TYPES.NODE_ACTION, this.handleNodeAction.bind(this));

			// Inject translatePosition method to area object
			// Usage: area.translatePosition(nodeId, { x, y }, duration)
			// This method animates a node from its parent's position to the target position
			this.area.translatePosition = (nodeId, targetPosition, duration = 1000) => {
				return this.translatePosition(nodeId, targetPosition, duration);
			};

			this.area.resize();
			this.area.update();

			// Set status to 'off' for all panel nodes after initialization
			setTimeout(() => {
				this.hideAllPanelNodeIcons();
			}, 500);

			// Trigger editor ready event
			setTimeout(() => {
				triggerEvent(EVENT_TYPES.EDITOR_READY, {
					nodeCount: this.editor.getNodes().length,
				});
			}, 100);

			return {
				editor: this.editor,
				area: this.area,
				historyPlugin,
			};
		} catch (error) {
			console.error('Failed to initialize Rete editor:', error);
			throw error;
		}
	}

	/**
	 * Arrange nodes with animation based on the hierarchy: Director > Managers > Inspectors > Agents
	 * Each level follows specific positioning rules
	 */
	async arrangeNodes() {
		if (!this.nodes.length) return;

		// Process each parent node
		for (let nodeInfo of this.nodes) {
			// Only process nodes with children
			if (!nodeInfo.children.length) continue;

			// Get parent node position
			const parentWidth = nodeInfo.node.width || 100;
			const parentHeight = nodeInfo.node.height || 100;

			// Different arrangement based on node type
			if (nodeInfo.nodeType === NODE_TYPES.DIRECTOR) {
				// For Director nodes: arrange managers horizontally
				await this.arrangeDirectorChildren(nodeInfo, 30, 60, parentWidth, parentHeight);
				break;
			}
		}

		// Update the entire area after all nodes are positioned
		this.area.update();
	}

	/**
	 * Arrange Director node's children (Managers) in a horizontal line
	 */
	async arrangeDirectorChildren(directorNode, parentX, parentY, parentWidth, parentHeight) {
		let col1 = [];
		let col2 = [];

		// Director nodes: position managers horizontally with spacing
		// Based on the screenshot, position them to the left and right sides of the director
		const managerSpacing = 80; // Space between managers

		// Sort managers by name to ensure consistent placement
		const sortedManagers = directorNode.children;

		// Determine position based on name/type
		let managerX = parentWidth + 50,
			managerY = 50;

		// Place managers
		for (let i = 0; i < sortedManagers.length; i++) {
			const managerNode = sortedManagers[i];
			const managerWidth = managerNode.node.width || 100;
			const managerHeight = managerNode.node.height || 100;

			let top = 0;
			let left = 0;

			if ((i + 1) % 2 == 1) {
				// ODD
				// get the top from col1
				col1.map((x) => (top += x.top));
				managerY = top + managerSpacing;
				// left is always this
				managerX = parentWidth + managerSpacing;
			} else if ((i + 1) % 2 == 0) {
				top = 0;
				// get the top from col2
				col2.map((x) => (top += x.top));
				managerY = top + managerSpacing;
				// get the left
				col1.map((x) => (left = Math.max(left, x.width)));
				managerX = left + managerSpacing + parentWidth + parentX + 20;
			}

			managerNode.node.position = [managerX, managerY];
			await this.area.translate(managerNode.node.id, { x: managerX, y: managerY });
			// await this.area.translate(managerNode.panelRawNode.id, { x: managerX - 20, y: managerY - 20 });

			// If this manager has inspector children, arrange them
			let childDimension = [0, 0];
			if (managerNode.children && managerNode.children.length > 0) {
				childDimension = await this.arrangeManagerChildren(managerNode, managerX, managerY, managerWidth, managerHeight);
			}

			this.area.update('node', managerNode.node.id); // Ensure area is updated after adding output
			this.area.update('node', managerNode.panelRawNode.id); // Ensure area is updated after adding output

			if ((i + 1) % 2 == 1) {
				col1.push({ left: parentWidth + 20, top: childDimension[1] + managerSpacing, width: childDimension[0] + managerWidth });
			} else if ((i + 1) % 2 == 0) {
				col2.push({ left: childDimension[0], top: childDimension[1] + managerSpacing });
			}
		}
	}

	/**
	 * Arrange Manager node's children (Inspectors) based on the manager's position
	 */
	async arrangeManagerChildren(managerNode, parentX, parentY, parentWidth, parentHeight) {
		let inspectorX,
			inspectorY = parentY;
		let childrenTotalHeight = 0,
			childrenTotalWidth = 0;

		for (let i = 0; i < managerNode.children.length; i++) {
			const inspectorNode = managerNode.children[i];
			const inspectorWidth = inspectorNode.node.width || 100;
			const inspectorHeight = inspectorNode.node.height || 100;

			// FRONTEND inspectors go to the right
			inspectorX = parentX + parentWidth + 50;

			// Apply the position
			inspectorNode.node.position = [inspectorX, inspectorY];
			await this.area.translate(inspectorNode.node.id, { x: inspectorX, y: inspectorY });

			// If this inspector has agent children, arrange them
			let childDimension = [0, 0];
			if (inspectorNode.children && inspectorNode.children.length > 0) {
				childDimension = await this.arrangeInspectorChildren(inspectorNode, inspectorX, inspectorY, inspectorWidth, inspectorHeight);
			}

			inspectorY += Math.max(inspectorHeight, childDimension[1]) + 10;
			childrenTotalWidth = Math.max(childDimension[0] + inspectorWidth + parentWidth + 80, childrenTotalWidth);
			childrenTotalHeight += Math.max(inspectorHeight, childDimension[1]) + 10;
		}

		return [childrenTotalWidth, childrenTotalHeight];
	}

	/**
	 * Arrange Inspector node's children (Agents) based on the inspector's position and name
	 */
	async arrangeInspectorChildren(inspectorNode, parentX, parentY, parentWidth, parentHeight) {
		let agentY = parentY;
		let agentX = parentX + parentWidth + 50;
		let childrenTotalHeight = 0,
			childrenTotalWidth = 0;

		for (let i = 0; i < inspectorNode.children.length; i++) {
			const agentNode = inspectorNode.children[i];
			const agentWidth = agentNode.node.width || 100;
			const agentHeight = agentNode.node.height || 100;

			childrenTotalHeight += agentHeight / 2;
			childrenTotalWidth += agentWidth / 2;

			// Apply the position
			agentNode.node.position = [agentX, agentY];
			await this.area.translate(agentNode.node.id, { x: agentX, y: agentY });
			agentY += agentHeight / 2;
			agentX += agentWidth / 2;
		}

		return [childrenTotalWidth, childrenTotalHeight];
	}

	/**
	 * Helper function to connect two nodes
	 * @param {Object} editor - The Rete editor instance
	 * @param {Object} sourceNode - Source node
	 * @param {Object} targetNode - Target node
	 * @param {string} connectionName - Name for logging purposes (optional)
	 */
	async connectNodes(editor, sourceNode, targetNode, connectionName = '', outputIndex = 0, inputIndex = 0) {
		try {
			const outputKey = Object.keys(sourceNode.outputs)[outputIndex];
			const inputKey = Object.keys(targetNode.inputs)[inputIndex];

			if (outputKey && inputKey) {
				const connection = {
					id: `${sourceNode.id}_${outputKey}_${targetNode.id}_${inputKey}`,
					source: sourceNode.id,
					target: targetNode.id,
					sourceOutput: outputKey,
					targetInput: inputKey,
				};
				await editor.addConnection(connection);
				return connection;
			} else {
				throw new Error('Missing output or input keys');
			}
		} catch (connErr) {
			const errorMsg = connectionName ? `Failed to connect ${connectionName}:` : 'Failed to connect nodes:';
			console.warn(errorMsg, connErr);
			throw connErr;
		}
	}

	/**
	 * Add a new node to the editor (this is the main method to use)
	 * @param {string} nodeType - Type of the node
	 * @param {string} name - Name of the node (default: 'Director')
	 * @param {number} inputsCount - Number of inputs (default: 0)
	 * @param {number} outputsCount - Number of outputs (default: 0)
	 * @param {string|null} nodeId - Optional: Use existing node ID from backend (default: null, will generate new ID)
	 * @returns {Object} The created and added node
	 */
	async addNode(nodeType, name = 'Director', inputsCount = 0, outputsCount = 0, nodeId = null) {
		if (!this.editor) {
			console.warn('Editor instance not initialized');
			return null;
		}

		try {
			// Create the node object directly, passing the optional nodeId
			let childNode = new SimpleNode(nodeType, name, null, inputsCount, outputsCount, this, nodeId);

			// Add it to the editor
			await this.editor.addNode(childNode.node);

			// Trigger node-added event via eventBus
			triggerEvent(EVENT_TYPES.NODE_ADDED, childNode);

			// update node position in the editor
			await this.area.translate(childNode.node.id, { x: 10, y: 10 });

			if (nodeType === 'director') {
				this.director = childNode;
			}

			this.nodes.push(childNode);

			return childNode;
		} catch (error) {
			console.error('Failed to add node:', error);
			return null;
		}
	}

	/**
	 * Remove all existing nodes and optionally create a new set of nodes
	 * @param {Array} initialNodes - Array of node configs to create after reset
	 * @returns {Promise<Array>} Array of created nodes
	 */
	async resetNodes(initialNodes = []) {
		if (!this.editor) {
			console.warn('Editor instance not initialized');
			return [];
		}

		const existingNodes = Array.from(this.editor.getNodes ? this.editor.getNodes() : []);

		for (const node of existingNodes) {
			try {
				await this.editor.removeNode(node.id);
			} catch (error) {
				console.warn('Failed to remove node during reset:', error);
			}
		}

		this.nodes = [];
		this.director = null;
		this.groups = [];

		if (this.area) {
			this.area.update();
		}

		const createdNodes = [];

		for (const nodeConfig of initialNodes) {
			const { type, name, inputs = 0, outputs = 0, id = null } = nodeConfig || {};

			if (!type || !name) {
				continue;
			}

			// Pass the backend node ID if available
			const createdNode = await this.addNode(type, name, inputs, outputs, id);

			if (createdNode) {
				createdNodes.push(createdNode);
			}
		}

		return createdNodes;
	}

	/**
	 * Delete a node using the node manager
	 * @param {string} nodeId - ID of the node to delete
	 * @returns {Promise<boolean>} Success status
	 */
	async deleteNode(nodeId) {
		if (!this.editor) {
			console.warn('Editor instance not initialized');
			return false;
		}

		try {
			// Get node info before removing it
			const node = this.editor.getNode(nodeId);
			const nodeName = node ? node.label || 'Unnamed Node' : 'Unknown Node';

			await this.editor.removeNode(nodeId);

			this.nodes = this.nodes.filter((x) => x.id !== nodeId);

			// Trigger node deleted event via eventBus
			triggerEvent(EVENT_TYPES.NODE_DELETED, {
				id: nodeId,
				name: nodeName,
			});

			// Update the area
			if (this.area) {
				this.area.update();
			}

			return true;
		} catch (error) {
			console.error('Failed to remove node:', error);
			return false;
		}
	}

	/**
	 * Get the current editor instance
	 * @returns {Object} The editor instance
	 */
	getEditor() {
		return this.editor;
	}

	/**
	 * Get the current area instance
	 * @returns {Object} The area instance
	 */
	getArea() {
		return this.area;
	}

	/**
	 * Check if the editor is initialized
	 * @returns {boolean} True if initialized
	 */
	isInitialized() {
		return !!(this.editor && this.area);
	}

	/**
	 * Undo the last action using the history plugin
	 * @returns {Promise<boolean>} Success status
	 */
	async undo() {
		if (!this.historyPlugin) {
			console.warn('History plugin not initialized');
			return false;
		}

		try {
			await this.historyPlugin.undo();
			return true;
		} catch (error) {
			console.error('Failed to undo:', error);
			return false;
		}
	}

	/**
	 * Redo the last undone action using the history plugin
	 * @returns {Promise<boolean>} Success status
	 */
	async redo() {
		if (!this.historyPlugin) {
			console.warn('History plugin not initialized');
			return false;
		}

		try {
			await this.historyPlugin.redo();
			return true;
		} catch (error) {
			console.error('Failed to redo:', error);
			return false;
		}
	}

	/**
	 * Check if undo is available
	 * @returns {boolean} True if undo is available
	 */
	canUndo() {
		return this.historyPlugin ? this.historyPlugin.canUndo() : false;
	}

	/**
	 * Check if redo is available
	 * @returns {boolean} True if redo is available
	 */
	canRedo() {
		return this.historyPlugin ? this.historyPlugin.canRedo() : false;
	}

	/**
	 * Set a node's status to 'off' (used primarily for panel nodes)
	 * @param {string} nodeId - The ID of the node to turn off
	 */
	setNodeStatusOff(nodeId) {
		if (!this.editor) {
			console.warn('Editor instance not initialized');
			return;
		}

		try {
			const node = this.editor.getNode(nodeId);
			if (node) {
				node.status = 'off';
				triggerEvent(EVENT_TYPES.NODE_STATUS_CHANGE, {
					nodeId: nodeId,
					status: 'off',
				});
				this.area.update('node', nodeId);
			}
		} catch (error) {
			console.error('Failed to set node status to off:', error);
		}
	}

	/**
	 * Find and hide icons for all panel nodes in the editor
	 */
	hideAllPanelNodeIcons() {
		if (!this.editor) {
			console.warn('Editor instance not initialized');
			return;
		}

		try {
			// Get all nodes from the editor
			const nodes = this.editor.getNodes();

			// Filter for panel nodes based on ID pattern
			nodes.forEach((node) => {
				if (node.id && (node.id.includes('panel_node') || node.id.startsWith('panel_'))) {
					// Set status to 'off' to hide the icon
					node.status = 'off';
					triggerEvent(EVENT_TYPES.NODE_STATUS_CHANGE, {
						nodeId: node.id,
						status: 'off',
					});
					this.area.update('node', node.id);
				}
			});
		} catch (error) {
			console.error('Failed to hide panel node icons:', error);
		}
	}

	/**
	 * Handle node refresh event
	 * @param {Object} payload - Event payload with nodeId and nodeData
	 */
	handleNodeRefresh(payload) {
		if (!this.editor) {
			console.warn('Editor instance not initialized');
			return;
		}

		try {
			const { nodeId, nodeData } = payload;

			// Find the SimpleNode instance that corresponds to the node
			const simpleNode = this.nodes.find((node) => node.node.id === nodeId);

			if (simpleNode) {
				// Set the node status to indicate refresh is happening
				simpleNode.setStatus('thinking');

				// Simulate a refresh operation
				setTimeout(() => {
					// Reset the status after "refresh" completes
					simpleNode.setStatus('completed');

					// After a short delay, return to idle if not a panel node
					setTimeout(() => {
						// Only set back to idle if not a panel node
						if (!(nodeId.includes('panel_node') || nodeId.startsWith('panel_'))) {
							simpleNode.setStatus('idle');
						}
					}, 1000);

					// Update the area to reflect changes
					this.area.update('node', nodeId);
				}, 800);

				// Log the refresh action for debugging
			} else {
				console.warn(`Node ${nodeId} not found in nodes list`);
			}
		} catch (error) {
			console.error('Failed to handle node refresh:', error);
		}
	}

	/**
	 * Search nodes by name/label
	 * @param {string} searchTerm - The search term to match against node names
	 * @returns {Array} Array of matching SimpleNode instances
	 */
	searchNodes(searchTerm) {
		if (!searchTerm || !this.nodes) {
			return [];
		}

		const term = searchTerm.toLowerCase().trim();
		if (term === '') {
			return [];
		}

		return this.nodes.filter((simpleNode) => {
			const nodeName = (simpleNode.name || '').toLowerCase();
			const nodeLabel = (simpleNode.node.label || '').toLowerCase();

			// Search in both name and label
			return nodeName.includes(term) || nodeLabel.includes(term);
		});
	}

	/**
	 * Focus on a specific node by making it visible and selected
	 * @param {Object} payload - Event payload with nodeId
	 */
	handleFocusNode(payload) {
		if (!this.editor || !this.area) {
			console.warn('Editor instance not initialized');
			return;
		}

		try {
			const { nodeId } = payload;
			const node = this.editor.getNode(nodeId);

			if (!node) {
				console.warn(`Node ${nodeId} not found`);
				return;
			}

			// Get current area size and node position
			const nodePos = node.position || [0, 0];
			const nodeWidth = node.width || 100;
			const nodeHeight = node.height || 100;

			// Center the node in the viewport by translating the area
			const centerX = nodePos[0] + nodeWidth / 2;
			const centerY = nodePos[1] + nodeHeight / 2;

			// Get container dimensions
			const containerRect = this.area.container.getBoundingClientRect();
			const viewportCenterX = containerRect.width / 2;
			const viewportCenterY = containerRect.height / 2;

			// Calculate the translation needed to center the node
			const translateX = viewportCenterX - centerX;
			const translateY = viewportCenterY - centerY;

			// Apply the translation using area transform
			const currentTransform = this.area.area.transform;
			const newTransform = {
				...currentTransform,
				x: translateX,
				y: translateY,
			};

			// Update transform
			this.area.area.transform = newTransform;

			// Trigger node selected event
			const simpleNode = this.nodes.find((n) => n.node.id === nodeId);
			if (simpleNode) {
				triggerEvent(EVENT_TYPES.NODE_SELECTED, {
					id: nodeId,
					name: simpleNode.name,
					data: {
						label: simpleNode.node.label,
						inputs: Object.keys(simpleNode.node.inputs || {}),
						outputs: Object.keys(simpleNode.node.outputs || {}),
					},
				});
			}

			// Update the area to reflect the changes
			this.area.update();
		} catch (error) {
			console.error('Failed to focus on node:', error);
		}
	}

	/**
	 * Zoom to fit all nodes in the viewport
	 */
	async zoomFit() {
		if (!this.editor || !this.area) {
			console.warn('Editor instance not initialized');
			return;
		}

		try {
			const nodes = this.editor.getNodes();

			if (nodes.length === 0) {
				console.log('No nodes to fit');
				return;
			}

			// Calculate the bounding box of all nodes
			let minX = Infinity,
				minY = Infinity;
			let maxX = -Infinity,
				maxY = -Infinity;

			nodes.forEach((node) => {
				const nodePos = node.position || [0, 0];
				const nodeWidth = node.width || 100;
				const nodeHeight = node.height || 100;

				const x1 = nodePos[0];
				const y1 = nodePos[1];
				const x2 = x1 + nodeWidth;
				const y2 = y1 + nodeHeight;

				minX = Math.min(minX, x1);
				minY = Math.min(minY, y1);
				maxX = Math.max(maxX, x2);
				maxY = Math.max(maxY, y2);
			});

			// Add some padding
			const padding = 50;
			minX -= padding;
			minY -= padding;
			maxX += padding;
			maxY += padding;

			// Calculate the content size
			const contentWidth = maxX - minX;
			const contentHeight = maxY - minY;

			// Get the container size
			const containerRect = this.area.container.getBoundingClientRect();
			const containerWidth = containerRect.width;
			const containerHeight = containerRect.height;

			// Calculate the scale to fit the content in the container
			const scaleX = containerWidth / contentWidth;
			const scaleY = containerHeight / contentHeight;
			let scale = Math.min(scaleX, scaleY, 1); // Don't zoom in beyond 100%
			if (!Number.isFinite(scale) || scale <= 0) {
				scale = 0.1;
			}

			// Calculate the center position
			const centerX = (minX + maxX) / 2;
			const centerY = (minY + maxY) / 2;

			// Calculate the translation to center the content
			const translateX = containerWidth / 2 - centerX * scale;
			const translateY = containerHeight / 2 - centerY * scale;

			// Apply the transformation
			const transform = {
				x: translateX,
				y: translateY,
				k: scale,
			};

			this.area.area.transform = transform;
			this.area.update();

			console.log('Zoom to fit applied:', transform);
		} catch (error) {
			console.error('Failed to zoom to fit:', error);
		}
	}

	/**
	 * Animate node position from parent's position to target position
	 * @param {string} nodeId - The ID of the node to animate
	 * @param {Object} targetPosition - Target position {x, y}
	 * @param {number} duration - Animation duration in milliseconds (default: 1000)
	 * @returns {Promise} Promise that resolves when animation completes
	 */
	async translatePosition(nodeId, targetPosition, duration = 1000) {
		try {
			// 1. Find the node in editor.nodes with the given nodeId
			const node = this.editor.getNode(nodeId);
			if (!node) {
				console.warn(`Node with ID ${nodeId} not found`);
				return;
			}

			// Remember current target position A
			const targetX = targetPosition.x;
			const targetY = targetPosition.y;

			// 2. Find its parentNode
			const simpleNode = this.nodes.find((n) => n.node.id === nodeId);
			if (!simpleNode || !simpleNode.parentNode) {
				console.warn(`Parent node not found for node ${nodeId}`);
				// If no parent, just animate from current position
				await this.area.translate(nodeId, { x: targetX, y: targetY });
				return;
			}

			// 3. Get position of parentNode (position B)
			const parentNode = simpleNode.parentNode.node;
			const parentPosition = parentNode.position || [0, 0];
			const parentX = parentPosition[0];
			const parentY = parentPosition[1];

			// Get the DOM element for the node using Rete.js area view system
			const nodeView = this.area.nodeViews.get(nodeId);
			if (!nodeView || !nodeView.element) {
				console.warn(`DOM element not found for node ${nodeId}`);
				// Fallback to instant positioning
				node.position = [targetX, targetY];
				await this.area.translate(nodeId, { x: targetX, y: targetY });
				return;
			}

			const nodeElement = nodeView.element;

			// 4. Place node at its parent's position B instantly
			node.position = [parentX, parentY];
			await this.area.translate(nodeId, { x: parentX, y: parentY });

			// 5. Force a reflow to apply the starting position immediately
			nodeElement.offsetHeight; // Force reflow

			// 6. Animate the transform to the target position A using CSS transition
			return new Promise((resolve) => {
				// Set up CSS transition for smooth animation
				nodeElement.style.transition = `transform ${duration}ms linear`;

				// Calculate the transform needed
				const deltaX = targetX - parentX;
				const deltaY = targetY - parentY;

				// Apply the transform animation
				nodeElement.style.transform = `translate(${deltaX}px, ${deltaY}px)`;

				// 7. After animation completes, clear transition and update node.position
				setTimeout(() => {
					// Clear the transition and transform
					nodeElement.style.transition = '';
					nodeElement.style.transform = '';

					// Update the node position to the target coordinates
					node.position = [targetX, targetY];
					this.area.translate(nodeId, { x: targetX, y: targetY });

					resolve();
				}, duration);
			});
		} catch (error) {
			console.error('Failed to animate node position:', error);
		}
	}

	/**
	 * Lock a node to prevent it from being moved or modified
	 * @param {string} nodeId - ID of the node to lock
	 * @returns {Promise<boolean>} Success status
	 */
	async lockNode(nodeId) {
		if (!this.editor) {
			console.warn('Editor instance not initialized');
			return false;
		}

		try {
			const node = this.editor.getNode(nodeId);
			if (!node) {
				console.warn('Node not found:', nodeId);
				return false;
			}

			// Set the locked property on the node
			node.locked = true;

			// Update the area to reflect changes
			if (this.area) {
				this.area.update('node', nodeId);
			}

			console.log('Node locked:', nodeId);
			return true;
		} catch (error) {
			console.error('Failed to lock node:', error);
			return false;
		}
	}

	/**
	 * Unlock a node to allow it to be moved or modified
	 * @param {string} nodeId - ID of the node to unlock
	 * @returns {Promise<boolean>} Success status
	 */
	async unlockNode(nodeId) {
		if (!this.editor) {
			console.warn('Editor instance not initialized');
			return false;
		}

		try {
			const node = this.editor.getNode(nodeId);
			if (!node) {
				console.warn('Node not found:', nodeId);
				return false;
			}

			// Remove the locked property from the node
			node.locked = false;

			// Update the area to reflect changes
			if (this.area) {
				this.area.update('node', nodeId);
			}

			console.log('Node unlocked:', nodeId);
			return true;
		} catch (error) {
			console.error('Failed to unlock node:', error);
			return false;
		}
	}

	/**
	 * Toggle minimap visibility
	 */
	toggleMinimap() {
		if (!this.minimapPlugin || !this.area) {
			console.warn('Minimap plugin or area not initialized');
			return;
		}

		try {
			// Find the minimap element in the DOM
			const minimapElement = this.area.container.querySelector('.minimap');

			if (minimapElement) {
				// Toggle visibility by changing display style
				if (minimapElement.style.display === 'none') {
					minimapElement.style.display = 'block';
				} else {
					minimapElement.style.display = 'none';
				}
			} else {
				console.warn('Minimap element not found in DOM');
			}
		} catch (error) {
			console.error('Failed to toggle minimap:', error);
		}
	}

	/**
	 * Handle node action events from TopBar
	 * @param {string} action - The action to perform (add, delete, lock, unlock, etc.)
	 */
	async handleNodeAction(action) {
		if (!this.editor) {
			console.warn('Editor instance not initialized');
			return;
		}

		try {
			switch (action) {
				case 'add':
					// Add a new node with 1 input and 1 output as specified in the issue
					await this.addNode('director', 'New Node', 1, 1);
					break;
				case 'delete':
					// Trigger DELETE_NODE event for confirmation dialog
					triggerEvent(EVENT_TYPES.DELETE_NODE);
					break;
				case 'lock':
					// TODO: Lock selected nodes - requires node selection tracking implementation
					console.warn('Lock node action not yet implemented - requires selection tracking');
					break;
				case 'unlock':
					// TODO: Unlock selected nodes - requires node selection tracking implementation
					console.warn('Unlock node action not yet implemented - requires selection tracking');
					break;
				default:
					console.warn('Unknown node action:', action);
			}
		} catch (error) {
			console.error('Failed to handle node action:', error);
		}
	}

	/**
	 * Cleanup editor resources
	 */
	cleanup() {
		this.editor = null;
		this.area = null;
		this.historyPlugin = null;
		this.minimapPlugin = null;
	}
}
