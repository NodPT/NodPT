'use strict'

const axios = require('axios')

const memoryService = require('./memoryService')
const chatService = require('./chatService')
const nodeService = require('./nodeService')

/**
 * Local replacement for the C# `Executor` project's `ChatStreamAgent`.
 *
 * Workflow (mirrors the original `ChatStreamWorker.cs` documentation):
 *  1. Receive a user chat message (already persisted).
 *  2. Look up node + project + template context.
 *  3. Load memory summary for the node.
 *  4. Compose an Ollama prompt (system prompts + memory + user message).
 *  5. Call Ollama at the configured endpoint.
 *  6. Persist the AI response as a new ChatMessage tied to the same NodeId.
 *  7. Append both user and AI turns to NodeMemory.
 *  8. Return the new AI message to the caller (renderer via IPC).
 *
 * The executor is fully local and does not require Redis, SignalR or a backend.
 * Streaming is supported via the optional `onToken` callback which the IPC layer
 * uses to forward partial tokens back to the renderer over a webContents channel.
 */
class OllamaExecutor {
  constructor(options = {}) {
    this.endpoint = options.endpoint || process.env.OLLAMA_ENDPOINT || 'http://localhost:11434'
    this.defaultModel = options.defaultModel || process.env.OLLAMA_MODEL || 'llama3.2:3b'
  }

  /**
   * Build the prompt to send to Ollama.
   * Uses the node's memory summary as system context and includes the user
   * message as the latest turn.
   */
  buildPayload(node, userMessage, memorySummary) {
    const systemParts = []
    systemParts.push(
      `You are an AI agent of type "${node?.NodeType || 'Agent'}" within a NodPT multi-agent workflow. ` +
      'Be concise, helpful, and stay on task.'
    )
    if (memorySummary) {
      systemParts.push(`Conversation memory so far:\n${memorySummary}`)
    }

    return {
      model: this.defaultModel,
      stream: false,
      messages: [
        { role: 'system', content: systemParts.join('\n\n') },
        { role: 'user', content: userMessage },
      ],
    }
  }

  /**
   * Call Ollama and return the assistant's textual reply.
   */
  async callOllama(payload, { onToken } = {}) {
    if (!onToken) {
      const res = await axios.post(`${this.endpoint}/api/chat`, payload, { timeout: 600000 })
      return res?.data?.message?.content || ''
    }

    // Streaming path (line-delimited JSON from Ollama).
    const streaming = { ...payload, stream: true }
    const res = await axios.post(`${this.endpoint}/api/chat`, streaming, {
      responseType: 'stream',
      timeout: 600000,
    })
    let full = ''
    let buffer = ''
    await new Promise((resolve, reject) => {
      res.data.on('data', (chunk) => {
        buffer += chunk.toString('utf8')
        let nl
        while ((nl = buffer.indexOf('\n')) !== -1) {
          const line = buffer.slice(0, nl).trim()
          buffer = buffer.slice(nl + 1)
          if (!line) continue
          try {
            const obj = JSON.parse(line)
            const token = obj?.message?.content || ''
            if (token) {
              full += token
              try { onToken(token) } catch (_) { /* ignore */ }
            }
          } catch (_) {
            // Ignore malformed lines.
          }
        }
      })
      res.data.on('end', resolve)
      res.data.on('error', reject)
    })
    return full
  }

  /**
   * Process a user message end-to-end.
   *
   * @param {Object} userMessage - The persisted ChatMessage row created by the renderer.
   * @param {Object} options - { onToken } streaming callback.
   * @returns {Promise<Object>} the AI ChatMessage record.
   */
  async processUserMessage(userMessage, options = {}) {
    if (!userMessage || !userMessage.NodeId) {
      throw new Error('OllamaExecutor.processUserMessage requires a message with NodeId')
    }

    const node = nodeService.getNode(userMessage.NodeId)
    const memory = memoryService.getMemory(userMessage.NodeId)
    const payload = this.buildPayload(node, userMessage.Message || '', memory?.Summary || '')

    let aiContent
    try {
      aiContent = await this.callOllama(payload, options)
    } catch (err) {
      const msg = err?.response?.data?.error || err?.message || 'Unknown Ollama error'
      aiContent = `[Ollama error] ${msg}`
    }

    const aiMessage = chatService.createMessage({
      Sender: 'ai',
      Message: aiContent,
      NodeId: userMessage.NodeId,
    })

    // Update rolling memory (append-only; can be replaced with a real summarizer later).
    memoryService.appendToMemory(userMessage.NodeId, `User: ${userMessage.Message}`)
    memoryService.appendToMemory(userMessage.NodeId, `AI: ${aiContent}`)

    return aiMessage
  }
}

module.exports = { OllamaExecutor }
