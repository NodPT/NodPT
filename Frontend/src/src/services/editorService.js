import "litegraph.js/css/litegraph.css"
import { LiteGraph, LGraph, LGraphCanvas } from "litegraph.js"
import { arrangeNodes as arrangeNodesPlugin, createDemoNodes } from "../plugins/nodeArrangePlugin.js"
import { bus, EVENT_TYPES } from '../plugins/bus.js'

/**
 * Emit an event if the bus and event types are initialized.
 * @param {string} eventType - Event name from EVENT_TYPES to trigger.
 * @param {Object} payload - Event payload.
 * @returns {void} Does nothing when bus/event types are not set.
 */
const emitEvent = (eventType, payload) => {
  if (!bus || !EVENT_TYPES) {
    return
  }
  bus.trigger(eventType, payload)
}

let activeGraphState = null
let allowConnections = true
let suppressNodeRemoved = false
let lastSelectedNodeId = null
let expandCanvasTimeout = null

const MIN_CANVAS_WIDTH = 1920
const MIN_CANVAS_HEIGHT = 1080

const NODE_TYPES = {
  DIRECTOR: "Director",
  MANAGER: "Manager",
  SUPERVISOR: "Supervisor",
  AGENT: "Agent"
}

const TWILIGHT_PALETTE = {
  [NODE_TYPES.DIRECTOR]: {
    color: "#3b3d3f",
    bgcolor: "#2B2D30",
    boxcolor: "#E3E5E8"
  },
  [NODE_TYPES.MANAGER]: {
    color: "#3b3d3f",
    bgcolor: "#3b4047",
    boxcolor: "#D0D3D8"
  },
  [NODE_TYPES.SUPERVISOR]: {
    color: "#3b3d3f",
    bgcolor: "#58616d",
    boxcolor: "#C2C6CC"
  },
  [NODE_TYPES.AGENT]: {
    color: "#3b3d3f",
    bgcolor: "#6a8198",
    boxcolor: "#B6BBC2"
  }
}

const getGraphState = () => activeGraphState

const getLinksMap = (graph) => graph?.links || graph?._links || {}

const ensureSupervisorSubgraph = (supervisorNode) => {
  if (!supervisorNode?.subgraph) {
    const subgraph = new LGraph()
    subgraph._subgraph_node = supervisorNode
    supervisorNode.subgraph = subgraph
  }
  return supervisorNode.subgraph
}

const getSupervisorAgents = (graph, supervisorNode) => {
  const links = getLinksMap(graph)
  const agentIds = new Set()

  Object.values(links).forEach((link) => {
    if (link?.origin_id === supervisorNode.id && link?.target_id != null) {
      agentIds.add(link.target_id)
    }
  })

  return (graph._nodes || []).filter((node) =>
    node &&
    agentIds.has(node.id) &&
    node.properties?.nodeType === NODE_TYPES.AGENT
  )
}

// move the Agent nodes to supervisor node
const moveAgentsIntoSupervisorSubgraph = (supervisorNode, agentNodes = null) => {
  const state = getGraphState()
  if (!state || !state.graph || !supervisorNode) {
    return
  }
  if (supervisorNode.properties?.nodeType !== NODE_TYPES.SUPERVISOR) {
    return
  }

  const graph = state.graph
  const subgraph = ensureSupervisorSubgraph(supervisorNode)
  const nodesToMove = agentNodes && agentNodes.length
    ? agentNodes
    : getSupervisorAgents(graph, supervisorNode)

  if (!nodesToMove.length) {
    return
  }

  let supervisorProxy = (subgraph._nodes || []).find((node) => node?.properties?._isSupervisorProxy)
  if (!supervisorProxy) {
    ensureAgentNodeRegistered()
    supervisorProxy = LiteGraph.createNode("nodpt/agent")
    supervisorProxy.title = supervisorNode.title || NODE_TYPES.SUPERVISOR
    supervisorProxy.properties = {
      ...(supervisorProxy.properties || {}),
      nodeType: NODE_TYPES.SUPERVISOR,
      _isSupervisorProxy: true
    }
    supervisorProxy.inputs = []
    supervisorProxy.outputs = []
    supervisorProxy.horizontal = false
    supervisorProxy.pos = [20, 20]
    supervisorProxy.widgets = supervisorProxy.widgets || []
    supervisorProxy.widgets.length = 0
    supervisorProxy.addWidget?.('button', 'X', '', () => {
      state.graphCanvas?.closeSubgraph?.()
    })
    subgraph.add(supervisorProxy)
  }

  suppressNodeRemoved = true
  const sortedAgents = nodesToMove.slice().sort((a, b) => {
    const titleA = a?.title || ''
    const titleB = b?.title || ''
    return titleA.localeCompare(titleB)
  })

  const startX = 240
  const startY = 40
  const verticalGap = 30

  sortedAgents.forEach((agentNode, index) => {
    if (!agentNode || agentNode.graph !== graph) {
      return
    }
    graph.remove(agentNode)
    const agentHeight = agentNode.size?.[1] ?? agentNode.height ?? 80
    agentNode.pos = [240, 40 + index * (agentHeight + verticalGap)]
    subgraph.add(agentNode)
  })
  subgraph.arrange(30)
  suppressNodeRemoved = false

  supervisorProxy.outputs = []
  sortedAgents.forEach((agentNode, index) => {
    const baseLabel = agentNode.title || agentNode.id || 'Agent'
    const outputLabel = index === 0 ? baseLabel : `${baseLabel} ${index + 1}`
    supervisorProxy.addOutput(outputLabel, 0, { label: outputLabel })
  })
  const slotCount = Math.max(supervisorProxy.inputs?.length || 0, supervisorProxy.outputs?.length || 0)
  const minHeight = LiteGraph.NODE_TITLE_HEIGHT + slotCount * LiteGraph.NODE_SLOT_HEIGHT + 8
  if (typeof supervisorProxy.computeSize === 'function') {
    supervisorProxy.size = supervisorProxy.computeSize()
  }
  supervisorProxy.size = supervisorProxy.size || [160, 80]
  supervisorProxy.size[1] = Math.max(supervisorProxy.size[1], minHeight)

  const prevAllowConnections = allowConnections
  allowConnections = true
  sortedAgents.forEach((agentNode, index) => {
    supervisorProxy.connect(index, agentNode, 0)
  })
  allowConnections = prevAllowConnections

  graph.setDirtyCanvas?.(true, true)
  state.graphCanvas?.setDirty(true, true)
}

const ensureAgentNodeRegistered = () => {
  if (LiteGraph.registered_node_types?.["nodpt/agent"]) {
    return
  }

  function AgentNode() {
    this.title = "Agent"
  }

  AgentNode.title = "Agent"
  AgentNode.category = "nodpt"

  LiteGraph.registerNodeType("nodpt/agent", AgentNode)
}

// add a new node to the graph
export const AddNode = (id, title, nodeType, outputs = [], connectFrom = null, autoArrange = false) => {
  const state = getGraphState()
  if (!state || !state.graph) {
    return null
  }

  const graph = state.graph
  ensureAgentNodeRegistered()
  const node = LiteGraph.createNode("nodpt/agent")

  node.id = id ?? node.id
  node.title = title || nodeType || ""
  node.properties = {
    ...(node.properties || {}),
    nodeType: nodeType || NODE_TYPES.AGENT
  }

  const palette = TWILIGHT_PALETTE[node.properties.nodeType] || TWILIGHT_PALETTE[NODE_TYPES.AGENT]
  node.color = palette.color
  node.bgcolor = palette.bgcolor
  node.boxcolor = palette.boxcolor

  node.inputs = []
  node.outputs = []
  node.horizontal = false
  node.addInput("input")
  outputs.forEach((outputName) => {
    const label = outputName// ? outputName.trim().charAt(0).toUpperCase() : ""
    node.addOutput(outputName, 0, { label })
  })

  if (node.widgets && node.widgets.length) {
    node.widgets.length = 0
  }
  node.serialize_widgets = false

  const index = graph._nodes ? graph._nodes.length : 0
  node.pos = [200 + index * 220, 180 + (index % 2) * 180]

  graph.add(node)

  // connect the new node if connectFrom is provided
  if (connectFrom && connectFrom.nodeId) {
    // find the source node
    const sourceNode = graph.getNodeById(connectFrom.nodeId)
    if (sourceNode) {
      let outputIndex = 0
      if (typeof connectFrom.outputIndex === "number") {
        outputIndex = connectFrom.outputIndex
      } else if (connectFrom.outputName) {
        outputIndex = sourceNode.findOutputSlot(connectFrom.outputName)
        if (outputIndex < 0) {
          outputIndex = 0
        }
      }

      const prevAllowConnections = allowConnections
      allowConnections = true
      sourceNode.connect(outputIndex, node, 0)
      allowConnections = prevAllowConnections

      // special case: if connecting a AGENT to an SUPERVISOR, 
      // move the agent into the supervisor's subgraph
      if (node.properties?.nodeType === NODE_TYPES.AGENT
        && sourceNode.properties?.nodeType === NODE_TYPES.SUPERVISOR) {
        moveAgentsIntoSupervisorSubgraph(sourceNode, [node])
      }
    }
  }

  // if autoArrange is true, arrange all nodes after adding
  if (autoArrange) {
    arrangeNodes();
  }

  // emit event for node added
  emitEvent(EVENT_TYPES.NODE_ADDED, {
    id: node.id,
    title: node.title,
    nodeType: node.properties.nodeType
  })

  // return the created node
  return node
}

// remove a node by ID
export const RemoveNode = (id) => {
  const state = getGraphState()
  if (!state || !state.graph) {
    return false
  }

  const graph = state.graph
  const node = graph.getNodeById(id)
  if (!node) {
    return false
  }

  if (node.properties?.nodeType === NODE_TYPES.DIRECTOR) {
    return false
  }

  graph.remove(node)
  // emit event for node deleted
  emitEvent(EVENT_TYPES.NODE_DELETED, { id: node.id, title: node.title, nodeType: node.properties?.nodeType })
  return true
}

// clear all nodes from the graph except DIRECTOR nodes
export const Clear = () => {
  const state = getGraphState()
  if (!state || !state.graph) {
    return 0
  }

  const graph = state.graph
  const nodes = (graph._nodes || []).slice()
  let removed = 0

  nodes.forEach((node) => {
    if (node.properties?.nodeType === NODE_TYPES.DIRECTOR) {
      return
    }
    graph.remove(node)
    // emit event for node deleted
    emitEvent(EVENT_TYPES.NODE_DELETED, { id: node.id, title: node.title, nodeType: node.properties?.nodeType })
    removed += 1
  })

  return removed
}

/**
 * Expand the canvas dimensions if any node's rendered position exceeds current canvas size.
 * Uses a debounce to avoid excessive resizes during dragging.
 */
const expandCanvasForNodes = () => {
  const state = getGraphState()
  if (!state || !state.graph || !state.graphCanvas) return

  const nodes = state.graph._nodes || []
  if (!nodes.length) return

  const { canvas, ds } = state.graphCanvas
  const scale = (ds && ds.scale) || 1
  const offset = (ds && ds.offset) || [0, 0]
  const padding = 200

  let newWidth = canvas.width
  let newHeight = canvas.height

  nodes.forEach((node) => {
    const pos = node.pos || [0, 0]
    const size = node.size || [200, 100]
    const canvasX = (pos[0] + size[0]) * scale + offset[0] + padding
    const canvasY = (pos[1] + size[1]) * scale + offset[1] + padding
    if (canvasX > newWidth) newWidth = Math.ceil(canvasX)
    if (canvasY > newHeight) newHeight = Math.ceil(canvasY)
  })

  if (newWidth > canvas.width || newHeight > canvas.height) {
    canvas.width = newWidth
    canvas.height = newHeight
    state.graphCanvas.resize()
  }
}

// Debounced wrapper for expandCanvasForNodes (used during node dragging)
const scheduleExpandCanvas = () => {
  clearTimeout(expandCanvasTimeout)
  expandCanvasTimeout = setTimeout(expandCanvasForNodes, 200)
}

// export arrangeNodes function
export const arrangeNodes = (margin = 50) => {
  const result = arrangeNodesPlugin(getGraphState(), NODE_TYPES, margin)
  expandCanvasForNodes()
  return result
}

// export zoomFit function
export const zoomFit = (padding = 80, maxScale = 1) => {
  const state = getGraphState()
  if (!state || !state.graph || !state.graphCanvas) {
    return false
  }

  const graphCanvas = state.graphCanvas
  const nodes = state.graph._nodes || []
  if (!nodes.length) {
    return false
  }

  let minX = Infinity
  let minY = Infinity
  let maxX = -Infinity
  let maxY = -Infinity

  nodes.forEach((node) => {
    const bounds = node.getBounding?.(null, true) || [node.pos[0], node.pos[1], node.size?.[0] || 0, node.size?.[1] || 0]
    const [x, y, w, h] = bounds
    minX = Math.min(minX, x)
    minY = Math.min(minY, y)
    maxX = Math.max(maxX, x + w)
    maxY = Math.max(maxY, y + h)
  })

  const width = Math.max(1, maxX - minX)
  const height = Math.max(1, maxY - minY)
  const canvasWidth = Math.max(1, graphCanvas.canvas.width)
  const canvasHeight = Math.max(1, graphCanvas.canvas.height)

  const scaleX = canvasWidth / (width + padding * 2)
  const scaleY = canvasHeight / (height + padding * 2)
  const scale = Math.min(scaleX, scaleY, maxScale)

  graphCanvas.ds.scale = scale
  graphCanvas.ds.offset[0] = canvasWidth * 0.5 - (minX + width * 0.5) * scale
  graphCanvas.ds.offset[1] = canvasHeight * 0.5 - (minY + height * 0.5) * scale
  graphCanvas.setDirty(true, true)
  return true
}

// initialize and return the graph instance
export const initGraph = (canvas, container, options = {}) => {
  if (!canvas || !container) {
    return null
  }

  const graph = new LGraph() // create the graph instance
  // Enable LiteGraph's built-in autoresize so it calls graphCanvas.resize() on every mouse-move event.
  // We override the instance resize() below to enforce the minimum canvas size.
  const graphCanvas = new LGraphCanvas(canvas, graph, { autoresize: true })

  // Override LiteGraph's built-in resize() on this instance to enforce minimum canvas dimensions.
  // When called without arguments, LiteGraph fills the canvas to its parentNode size; we apply our
  // minimum (1920×1080) so the canvas is always at least that large even on small viewports.
  const _lgResize = graphCanvas.resize.bind(graphCanvas)
  graphCanvas.resize = (width, height) => {
    if (width === undefined && height === undefined) {
      const parent = canvas.parentNode
      width = Math.max(MIN_CANVAS_WIDTH, parent ? parent.offsetWidth : 0)
      height = Math.max(MIN_CANVAS_HEIGHT, parent ? parent.offsetHeight : 0)
    }
    _lgResize(width, height)
  }

  // Also handle window resize events (autoresize only fires on canvas mousemove)
  const onWindowResize = () => graphCanvas.resize()
  window.addEventListener("resize", onWindowResize)
  graphCanvas.resize() // initial size

  // graphCanvas.read_only = true;
  // graphCanvas.allow_interaction = false;
  graphCanvas.allow_searchbox = false
  graphCanvas.allow_reconnect_links = false
  graphCanvas.processContextMenu = () => { } // disable default context menu
  graphCanvas.showLinkMenu = () => false // disable link context menu
  graphCanvas.drawSubgraphPanel = () => { } // disable subgraph panel
  canvas.addEventListener("contextmenu", (event) => event.preventDefault()) // disable right-click context menu

  // disable certain link-breaking behaviors
  LiteGraph.shift_click_do_break_link_from = false
  LiteGraph.click_do_break_link_to = false

  // override isValidConnection to respect allowConnections flag
  if (!LiteGraph.__originalIsValidConnection) {
    LiteGraph.__originalIsValidConnection = LiteGraph.isValidConnection
  }

  // new isValidConnection that checks allowConnections flag
  LiteGraph.isValidConnection = (...args) => {
    if (!allowConnections) { // check if connections are allowed
      return false // disallow connection
    }
    // call the original isValidConnection method
    return LiteGraph.__originalIsValidConnection(...args)
  }

  // set up graph event handlers
  graphCanvas.onNodeSelected = (node) => {
    const nextId = node?.id ?? null
    if (nextId === lastSelectedNodeId) {
      return
    }
    lastSelectedNodeId = nextId
    emitEvent(EVENT_TYPES.NODE_SELECTED, node)
    console.log(`selected node`, lastSelectedNodeId)
  }
  // when node is deselected, emit with null
  graphCanvas.onNodeDeselected = (node) => {
    if (lastSelectedNodeId === null || lastSelectedNodeId == node?.id) {
      return
    }
    lastSelectedNodeId = null
    emitEvent(EVENT_TYPES.NODE_SELECTED, null)
  }
  // handle node removal
  graph.onNodeRemoved = (node) => {
    if (suppressNodeRemoved) {
      return
    }
    if (node?.properties?.nodeType === NODE_TYPES.DIRECTOR) {
      return
    }
    // emit event for node deleted
    emitEvent(EVENT_TYPES.NODE_DELETED, { id: node.id, title: node.title, nodeType: node.properties?.nodeType })
  }

  // auto-expand canvas when a node is moved outside the current canvas bounds
  graphCanvas.onNodeMoved = () => {
    scheduleExpandCanvas()
  }

  // Set up the graph state
  activeGraphState = { graph, graphCanvas, onWindowResize }

  // Only create demo nodes if explicitly requested
  if (options.createDemo) {
    allowConnections = true
    createDemoNodes(AddNode, NODE_TYPES, arrangeNodes)
    allowConnections = false;
    (graph._nodes || []).forEach((node) => {
      if (node.properties?.nodeType === NODE_TYPES.SUPERVISOR) {
        moveAgentsIntoSupervisorSubgraph(node)
      }
    })
  }

  graph.start()

  return activeGraphState
}

// destroy the graph instance and clean up
export const destroyGraph = (state) => {
  if (!state) {
    return
  }

  clearTimeout(expandCanvasTimeout)
  expandCanvasTimeout = null
  window.removeEventListener("resize", state.onWindowResize)

  if (state.graph) {
    state.graph.stop()
    state.graph.clear()
  }

  state.graphCanvas = null
  state.graph = null

  if (activeGraphState === state) {
    activeGraphState = null
  }
}

/**
 * Load nodes from project data into the editor
 * @param {Array} nodes - Array of node DTOs from the API
 */
export const loadProjectNodes = (nodes) => {
  if (!nodes || !Array.isArray(nodes) || nodes.length === 0) {
    console.debug('loadProjectNodes: No nodes to load or invalid nodes parameter')
    return
  }

  const state = getGraphState()
  if (!state || !state.graph) {
    console.warn('loadProjectNodes: Cannot load nodes - graph not initialized')
    return
  }

  // Clear existing nodes first
  Clear()

  // Build a map of nodes by ID for quick lookup
  const nodeMap = new Map()
  nodes.forEach(node => {
    nodeMap.set(node.Id, node)
  })

  // Create a map to store created graph nodes
  const createdNodes = new Map()

  // Helper function to get output names for a node based on its children
  const getOutputNames = (nodeId) => {
    const children = nodes.filter(n => n.ParentId === nodeId)
    return children.map(child => child.Name || child.NodeType || 'Output')
  }

  // Helper function to recursively create nodes
  const ensureNodeCreated = (nodeDto) => {
    // Skip if already created
    if (createdNodes.has(nodeDto.Id)) {
      return createdNodes.get(nodeDto.Id)
    }

    const outputs = getOutputNames(nodeDto.Id)

    // Determine connection info if node has a parent
    let connectFrom = null
    if (nodeDto.ParentId) {
      const parentNode = createdNodes.get(nodeDto.ParentId)
      if (!parentNode) {
        // Create parent first
        const parentDto = nodeMap.get(nodeDto.ParentId)
        if (parentDto) {
          ensureNodeCreated(parentDto)
        }
      }

      // Now try to get parent node again
      const parentGraphNode = createdNodes.get(nodeDto.ParentId)
      if (parentGraphNode) {
        connectFrom = {
          nodeId: parentGraphNode.id,
          outputName: nodeDto.Name || nodeDto.NodeType
        }
      }
    }

    // Enable connections temporarily to create the node with connections
    allowConnections = true

    // Map NodeType enum to expected string values
    const nodeType = nodeDto.NodeType || NODE_TYPES.AGENT
    if (!nodeDto.NodeType) {
      console.debug('Node missing NodeType, defaulting to AGENT:', nodeDto.Id)
    }

    // Create the node
    const graphNode = AddNode(
      nodeDto.Id,
      nodeDto.Name || nodeType,
      nodeType,
      outputs,
      connectFrom
    )

    allowConnections = false

    if (graphNode) {
      createdNodes.set(nodeDto.Id, graphNode)
    }

    return graphNode
  }

  // First, find root nodes (nodes without parents)
  const rootNodes = nodes.filter(n => !n.ParentId)

  // Create root nodes first
  rootNodes.forEach(nodeDto => {
    ensureNodeCreated(nodeDto)
  })

  // Then create all other nodes (which will recursively create their parents if needed)
  nodes.forEach(nodeDto => {
    ensureNodeCreated(nodeDto)
  })

  // Move agents into supervisor subgraphs
  allowConnections = true
    ; (state.graph._nodes || []).forEach((node) => {
      if (node.properties?.nodeType === NODE_TYPES.SUPERVISOR) {
        moveAgentsIntoSupervisorSubgraph(node)
      }
    })
  allowConnections = false

  // Arrange the nodes
  arrangeNodes()

  // Zoom to fit after layout completes (using requestAnimationFrame for reliability)
  requestAnimationFrame(() => {
    requestAnimationFrame(() => {
      zoomFit()
    })
  })
}
