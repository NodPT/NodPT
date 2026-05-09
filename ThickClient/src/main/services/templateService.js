'use strict'

const { getDb } = require('../db/database')

function nowIso() {
  return new Date().toISOString()
}

function listTemplates() {
  const db = getDb()
  const rows = db.prepare('SELECT * FROM Templates WHERE IsActive = 1 ORDER BY Name').all()
  return rows.map((row) => ({
    Id: row.Id,
    Name: row.Name,
    Description: row.Description,
    IsActive: !!row.IsActive,
    CreatedAt: row.CreatedAt,
    UpdatedAt: row.UpdatedAt,
  }))
}

function getTemplate(id) {
  const db = getDb()
  const row = db.prepare('SELECT * FROM Templates WHERE Id = ?').get(id)
  if (!row) return null
  const aiModels = db.prepare('SELECT * FROM AIModels WHERE TemplateId = ?').all(id)
  const prompts = db.prepare('SELECT * FROM Prompts WHERE TemplateId = ?').all(id)
  return {
    Id: row.Id,
    Name: row.Name,
    Description: row.Description,
    IsActive: !!row.IsActive,
    CreatedAt: row.CreatedAt,
    UpdatedAt: row.UpdatedAt,
    AIModels: aiModels,
    Prompts: prompts,
  }
}

function ensureDefaultTemplate() {
  const db = getDb()
  const row = db.prepare('SELECT Id FROM Templates LIMIT 1').get()
  if (row) return row.Id
  const ts = nowIso()
  const result = db.prepare(`
    INSERT INTO Templates (Name, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES (?, ?, 1, ?, ?)
  `).run('Default', 'Default local template', ts, ts)
  return result.lastInsertRowid
}

module.exports = {
  listTemplates,
  getTemplate,
  ensureDefaultTemplate,
}
