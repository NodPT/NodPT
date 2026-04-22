'use strict'

const { getDb } = require('../db/database')

function nowIso() {
  return new Date().toISOString()
}

/**
 * Get the current rolling-summary memory for a node, or null if none yet.
 */
function getMemory(nodeId) {
  const db = getDb()
  const row = db.prepare('SELECT * FROM NodeMemories WHERE NodeId = ?').get(nodeId)
  if (!row) return null
  return {
    Id: row.Id,
    NodeId: row.NodeId,
    Summary: row.Summary,
    CreatedAt: row.CreatedAt,
    UpdatedAt: row.UpdatedAt,
  }
}

/**
 * Append `entry` (plain text) onto the node's rolling summary.
 *
 * The original .NET ChatStreamWorker performs LLM-based summarization on each
 * exchange. The ThickClient keeps the same persistence shape but defers smart
 * summarization to the executor (which can call Ollama). This function only
 * appends; pass a pre-summarized string to keep the memory bounded.
 */
function appendToMemory(nodeId, entry) {
  if (!nodeId || !entry) return getMemory(nodeId)
  const db = getDb()
  const ts = nowIso()
  const existing = getMemory(nodeId)
  if (existing) {
    const newSummary = `${existing.Summary || ''}\n${entry}`.trim()
    db.prepare('UPDATE NodeMemories SET Summary = ?, UpdatedAt = ? WHERE NodeId = ?')
      .run(newSummary, ts, nodeId)
  } else {
    db.prepare(`
      INSERT INTO NodeMemories (NodeId, Summary, CreatedAt, UpdatedAt)
      VALUES (?, ?, ?, ?)
    `).run(nodeId, entry, ts, ts)
  }
  return getMemory(nodeId)
}

function setMemory(nodeId, summary) {
  const db = getDb()
  const ts = nowIso()
  const existing = getMemory(nodeId)
  if (existing) {
    db.prepare('UPDATE NodeMemories SET Summary = ?, UpdatedAt = ? WHERE NodeId = ?')
      .run(summary, ts, nodeId)
  } else {
    db.prepare(`
      INSERT INTO NodeMemories (NodeId, Summary, CreatedAt, UpdatedAt)
      VALUES (?, ?, ?, ?)
    `).run(nodeId, summary, ts, ts)
  }
  return getMemory(nodeId)
}

module.exports = {
  getMemory,
  appendToMemory,
  setMemory,
}
