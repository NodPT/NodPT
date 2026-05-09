'use strict'

const { BrowserWindow } = require('electron')

const projectService = require('../services/projectService')
const nodeService = require('../services/nodeService')
const chatService = require('../services/chatService')
const memoryService = require('../services/memoryService')
const templateService = require('../services/templateService')
const { OllamaExecutor } = require('../services/ollamaExecutor')

const executor = new OllamaExecutor()

/**
 * Wraps an async handler so any thrown error is converted into a serializable
 * `{ ok: false, error }` payload. The renderer's IPC bridge unwraps this.
 */
function safe(fn) {
  return async (event, ...args) => {
    try {
      const data = await fn(event, ...args)
      return { ok: true, data }
    } catch (err) {
      return { ok: false, error: err?.message || String(err) }
    }
  }
}

function broadcast(channel, payload) {
  for (const win of BrowserWindow.getAllWindows()) {
    win.webContents.send(channel, payload)
  }
}

/**
 * Register all IPC channels exposed to the renderer.
 *
 * Channels mirror the original WebAPI controllers/SignalR events one-to-one
 * so the renderer's service layer can be ported with minimal change:
 *
 *   projects:list / get / create / update-name / delete
 *   nodes:list / get / create / update / delete
 *   chat:send / list-by-node / mark-solution / retry
 *   memory:get / set / append
 *   templates:list / get / ensure-default
 */
function registerIpcHandlers(ipcMain) {
  // Projects
  ipcMain.handle('projects:list', safe(() => projectService.listProjects()))
  ipcMain.handle('projects:get', safe((_e, id) => projectService.getProject(id)))
  ipcMain.handle('projects:create', safe((_e, input) => projectService.createProject(input)))
  ipcMain.handle('projects:update-name', safe((_e, id, name) => projectService.updateProjectName(id, name)))
  ipcMain.handle('projects:delete', safe((_e, id) => projectService.deleteProject(id)))

  // Nodes
  ipcMain.handle('nodes:list', safe((_e, projectId) => nodeService.listNodes(projectId)))
  ipcMain.handle('nodes:get', safe((_e, id) => nodeService.getNode(id)))
  ipcMain.handle('nodes:create', safe((_e, input) => nodeService.createNode(input)))
  ipcMain.handle('nodes:update', safe((_e, id, patch) => nodeService.updateNode(id, patch)))
  ipcMain.handle('nodes:delete', safe((_e, id) => nodeService.deleteNode(id)))

  // Chat
  ipcMain.handle('chat:list-by-node', safe((_e, nodeId) => chatService.listMessagesByNode(nodeId)))
  ipcMain.handle('chat:mark-solution', safe((_e, messageId) => chatService.markAsSolution(messageId)))

  // Send a user message and trigger the local executor.
  // Streams partial AI tokens back to the renderer on `chat:stream`.
  ipcMain.handle('chat:send', safe(async (_event, input) => {
    const userMessage = chatService.createMessage({
      Sender: 'user',
      Message: input?.Message || '',
      NodeId: input?.NodeId,
      MarkedAsSolution: false,
      ConnectionId: input?.ConnectionId || null,
    })

    // Run the executor in the background so the IPC call returns quickly,
    // mirroring the WebAPI behavior of enqueueing work and answering 202.
    setImmediate(() => {
      executor
        .processUserMessage(userMessage, {
          onToken: (token) => broadcast('chat:stream', {
            NodeId: userMessage.NodeId,
            UserMessageId: userMessage.Id,
            Token: token,
          }),
        })
        .then((aiMessage) => broadcast('chat:complete', {
          NodeId: userMessage.NodeId,
          UserMessageId: userMessage.Id,
          AiMessage: aiMessage,
        }))
        .catch((err) => broadcast('chat:error', {
          NodeId: userMessage.NodeId,
          UserMessageId: userMessage.Id,
          Error: err?.message || String(err),
        }))
    })

    return userMessage
  }))

  ipcMain.handle('chat:retry', safe(async (_e, messageId) => {
    const original = chatService.getMessage(messageId)
    if (!original) throw new Error(`Message ${messageId} not found`)
    return executor.processUserMessage(original)
  }))

  // Memory
  ipcMain.handle('memory:get', safe((_e, nodeId) => memoryService.getMemory(nodeId)))
  ipcMain.handle('memory:set', safe((_e, nodeId, summary) => memoryService.setMemory(nodeId, summary)))
  ipcMain.handle('memory:append', safe((_e, nodeId, entry) => memoryService.appendToMemory(nodeId, entry)))

  // Templates
  ipcMain.handle('templates:list', safe(() => templateService.listTemplates()))
  ipcMain.handle('templates:get', safe((_e, id) => templateService.getTemplate(id)))
  ipcMain.handle('templates:ensure-default', safe(() => templateService.ensureDefaultTemplate()))
}

module.exports = { registerIpcHandlers }
