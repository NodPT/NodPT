import "litegraph.js/css/litegraph.css"
import { LiteGraph, LGraph, LGraphCanvas } from "litegraph.js"
import { arrangeNodes as arrangeNodesPlugin, createDemoNodes } from "../plugins/nodeArrangePlugin.js"
let bus
let EVENT_TYPES

export const setBus = (busInstance, eventTypes) => {
  if (!busInstance || typeof busInstance.trigger !== 'function' || !eventTypes || typeof eventTypes !== 'object') {
    return false
  }
  if (!eventTypes.NODE_ADDED || !eventTypes.NODE_DELETED) {
    return false
  }
  bus = busInstance
  EVENT_TYPES = eventTypes
  return true
}

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

const NODE_TYPES = {
  DIRECTOR: "Director",
  MANAGER: "Manager",
  INSPECTOR: "Inspector",
  WORKER: "Worker"
}

const TWILIGHT_PALETTE = {
  [NODE_TYPES.DIRECTOR]: {
    color: "#3b3d3f",
    bgcolor: "#2B2D30",
    boxcolor: "#E3E5E8"
  },
  [NODE_TYPES.MANAGER]: {
    color: "#3b3d3f",
    bgcolor: "#23262A",
    boxcolor: "#D0D3D8"
  },
  [NODE_TYPES.INSPECTOR]: {
    color: "#3b3d3f",
    bgcolor: "#1D2024",
    boxcolor: "#C2C6CC"
  },
  [NODE_TYPES.WORKER]: {
    color: "#3b3d3f",
    bgcolor: "#171A1D",
    boxcolor: "#B6BBC2"
  }
}

const getGraphState = () => activeGraphState

const getLinksMap = (graph) => graph?.links || graph?._links || {}

const ensureInspectorSubgraph = (inspectorNode) => {
  if (!inspectorNode?.subgraph) {
    const subgraph = new LGraph()
    subgraph._subgraph_node = inspectorNode
    inspectorNode.subgraph = subgraph
  }
  return inspectorNode.subgraph
}

const getInspectorWorkers = (graph, inspectorNode) => {
  const links = getLinksMap(graph)
  const workerIds = new Set()

  Object.values(links).forEach((link) => {
    if (link?.origin_id === inspectorNode.id && link?.target_id != null) {
      workerIds.add(link.target_id)
    }
  })

  return (graph._nodes || []).filter((node) =>
    node &&
    workerIds.has(node.id) &&
    node.properties?.nodeType === NODE_TYPES.WORKER
  )
}

const moveWorkersIntoInspectorSubgraph = (inspectorNode, workerNodes = null) => {
  const state = getGraphState()
  if (!state || !state.graph || !inspectorNode) {
    return
  }
  if (inspectorNode.properties?.nodeType !== NODE_TYPES.INSPECTOR) {
    return
  }

  const graph = state.graph
  const subgraph = ensureInspectorSubgraph(inspectorNode)
  const nodesToMove = workerNodes && workerNodes.length
    ? workerNodes
    : getInspectorWorkers(graph, inspectorNode)

  if (!nodesToMove.length) {
    return
  }

  let inspectorProxy = (subgraph._nodes || []).find((node) => node?.properties?._isInspectorProxy)
  if (!inspectorProxy) {
    ensureAgentNodeRegistered()
    inspectorProxy = LiteGraph.createNode("nodpt/agent")
    inspectorProxy.title = inspectorNode.title || NODE_TYPES.INSPECTOR
    inspectorProxy.properties = {
      ...(inspectorProxy.properties || {}),
      nodeType: NODE_TYPES.INSPECTOR,
      _isInspectorProxy: true
    }
    inspectorProxy.inputs = []
    inspectorProxy.outputs = []
    inspectorProxy.horizontal = false
    inspectorProxy.pos = [20, 20]
    inspectorProxy.widgets = inspectorProxy.widgets || []
    inspectorProxy.widgets.length = 0
    inspectorProxy.addWidget?.('button', 'X', '', () => {
      state.graphCanvas?.closeSubgraph?.()
    })
    subgraph.add(inspectorProxy)
  }

  suppressNodeRemoved = true
  const sortedWorkers = nodesToMove.slice().sort((a, b) => {
    const titleA = a?.title || ''
    const titleB = b?.title || ''
    return titleA.localeCompare(titleB)
  })

  const startX = 240
  const startY = 40
  const verticalGap = 30

  sortedWorkers.forEach((workerNode, index) => {
    if (!workerNode || workerNode.graph !== graph) {
      return
    }
    graph.remove(workerNode)
    const workerHeight = workerNode.size?.[1] ?? workerNode.height ?? 80
      workerNode.pos = [240, 40 + index * (workerHeight + verticalGap)]
    subgraph.add(workerNode)
  })
  subgraph.arrange(30)
  suppressNodeRemoved = false

  inspectorProxy.outputs = []
  sortedWorkers.forEach((workerNode, index) => {
    const baseLabel = workerNode.title || workerNode.id || 'Worker'
    const outputLabel = index === 0 ? baseLabel : `${baseLabel} ${index + 1}`
    inspectorProxy.addOutput(outputLabel, 0, { label: outputLabel })
  })
  const slotCount = Math.max(inspectorProxy.inputs?.length || 0, inspectorProxy.outputs?.length || 0)
  const minHeight = LiteGraph.NODE_TITLE_HEIGHT + slotCount * LiteGraph.NODE_SLOT_HEIGHT + 8
  if (typeof inspectorProxy.computeSize === 'function') {
    inspectorProxy.size = inspectorProxy.computeSize()
  }
  inspectorProxy.size = inspectorProxy.size || [160, 80]
  inspectorProxy.size[1] = Math.max(inspectorProxy.size[1], minHeight)

  const prevAllowConnections = allowConnections
  allowConnections = true
  sortedWorkers.forEach((workerNode, index) => {
    inspectorProxy.connect(index, workerNode, 0)
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

export const AddNode = (id, title, nodeType, outputs = [], connectFrom = null) => {
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
    nodeType: nodeType || NODE_TYPES.WORKER
  }

  const palette = TWILIGHT_PALETTE[node.properties.nodeType] || TWILIGHT_PALETTE[NODE_TYPES.WORKER]
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

  if (connectFrom && connectFrom.nodeId) {
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

      if (node.properties?.nodeType === NODE_TYPES.WORKER && sourceNode.properties?.nodeType === NODE_TYPES.INSPECTOR) {
        moveWorkersIntoInspectorSubgraph(sourceNode, [node])
      }
    }
  }

  emitEvent(EVENT_TYPES.NODE_ADDED, { id: node.id, title: node.title, nodeType: node.properties.nodeType })
  return node
}

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
  emitEvent(EVENT_TYPES.NODE_DELETED, { id: node.id, title: node.title, nodeType: node.properties?.nodeType })
  return true
}

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
    emitEvent(EVENT_TYPES.NODE_DELETED, { id: node.id, title: node.title, nodeType: node.properties?.nodeType })
    removed += 1
  })

  return removed
}

export const arrangeNodes = (margin = 50) => arrangeNodesPlugin(getGraphState(), NODE_TYPES, margin)

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

 const emitSelection = (node) => {
    if (!node) {
      emitEvent(EVENT_TYPES.NODE_SELECTED, null)
      return
    }
    emitEvent(EVENT_TYPES.NODE_SELECTED, node)
  }


// initialize and return the graph instance
export const initGraph = (canvas, container, options = {}) => {
  if (!canvas || !container) {
    return null
  }

  const graph = new LGraph()
  const graphCanvas = new LGraphCanvas(canvas, graph)
  
  const resize = () => {
    const rect = container.getBoundingClientRect()
    canvas.width = Math.max(1, Math.floor(rect.width))
    canvas.height = Math.max(1, Math.floor(rect.height))
    graphCanvas.resize()
  }

  window.addEventListener("resize", resize)
  resize()

  // graphCanvas.read_only = true;
  // graphCanvas.allow_interaction = false;
  graphCanvas.allow_searchbox = false
  graphCanvas.allow_reconnect_links = false
  graphCanvas.processContextMenu = () => {}
  graphCanvas.showLinkMenu = () => false
  graphCanvas.drawSubgraphPanel = () => {}
  canvas.addEventListener("contextmenu", (event) => event.preventDefault())

  LiteGraph.shift_click_do_break_link_from = false
  LiteGraph.click_do_break_link_to = false

  if (!LiteGraph.__originalIsValidConnection) {
    LiteGraph.__originalIsValidConnection = LiteGraph.isValidConnection
  }

  LiteGraph.isValidConnection = (...args) => {
    if (!allowConnections) {
      return false
    }
    return LiteGraph.__originalIsValidConnection(...args)
  }

  graphCanvas.onNodeSelected = (node) => emitSelection(node)
  graphCanvas.onNodeDeselected = () => emitSelection(null)

  graph.onNodeRemoved = (node) => {
    if (suppressNodeRemoved) {
      return
    }
    if (node?.properties?.nodeType === NODE_TYPES.DIRECTOR) {
      return
    }
    emitEvent(EVENT_TYPES.NODE_DELETED, { id: node.id, title: node.title, nodeType: node.properties?.nodeType })
  }

  // create some demo nodes
  activeGraphState = { graph, graphCanvas, resize }
  allowConnections = true
  createDemoNodes(AddNode, NODE_TYPES, arrangeNodes)
  allowConnections = false
  ;(graph._nodes || []).forEach((node) => {
    if (node.properties?.nodeType === NODE_TYPES.INSPECTOR) {
      moveWorkersIntoInspectorSubgraph(node)
    }
  })
  graph.start()

  return activeGraphState
}

export const destroyGraph = (state) => {
  if (!state) {
    return
  }

  window.removeEventListener("resize", state.resize)

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
