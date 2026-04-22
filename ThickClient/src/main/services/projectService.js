'use strict'

const { getDb } = require('../db/database')

function nowIso() {
  return new Date().toISOString()
}

function rowToProject(row) {
  if (!row) return null
  return {
    Id: row.Id,
    Name: row.Name,
    Description: row.Description,
    IsActive: !!row.IsActive,
    CreatedAt: row.CreatedAt,
    UpdatedAt: row.UpdatedAt,
    TemplateId: row.TemplateId,
    TemplateName: row.TemplateName || null,
  }
}

function listProjects() {
  const db = getDb()
  const rows = db.prepare(`
    SELECT p.*, t.Name AS TemplateName
    FROM Projects p
    LEFT JOIN Templates t ON t.Id = p.TemplateId
    WHERE p.IsActive = 1
    ORDER BY p.UpdatedAt DESC
  `).all()
  return rows.map(rowToProject)
}

function getProject(id) {
  const db = getDb()
  const row = db.prepare(`
    SELECT p.*, t.Name AS TemplateName
    FROM Projects p
    LEFT JOIN Templates t ON t.Id = p.TemplateId
    WHERE p.Id = ?
  `).get(id)
  if (!row) return null
  const project = rowToProject(row)
  const nodes = db.prepare('SELECT * FROM Nodes WHERE ProjectId = ?').all(id)
  project.Nodes = nodes.map((n) => ({
    Id: n.Id,
    Name: n.Name,
    NodeType: n.NodeType,
    MessageType: n.MessageType,
    Status: n.Status,
    ParentId: n.ParentId,
    OriginalParentNodeId: n.OriginalParentNodeId,
    ProjectId: n.ProjectId,
    TemplateId: n.TemplateId,
    Properties: n.Properties ? safeJson(n.Properties) : {},
    CreatedAt: n.CreatedAt,
    UpdatedAt: n.UpdatedAt,
  }))
  return project
}

function safeJson(value) {
  try {
    return JSON.parse(value)
  } catch (e) {
    return {}
  }
}

function createProject(input) {
  const db = getDb()
  const ts = nowIso()
  const stmt = db.prepare(`
    INSERT INTO Projects (Name, Description, IsActive, CreatedAt, UpdatedAt, TemplateId)
    VALUES (?, ?, 1, ?, ?, ?)
  `)
  const result = stmt.run(
    input.Name || 'Untitled Project',
    input.Description || null,
    ts,
    ts,
    input.TemplateId || null,
  )
  return getProject(result.lastInsertRowid)
}

function updateProjectName(id, name) {
  const db = getDb()
  db.prepare('UPDATE Projects SET Name = ?, UpdatedAt = ? WHERE Id = ?')
    .run(name, nowIso(), id)
  return getProject(id)
}

function deleteProject(id) {
  const db = getDb()
  // Soft-delete to mirror the behavior of the original WebAPI.
  db.prepare('UPDATE Projects SET IsActive = 0, UpdatedAt = ? WHERE Id = ?')
    .run(nowIso(), id)
  return true
}

module.exports = {
  listProjects,
  getProject,
  createProject,
  updateProjectName,
  deleteProject,
}
