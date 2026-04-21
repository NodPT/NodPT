'use strict'

const { app } = require('electron')
const path = require('node:path')
const fs = require('node:fs')
const Database = require('better-sqlite3')

let db = null

/**
 * Returns the absolute path of the SQLite database file inside the user's app data directory.
 * In tests / non-Electron contexts, falls back to the current working directory.
 */
function getDatabasePath() {
  let dir
  try {
    dir = app.getPath('userData')
  } catch (e) {
    dir = path.join(process.cwd(), '.nodpt-data')
  }
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true })
  }
  return path.join(dir, 'nodpt.sqlite')
}

/**
 * Initialize the SQLite database, creating the schema if it does not exist.
 * Idempotent: safe to call multiple times.
 */
function initDatabase() {
  if (db) return db
  const dbPath = getDatabasePath()
  db = new Database(dbPath)
  db.pragma('journal_mode = WAL')
  db.pragma('foreign_keys = ON')
  applySchema(db)
  return db
}

function getDb() {
  if (!db) return initDatabase()
  return db
}

function closeDatabase() {
  if (db) {
    db.close()
    db = null
  }
}

/**
 * Apply the SQLite schema. Mirrors the C# XPO models:
 *   Project, Template, AIModel, Prompt, Node, ChatMessage, ChatResponse, NodeMemory.
 *
 * Note: User and authentication tables are intentionally omitted - the desktop client
 * is single-user and works locally without login.
 */
function applySchema(database) {
  database.exec(`
    CREATE TABLE IF NOT EXISTS Templates (
      Id INTEGER PRIMARY KEY AUTOINCREMENT,
      Name TEXT NOT NULL,
      Description TEXT,
      IsActive INTEGER NOT NULL DEFAULT 1,
      CreatedAt TEXT NOT NULL,
      UpdatedAt TEXT NOT NULL
    );

    CREATE TABLE IF NOT EXISTS Projects (
      Id INTEGER PRIMARY KEY AUTOINCREMENT,
      Name TEXT NOT NULL,
      Description TEXT,
      IsActive INTEGER NOT NULL DEFAULT 1,
      CreatedAt TEXT NOT NULL,
      UpdatedAt TEXT NOT NULL,
      TemplateId INTEGER,
      FOREIGN KEY (TemplateId) REFERENCES Templates(Id) ON DELETE SET NULL
    );

    CREATE TABLE IF NOT EXISTS AIModels (
      Id INTEGER PRIMARY KEY AUTOINCREMENT,
      Name TEXT NOT NULL,
      ModelIdentifier TEXT NOT NULL,
      MessageType TEXT,
      NodeType TEXT,
      Description TEXT,
      IsActive INTEGER NOT NULL DEFAULT 1,
      EndpointAddress TEXT,
      TemplateId INTEGER,
      CreatedAt TEXT NOT NULL,
      UpdatedAt TEXT NOT NULL,
      FOREIGN KEY (TemplateId) REFERENCES Templates(Id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS Prompts (
      Id INTEGER PRIMARY KEY AUTOINCREMENT,
      Name TEXT,
      Content TEXT,
      MessageType TEXT,
      NodeType TEXT,
      TemplateId INTEGER,
      CreatedAt TEXT NOT NULL,
      UpdatedAt TEXT NOT NULL,
      FOREIGN KEY (TemplateId) REFERENCES Templates(Id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS Nodes (
      Id TEXT PRIMARY KEY,
      Name TEXT,
      NodeType TEXT NOT NULL,
      MessageType TEXT,
      Status TEXT,
      Properties TEXT,
      ParentId TEXT,
      OriginalParentNodeId TEXT,
      ProjectId INTEGER,
      TemplateId INTEGER,
      CreatedAt TEXT NOT NULL,
      UpdatedAt TEXT NOT NULL,
      FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE,
      FOREIGN KEY (TemplateId) REFERENCES Templates(Id) ON DELETE SET NULL,
      FOREIGN KEY (ParentId) REFERENCES Nodes(Id) ON DELETE SET NULL
    );

    CREATE INDEX IF NOT EXISTS idx_nodes_project ON Nodes(ProjectId);
    CREATE INDEX IF NOT EXISTS idx_nodes_parent ON Nodes(ParentId);

    CREATE TABLE IF NOT EXISTS ChatMessages (
      Id INTEGER PRIMARY KEY AUTOINCREMENT,
      Sender TEXT,
      Message TEXT,
      Timestamp TEXT NOT NULL,
      MarkedAsSolution INTEGER NOT NULL DEFAULT 0,
      ConnectionId TEXT,
      NodeId TEXT,
      FOREIGN KEY (NodeId) REFERENCES Nodes(Id) ON DELETE CASCADE
    );

    CREATE INDEX IF NOT EXISTS idx_chatmessages_node ON ChatMessages(NodeId);

    CREATE TABLE IF NOT EXISTS ChatResponses (
      Id INTEGER PRIMARY KEY AUTOINCREMENT,
      ChatMessageId INTEGER NOT NULL,
      Content TEXT,
      Status TEXT,
      CreatedAt TEXT NOT NULL,
      UpdatedAt TEXT NOT NULL,
      FOREIGN KEY (ChatMessageId) REFERENCES ChatMessages(Id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS NodeMemories (
      Id INTEGER PRIMARY KEY AUTOINCREMENT,
      NodeId TEXT NOT NULL UNIQUE,
      Summary TEXT,
      CreatedAt TEXT NOT NULL,
      UpdatedAt TEXT NOT NULL
    );
  `)
}

module.exports = {
  initDatabase,
  getDb,
  closeDatabase,
  getDatabasePath,
}
