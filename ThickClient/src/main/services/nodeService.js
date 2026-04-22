'use strict'

const { v4: uuidv4 } = require('uuid')
const { getDb } = require('../db/database')

function nowIso() {
  return new Date().toISOString()
}

function rowToNode(row) {
  if (!row) return null
  return {
    Id: row.Id,
    Name: row.Name,
    NodeType: row.NodeType,
    MessageType: row.MessageType,
    Status: row.Status,
    Properties: row.Properties ? safeJson(row.Properties) : {},
    ParentId: row.ParentId,
    OriginalParentNodeId: row.OriginalParentNodeId,
    ProjectId: row.ProjectId,
    TemplateId: row.TemplateId,
    CreatedAt: row.CreatedAt,
    UpdatedAt: row.UpdatedAt,
  }
}

function safeJson(value) {
  try {
    return JSON.parse(value)
  } catch (e) {
    return {}
  }
}

function getNode(id) {
  const db = getDb()
  return rowToNode(db.prepare('SELECT * FROM Nodes WHERE Id = ?').get(id))
}

function listNodes(projectId) {
  const db = getDb()
  const rows = db.prepare('SELECT * FROM Nodes WHERE ProjectId = ?').all(projectId)
  return rows.map(rowToNode)
}

function createNode(input) {
  const db = getDb()
  const ts = nowIso()
  const id = input.Id || uuidv4()
  db.prepare(`
    INSERT INTO Nodes
      (Id, Name, NodeType, MessageType, Status, Properties, ParentId, OriginalParentNodeId, ProjectId, TemplateId, CreatedAt, UpdatedAt)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
  `).run(
    id,
    input.Name || null,
    input.NodeType || 'Agent',
    input.MessageType || 'Discussion',
    input.Status || 'Active',
    input.Properties ? JSON.stringify(input.Properties) : null,
    input.ParentId || null,
    input.OriginalParentNodeId || null,
    input.ProjectId || null,
    input.TemplateId || null,
    ts,
    ts,
  )
  return getNode(id)
}

function updateNode(id, patch) {
  const db = getDb()
  const existing = getNode(id)
  if (!existing) return null
  const merged = { ...existing, ...patch }
  db.prepare(`
    UPDATE Nodes SET
      Name = ?,
      NodeType = ?,
      MessageType = ?,
      Status = ?,
      Properties = ?,
      ParentId = ?,
      OriginalParentNodeId = ?,
      UpdatedAt = ?
    WHERE Id = ?
  `).run(
    merged.Name,
    merged.NodeType,
    merged.MessageType,
    merged.Status,
    merged.Properties ? JSON.stringify(merged.Properties) : null,
    merged.ParentId,
    merged.OriginalParentNodeId,
    nowIso(),
    id,
  )
  return getNode(id)
}

function deleteNode(id) {
  const db = getDb()
  db.prepare('DELETE FROM Nodes WHERE Id = ?').run(id)
  return true
}

module.exports = {
  getNode,
  listNodes,
  createNode,
  updateNode,
  deleteNode,
}
