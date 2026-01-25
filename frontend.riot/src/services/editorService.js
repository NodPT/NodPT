import "litegraph.js/css/litegraph.css"
import { LiteGraph, LGraph, LGraphCanvas } from "litegraph.js"
let bus = null
let EVENT_TYPES = null

export const setBus = (busInstance, eventTypes) => {
  bus = busInstance
  EVENT_TYPES = eventTypes
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
  // node.horizontal = true
  node.addInput("input")
  outputs.forEach((outputName) => {
    node.addOutput(outputName)
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

  if (bus && EVENT_TYPES) {
    bus.trigger(EVENT_TYPES.NODE_ADDED, { id: node.id, title: node.title, nodeType: node.properties.nodeType })
  }
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
  if (bus && EVENT_TYPES) {
    bus.trigger(EVENT_TYPES.NODE_DELETED, { id: node.id, title: node.title, nodeType: node.properties?.nodeType })
  }
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
    if (bus && EVENT_TYPES) {
      bus.trigger(EVENT_TYPES.NODE_DELETED, { id: node.id, title: node.title, nodeType: node.properties?.nodeType })
    }
    removed += 1
  })

  return removed
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
    if (bus && EVENT_TYPES) {
      bus.trigger(EVENT_TYPES.NODE_DELETED, { id: node.id, title: node.title, nodeType: node.properties?.nodeType })
    }
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
