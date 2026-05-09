'use strict'

const { contextBridge, ipcRenderer } = require('electron')

/**
 * Invoke a main-process IPC handler and unwrap the standard
 * `{ ok, data, error }` envelope returned by `safe()` in `registerIpcHandlers`.
 */
async function call(channel, ...args) {
  const res = await ipcRenderer.invoke(channel, ...args)
  if (res && res.ok === false) {
    throw new Error(res.error || `IPC call ${channel} failed`)
  }
  return res ? res.data : undefined
}

const projects = {
  list: () => call('projects:list'),
  get: (id) => call('projects:get', id),
  create: (input) => call('projects:create', input),
  updateName: (id, name) => call('projects:update-name', id, name),
  delete: (id) => call('projects:delete', id),
}

const nodes = {
  list: (projectId) => call('nodes:list', projectId),
  get: (id) => call('nodes:get', id),
  create: (input) => call('nodes:create', input),
  update: (id, patch) => call('nodes:update', id, patch),
  delete: (id) => call('nodes:delete', id),
}

const chat = {
  listByNode: (nodeId) => call('chat:list-by-node', nodeId),
  send: (input) => call('chat:send', input),
  markSolution: (id) => call('chat:mark-solution', id),
  retry: (id) => call('chat:retry', id),
  onStream: (cb) => {
    const handler = (_e, payload) => cb(payload)
    ipcRenderer.on('chat:stream', handler)
    return () => ipcRenderer.removeListener('chat:stream', handler)
  },
  onComplete: (cb) => {
    const handler = (_e, payload) => cb(payload)
    ipcRenderer.on('chat:complete', handler)
    return () => ipcRenderer.removeListener('chat:complete', handler)
  },
  onError: (cb) => {
    const handler = (_e, payload) => cb(payload)
    ipcRenderer.on('chat:error', handler)
    return () => ipcRenderer.removeListener('chat:error', handler)
  },
}

const memory = {
  get: (nodeId) => call('memory:get', nodeId),
  set: (nodeId, summary) => call('memory:set', nodeId, summary),
  append: (nodeId, entry) => call('memory:append', nodeId, entry),
}

const templates = {
  list: () => call('templates:list'),
  get: (id) => call('templates:get', id),
  ensureDefault: () => call('templates:ensure-default'),
}

contextBridge.exposeInMainWorld('nodpt', {
  isElectron: true,
  projects,
  nodes,
  chat,
  memory,
  templates,
})
