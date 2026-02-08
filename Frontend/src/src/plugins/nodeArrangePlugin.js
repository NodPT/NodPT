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
      nodeType: node.properties?.nodeType || nodeTypes.AGENT,
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

const arrangeSupervisorChildren = (supervisorInfo, parentX, parentY, parentWidth, parentHeight, spacing, marginBottom) => {
  let agentY = parentY
  let agentX = parentX + parentWidth + spacing
  let childrenTotalHeight = 0
  let childrenTotalWidth = 0

  const children = (supervisorInfo.children || []).slice().sort(sortByTitle)
  children.forEach((agentInfo) => {
    const [agentWidth, agentHeight] = getNodeSize(agentInfo.node)
    childrenTotalHeight += agentHeight / 2
    childrenTotalWidth += agentWidth / 2

    setNodePosition(agentInfo.node, agentX, agentY)
    agentY += agentHeight / 2 + marginBottom
    agentX += agentWidth / 2
  })

  return [childrenTotalWidth, childrenTotalHeight]
}

const arrangeManagerChildren = (managerInfo, parentX, parentY, parentWidth, parentHeight, spacing, marginBottom) => {
  let supervisorX = parentX + parentWidth + spacing
  let supervisorY = parentY
  let childrenTotalHeight = 0
  let childrenTotalWidth = 0

  const children = (managerInfo.children || []).slice().sort(sortByTitle)
  children.forEach((supervisorInfo) => {
    const [supervisorWidth, supervisorHeight] = getNodeSize(supervisorInfo.node)

    setNodePosition(supervisorInfo.node, supervisorX, supervisorY)

    let childDimension = [0, 0]
    if (supervisorInfo.children && supervisorInfo.children.length > 0) {
      childDimension = arrangeSupervisorChildren(supervisorInfo, supervisorX, supervisorY, supervisorWidth, supervisorHeight, spacing, marginBottom)
    }

    supervisorY += Math.max(supervisorHeight, childDimension[1]) + marginBottom
    childrenTotalWidth = Math.max(childDimension[0] + supervisorWidth + parentWidth + spacing, childrenTotalWidth)
    childrenTotalHeight += Math.max(supervisorHeight, childDimension[1]) + marginBottom
  })

  return [childrenTotalWidth, childrenTotalHeight]
}

const arrangeDirectorChildren = (directorInfo, parentX, parentY, parentWidth, parentHeight, spacing, marginBottom) => {
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
      childDimension = arrangeManagerChildren(managerInfo, managerX, managerY, managerWidth, managerHeight, spacing, marginBottom)
    }

    if ((i + 1) % 2 === 1) {
      col1.push({ left: parentWidth + 20, top: childDimension[1] + managerSpacing, width: childDimension[0] + managerWidth })
    } else {
      col2.push({ left: childDimension[0], top: childDimension[1] + managerSpacing })
    }
  }
}

export const arrangeNodes = (state, nodeTypes, margin = 50, marginBottom = 30) => {
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
    arrangeDirectorChildren(directorInfo, startX, startY, directorWidth, directorHeight, margin, marginBottom)
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
    const supervisorOutputs = ['Supervisor 1', 'Supervisor 2', 'Supervisor 3']
    addNode(manager.id, manager.id === 'manager-1' ? 'Manager 1' : 'Manager 2', nodeTypes.MANAGER, supervisorOutputs, {
      nodeId: 'director-1',
      outputName: manager.output
    })

    supervisorOutputs.forEach((supervisorOutput, supervisorIndex) => {
      const supervisorId = `supervisor-${managerIndex + 1}-${supervisorIndex + 1}`
      const agentOutputs = [
        'Agent 1',
        'Agent 2',
        'Agent 3',
        'Agent 4',
        'Agent 5'
      ]

      addNode(supervisorId, supervisorOutput, nodeTypes.SUPERVISOR, agentOutputs, {
        nodeId: manager.id,
        outputName: supervisorOutput
      })

      agentOutputs.forEach((agentOutput, agentIndex) => {
        const agentId = `agent-${managerIndex + 1}-${supervisorIndex + 1}-${agentIndex + 1}`
        addNode(agentId, agentOutput, nodeTypes.AGENT, [], {
          nodeId: supervisorId,
          outputName: agentOutput
        })
      })
    })
  })

  if (typeof arrangeFn === 'function') {
    arrangeFn(50)
  }
}
