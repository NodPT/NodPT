import { ClassicPreset } from 'rete';
import { triggerEvent, EVENT_TYPES } from './eventBus.js';
/**
 * Simple node factory class that follows Rete.js 2.0.6 API
 */
export class SimpleNode {
    constructor(nodeType, label, rootNode, inputsCount = 1, outputsCount = 1, editorManager, nodeId = null) {
        this.name = label;
        this.inputsCount = inputsCount;
        this.outputsCount = outputsCount;
        this.editorManager = editorManager;
        this.nodeType = nodeType; // Type of the node (director, manager, inspector, agent)
        this.panelRawNode = rootNode;
        this.nodeId = nodeId; // Optional: Use existing node ID from backend

        // Set tier-specific dimensions based on node name
        const dimensions = this._getNodeDimensions(nodeType);
        this.width = dimensions.width;
        this.height = dimensions.height;

        this.director = null;
        this.children = []; // Array to hold child nodes
        this.group = []; // Group this node belongs to
        this.inputs = [];
        this.outputs = [];

        // create the node object
        this.node = this.createNode();
    }

    /**
     * Determine node dimensions based on node tier/type
     * @param {string} name - Node name to analyze
     * @returns {Object} Object with width and height properties
     */
    _getNodeDimensions(nodeType) {
        // Default dimensions for any other node types
        return { width: 100, height: 100 };
    }

    /**
     * Create a node object (internal use - used by NodeManager)
     * @param {Object} editor - The Rete editor instance (for compatibility)
     * @returns {Object} The created node object
     */
    createNode() {

        // Create a proper ClassicPreset node
        const node = new ClassicPreset.Node(this.name);
        
        // Use backend node ID if provided, otherwise generate a new one
        if (this.nodeId) {
            node.id = this.nodeId;
        } else {
            node.id = `node_${(this.panelRawNode ? this.panelRawNode.id : 'director')}_${this.name}_${Date.now()}_${Math.floor(Math.random() * 10000)}`;
        }
        
        node.label = this.name.toUpperCase(); // Set the node label
        node.nodeType = this.nodeType; // Store the node type in the node data for styling

        // Create a shared socket type
        const socket = new ClassicPreset.Socket('default');

        // Add inputs with the socket
        for (let i = 0; i < this.inputsCount; i++) {
            const inputName = i === 0 ? 'input' : `input${i + 1}`;
            node.addInput(inputName, new ClassicPreset.Input(socket, inputName, false));
        }

        // Add outputs with the socket
        for (let i = 0; i < this.outputsCount; i++) {
            const outputName = i === 0 ? 'output' : `output${i + 1}`;
            node.addOutput(outputName, new ClassicPreset.Output(socket, outputName, true));
        }

        // Position it randomly within a reasonable area
        let width = this.width || 100; // Default width if not set,
        let height = this.height || 100; // Default height if not set

        node.position = [10, 10];
        // Set required dimensions for arrange plugin
        node.width = width;
        node.height = height;


        return node;
    }

    async addChild(nodeType, name, outputName = '', inputName = 'in') {
        try {

            // Create the node object directly
            let childNode = new SimpleNode(nodeType, name, this.panelRawNode, 0, 0, this.editorManager);

            // Add it to the editor
            await this.editorManager.editor.addNode(childNode.node);

            // Trigger node-added event via eventBus
            triggerEvent(EVENT_TYPES.NODE_ADDED, childNode);

            // update node position in the editor
            await this.editorManager.area.translate(childNode.node.id, { x: childNode.node.position[0], y: childNode.node.position[1] });

            this.editorManager.nodes.push(childNode); // add to node list
            this.children.push(childNode);

            this.node.height = (this.children.length + 2) * 25;

            if (!outputName) {
                outputName = `${childNode.name}`;
            }

            if (!inputName) {
                inputName = `_${this.children.length}`;
            }

            const socket = new ClassicPreset.Socket('default');

            const connectNode = () => {
                // now add a input socket for childNode
                const ip = new ClassicPreset.Input(socket, inputName, false);
                childNode.node.addInput(inputName, ip);
                this.editorManager.area.update('node', childNode.node.id); // Ensure area is updated after adding input
                childNode.inputs.push(ip);
                childNode.node.height = (childNode.inputs.length + 2) * 20;

                // add a new output socket for parentNode
                this.node.addOutput(outputName, new ClassicPreset.Output(socket, outputName, false));
                this.editorManager.area.update('node', this.node.id); // Ensure area is updated after adding output

                // connect the node
                this.editorManager.connectNodes(this.editorManager.editor,
                    this.node, childNode.node,
                    `${childNode.name}_child_of_${this.name}`,
                    this.children.length - 1, 0);
            };


            if (nodeType === NODE_TYPES.DIRECTOR) {
                // no need to group
                childNode.panelRawNode = null;
            }
            else if (nodeType === NODE_TYPES.MANAGER) {
                // create a group panel to group all the child in it
                const panelNode = new ClassicPreset.Node(`panel_node_of${childNode.name}`);
                panelNode.id = `panel_node_${childNode.node.id}`;
                panelNode.label = (childNode.name + ' manager').toUpperCase(); // Set the node label
                panelNode.status = 'off'; // Set panel node status to 'off' to hide the icon
                panelNode.nodeType = 'panel'; // Explicitly set the panel node type
                await this.editorManager.editor.addNode(panelNode);
                this.editorManager.area.update('node', panelNode.id); // Ensure area is updated after adding output

                // Emit event to update panel node status to 'off'
                triggerEvent(EVENT_TYPES.NODE_STATUS_CHANGE, {
                    nodeId: panelNode.id,
                    status: 'off'
                });

                childNode.panelRawNode = panelNode;
                childNode.node.parent = panelNode.id; // Set the parent node as root for manager nodes
                connectNode();

            }
            else if (nodeType === NODE_TYPES.INSPECTOR) {
                childNode.panelRawNode = this.panelRawNode; // Set the parent node as root for inspector nodes
                childNode.node.parent = this.panelRawNode.id;
                connectNode();
            }
            else if (nodeType === NODE_TYPES.AGENT) {
                // Agents are leaf nodes, no further grouping
                childNode.panelRawNode = this.panelRawNode; // Set the parent node as root for agent nodes
                childNode.node.parent = this.panelRawNode.id;

                connectNode();
            }

            childNode.parentNode = this;
            return childNode;

        } catch (error) {
            console.error('Failed to add node:', error);
            return null;
        }
    }

    setParent(parentNode) {
        this.panelRawNode = parentNode;
        if (parentNode) {
            parentNode.addChild(this);
        }
    }

    /**
     * Set the status of a node
     * @param {string} status - The status to set ('idle', 'thinking', 'working', 'completed', 'error', 'off')
     */
    setStatus(status) {
        // Store the status in the node data
        this.node.status = status;

        // Emit an event to update the visual component
        triggerEvent(EVENT_TYPES.NODE_STATUS_CHANGE, {
            nodeId: this.node.id,
            status: status
        });

        // Update the area to reflect changes
        if (this.editorManager && this.editorManager.area) {
            this.editorManager.area.update('node', this.node.id);
        }
    }


}

const NODE_TYPES = {
    DIRECTOR: 'director',
    MANAGER: 'manager',
    INSPECTOR: 'inspector',
    AGENT: 'agent',
    PANEL: 'panel'
};

export { NODE_TYPES };