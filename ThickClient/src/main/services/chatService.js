'use strict'

const { getDb } = require('../db/database')

function nowIso() {
  return new Date().toISOString()
}

function rowToMessage(row) {
  if (!row) return null
  return {
    Id: row.Id,
    Sender: row.Sender,
    Message: row.Message,
    Timestamp: row.Timestamp,
    MarkedAsSolution: !!row.MarkedAsSolution,
    ConnectionId: row.ConnectionId,
    NodeId: row.NodeId,
  }
}

function getMessage(id) {
  const db = getDb()
  return rowToMessage(db.prepare('SELECT * FROM ChatMessages WHERE Id = ?').get(id))
}

function listMessagesByNode(nodeId) {
  const db = getDb()
  const rows = db.prepare('SELECT * FROM ChatMessages WHERE NodeId = ? ORDER BY Timestamp ASC')
    .all(nodeId)
  return rows.map(rowToMessage)
}

function createMessage(input) {
  const db = getDb()
  const ts = input.Timestamp || nowIso()
  const result = db.prepare(`
    INSERT INTO ChatMessages
      (Sender, Message, Timestamp, MarkedAsSolution, ConnectionId, NodeId)
    VALUES (?, ?, ?, ?, ?, ?)
  `).run(
    input.Sender || 'user',
    input.Message || '',
    ts,
    input.MarkedAsSolution ? 1 : 0,
    input.ConnectionId || null,
    input.NodeId || null,
  )
  return getMessage(result.lastInsertRowid)
}

function markAsSolution(messageId) {
  const db = getDb()
  db.prepare('UPDATE ChatMessages SET MarkedAsSolution = 1 WHERE Id = ?').run(messageId)
  return getMessage(messageId)
}

module.exports = {
  getMessage,
  listMessagesByNode,
  createMessage,
  markAsSolution,
}
