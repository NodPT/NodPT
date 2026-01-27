import "litegraph.js/css/litegraph.css"
import { LiteGraph, LGraph, LGraphCanvas } from "litegraph.js"
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
  node.title = title || nodeType || "Node"
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

const getNodeSize = (node) => {
  const width = node?.size?.[0] ?? node?.width ?? 120
  const height = node?.size?.[1] ?? node?.height ?? 80
  return [width, height]
}

const setNodePosition = (node, x, y) => {
  node.pos = [x, y]
}

const buildHierarchy = (graph) => {
  const nodes = graph._nodes || []
  const nodeMap = new Map()

  nodes.forEach((node) => {
    nodeMap.set(node.id, {
      node,
      nodeType: node.properties?.nodeType || NODE_TYPES.WORKER,
      children: []
    })
  })

  const links = graph.links || graph._links || {}
  Object.values(links).forEach((link) => {
    const sourceInfo = nodeMap.get(link.origin_id)
    const targetInfo = nodeMap.get(link.target_id)
    if (!sourceInfo || !targetInfo) return
    if (!sourceInfo.children.includes(targetInfo)) {
      sourceInfo.children.push(targetInfo)
    }
  })

  return Array.from(nodeMap.values())
}

const sortByTitle = (a, b) => {
  const titleA = a?.node?.title || ''
  const titleB = b?.node?.title || ''
  return titleA.localeCompare(titleB)
}

const arrangeInspectorChildren = (inspectorInfo, parentX, parentY, parentWidth, parentHeight, spacing) => {
  let agentY = parentY
  let agentX = parentX + parentWidth + spacing
  let childrenTotalHeight = 0
  let childrenTotalWidth = 0

  const children = (inspectorInfo.children || []).slice().sort(sortByTitle)
  children.forEach((agentInfo) => {
    const [agentWidth, agentHeight] = getNodeSize(agentInfo.node)
    childrenTotalHeight += agentHeight / 2
    childrenTotalWidth += agentWidth / 2

    setNodePosition(agentInfo.node, agentX, agentY)
    agentY += agentHeight / 2
    agentX += agentWidth / 2
  })

  return [childrenTotalWidth, childrenTotalHeight]
}

const arrangeManagerChildren = (managerInfo, parentX, parentY, parentWidth, parentHeight, spacing) => {
  let inspectorX = parentX + parentWidth + spacing
  let inspectorY = parentY
  let childrenTotalHeight = 0
  let childrenTotalWidth = 0

  const children = (managerInfo.children || []).slice().sort(sortByTitle)
  children.forEach((inspectorInfo) => {
    const [inspectorWidth, inspectorHeight] = getNodeSize(inspectorInfo.node)

    setNodePosition(inspectorInfo.node, inspectorX, inspectorY)

    let childDimension = [0, 0]
    if (inspectorInfo.children && inspectorInfo.children.length > 0) {
      childDimension = arrangeInspectorChildren(inspectorInfo, inspectorX, inspectorY, inspectorWidth, inspectorHeight, spacing)
    }

    inspectorY += Math.max(inspectorHeight, childDimension[1]) + 10
    childrenTotalWidth = Math.max(childDimension[0] + inspectorWidth + parentWidth + spacing, childrenTotalWidth)
    childrenTotalHeight += Math.max(inspectorHeight, childDimension[1]) + 10
  })

  return [childrenTotalWidth, childrenTotalHeight]
}

const arrangeDirectorChildren = (directorInfo, parentX, parentY, parentWidth, parentHeight, spacing) => {
  const col1 = []
  const col2 = []
  const managerSpacing = Math.max(80, spacing)

  const sortedManagers = (directorInfo.children || []).slice().sort(sortByTitle)

  let managerX = parentWidth + managerSpacing
  let managerY = parentY

  for (let i = 0; i < sortedManagers.length; i += 1) {
    const managerInfo = sortedManagers[i]
    const [managerWidth, managerHeight] = getNodeSize(managerInfo.node)

    let top = 0
    let left = 0

    if ((i + 1) % 2 === 1) {
      col1.forEach((x) => {
        top += x.top
      })
      managerY = top + managerSpacing
      managerX = parentWidth + managerSpacing + parentX
    } else {
      col2.forEach((x) => {
        top += x.top
      })
      managerY = top + managerSpacing
      col1.forEach((x) => {
        left = Math.max(left, x.width)
      })
      managerX = left + managerSpacing + parentWidth + parentX + 20
    }

    setNodePosition(managerInfo.node, managerX, managerY)

    let childDimension = [0, 0]
    if (managerInfo.children && managerInfo.children.length > 0) {
      childDimension = arrangeManagerChildren(managerInfo, managerX, managerY, managerWidth, managerHeight, spacing)
    }

    if ((i + 1) % 2 === 1) {
      col1.push({ left: parentWidth + 20, top: childDimension[1] + managerSpacing, width: childDimension[0] + managerWidth })
    } else {
      col2.push({ left: childDimension[0], top: childDimension[1] + managerSpacing })
    }
  }
}

export const arrangeNodes = (layout = 'vertical', margin = 50) => {
  const state = getGraphState()
  if (!state || !state.graph) {
    return false
  }

  const graph = state.graph
  const nodeInfos = buildHierarchy(graph)
  if (!nodeInfos.length) {
    return false
  }

  const directorInfo = nodeInfos.find((info) => info.nodeType === NODE_TYPES.DIRECTOR)
  if (!directorInfo) {
    return false
  }

  const [directorWidth, directorHeight] = getNodeSize(directorInfo.node)
  const startX = 60
  const startY = 60
  setNodePosition(directorInfo.node, startX, startY)

  if (directorInfo.children && directorInfo.children.length > 0) {
    arrangeDirectorChildren(directorInfo, startX, startY, directorWidth, directorHeight, margin)
  }

  graph.setDirtyCanvas?.(true, true)
  state.graphCanvas?.setDirty(true, true)
  return true
}

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

const createDemoNodes = () => {
  AddNode("director-1", "Director", NODE_TYPES.DIRECTOR, ["Plan", "Decide"])

  const managers = [
    { id: "manager-1", output: "Plan" },
    { id: "manager-2", output: "Decide" }
  ]

  managers.forEach((manager, managerIndex) => {
    const inspectorOutputs = ["Inspector 1", "Inspector 2", "Inspector 3"]
    AddNode(manager.id, manager.id === "manager-1" ? "Manager 1" : "Manager 2", NODE_TYPES.MANAGER, inspectorOutputs, {
      nodeId: "director-1",
      outputName: manager.output
    })

    inspectorOutputs.forEach((inspectorOutput, inspectorIndex) => {
      const inspectorId = `inspector-${managerIndex + 1}-${inspectorIndex + 1}`
      const workerOutputs = [
        "Worker 1",
        "Worker 2",
        "Worker 3",
        "Worker 4",
        "Worker 5"
      ]

      AddNode(inspectorId, inspectorOutput, NODE_TYPES.INSPECTOR, workerOutputs, {
        nodeId: manager.id,
        outputName: inspectorOutput
      })

      workerOutputs.forEach((workerOutput, workerIndex) => {
        const workerId = `worker-${managerIndex + 1}-${inspectorIndex + 1}-${workerIndex + 1}`
        AddNode(workerId, workerOutput, NODE_TYPES.WORKER, [], {
          nodeId: inspectorId,
          outputName: workerOutput
        })
      })
    })
  })
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

  graph.onNodeRemoved = (node) => {
    if (node?.properties?.nodeType === NODE_TYPES.DIRECTOR) {
      return
    }
    emitEvent(EVENT_TYPES.NODE_DELETED, { id: node.id, title: node.title, nodeType: node.properties?.nodeType })
  }

  // create some demo nodes
  activeGraphState = { graph, graphCanvas, resize }
  allowConnections = true
  createDemoNodes()
  allowConnections = false

  if (options.autoArrange) {
    const layout = options.layout === "vertical" ? LiteGraph.VERTICAL_LAYOUT : undefined
    graph.arrange(50, layout)
  }
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
