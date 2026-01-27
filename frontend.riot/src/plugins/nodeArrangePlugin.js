const getNodeSize = (node) => {
  const width = node?.size?.[0] ?? node?.width ?? 120
  const height = node?.size?.[1] ?? node?.height ?? 80
  return [width, height]
}

const setNodePosition = (node, x, y) => {
  node.pos = [x, y]
}

const buildHierarchy = (graph, nodeTypes) => {
  const nodes = graph._nodes || []
  const nodeMap = new Map()

  nodes.forEach((node) => {
    nodeMap.set(node.id, {
      node,
      nodeType: node.properties?.nodeType || nodeTypes.WORKER,
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

export const arrangeNodes = (state, nodeTypes, margin = 50) => {
  if (!state || !state.graph) {
    return false
  }

  const graph = state.graph
  const nodeInfos = buildHierarchy(graph, nodeTypes)
  if (!nodeInfos.length) {
    return false
  }

  const directorInfo = nodeInfos.find((info) => info.nodeType === nodeTypes.DIRECTOR)
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

export const createDemoNodes = (addNode, nodeTypes, arrangeFn) => {
  if (typeof addNode !== 'function') {
    return
  }

  addNode('director-1', 'Director', nodeTypes.DIRECTOR, ['Plan', 'Decide'])

  const managers = [
    { id: 'manager-1', output: 'Plan' },
    { id: 'manager-2', output: 'Decide' }
  ]

  managers.forEach((manager, managerIndex) => {
    const inspectorOutputs = ['Inspector 1', 'Inspector 2', 'Inspector 3']
    addNode(manager.id, manager.id === 'manager-1' ? 'Manager 1' : 'Manager 2', nodeTypes.MANAGER, inspectorOutputs, {
      nodeId: 'director-1',
      outputName: manager.output
    })

    inspectorOutputs.forEach((inspectorOutput, inspectorIndex) => {
      const inspectorId = `inspector-${managerIndex + 1}-${inspectorIndex + 1}`
      const workerOutputs = [
        'Worker 1',
        'Worker 2',
        'Worker 3',
        'Worker 4',
        'Worker 5'
      ]

      addNode(inspectorId, inspectorOutput, nodeTypes.INSPECTOR, workerOutputs, {
        nodeId: manager.id,
        outputName: inspectorOutput
      })

      workerOutputs.forEach((workerOutput, workerIndex) => {
        const workerId = `worker-${managerIndex + 1}-${inspectorIndex + 1}-${workerIndex + 1}`
        addNode(workerId, workerOutput, nodeTypes.WORKER, [], {
          nodeId: inspectorId,
          outputName: workerOutput
        })
      })
    })
  })

  if (typeof arrangeFn === 'function') {
    arrangeFn(50)
  }
}
