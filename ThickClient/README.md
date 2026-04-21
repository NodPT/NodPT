# NodPT ThickClient

NodPT ThickClient is the **local-first desktop client** for NodPT, built with
[Electron](https://www.electronjs.org/). It bundles the Riot.js editor that
used to live in `Frontend/` together with a Node.js backend that replaces the
C# `WebAPI`, `Executor` and `Data` projects.

Everything runs locally:

* SQLite (via `better-sqlite3`) replaces MySQL.
* An in-process Ollama executor replaces the Redis + .NET executor pipeline.
* Electron IPC replaces SignalR + HTTP.
* No login, no Firebase, no cloud account &mdash; the client is single-user and
  works fully offline (provided you have Ollama running locally).

The original `Frontend/`, `WebAPI/`, `Executor/`, `Data/`, `Redis/`, etc.
projects are intentionally left in place; the website (`Frontend/`) now serves
as a marketing site whose **Download** page (auto-detecting the visitor's OS)
links to ThickClient builds.

---

## Project layout

```
ThickClient/
├── package.json                # Electron + Vite scripts and dependencies
├── src/
│   ├── main/
│   │   ├── index.js            # Electron main process entry
│   │   ├── db/database.js      # SQLite schema + connection
│   │   ├── services/           # project / node / chat / memory / template / ollama executor
│   │   └── ipc/                # IPC handler registration
│   └── preload/preload.js      # contextBridge exposing window.nodpt
└── renderer/                   # Riot.js + Vite UI (copied from Frontend/, login removed)
    ├── index.html
    ├── vite.config.js
    └── src/
        ├── app.riot
        ├── index.js
        ├── components/         # graph.riot, chat.riot, modals, etc.
        ├── pages/              # project, editor, create, terms, about, not-found
        ├── plugins/
        │   └── api-plugin.js   # IPC shim that mirrors the old axios api surface
        ├── services/           # projectApiService, chatApiService, ... (unchanged)
        └── styles/
```

## Prerequisites

* Node.js 18+
* [Ollama](https://ollama.com/) installed and running (`ollama serve`)
* At least one Ollama model pulled, e.g. `ollama pull llama3.2:3b`

## Install

```bash
cd ThickClient
npm install
```

`better-sqlite3` builds a native module on install; no manual rebuild is
needed when the Electron and Node ABI line up. If they don't, run
`npx electron-rebuild` once.

## Develop

```bash
npm run dev
```

This runs the Vite renderer dev server on `http://localhost:5173` and starts
Electron with `NODPT_DEV=1`, which loads the dev URL and opens DevTools.

## Build & package

```bash
npm run build:renderer     # produce renderer/dist
npm run package            # build native installers via electron-builder
```

Output goes to `dist-electron/` (NSIS installer for Windows, DMG for macOS,
AppImage for Linux). Update the URLs in `Frontend/src/src/pages/download.riot`
once these artifacts are published to GitHub Releases.

## Configuration

Environment variables read by the main process:

| Variable           | Default                         | Description                              |
|--------------------|---------------------------------|------------------------------------------|
| `OLLAMA_ENDPOINT`  | `http://localhost:11434`        | Base URL of the local Ollama server.     |
| `OLLAMA_MODEL`     | `llama3.2:3b`                   | Default model used by the executor.      |
| `NODPT_DEV`        | unset                           | When `1`, loads renderer from Vite dev.  |

The SQLite database is stored under Electron's `userData` directory:

* macOS:   `~/Library/Application Support/NodPT/nodpt.sqlite`
* Linux:   `~/.config/NodPT/nodpt.sqlite`
* Windows: `%APPDATA%\NodPT\nodpt.sqlite`

## IPC API

All renderer &harr; main calls go through the contextBridge object
`window.nodpt`:

```js
await window.nodpt.projects.list()
await window.nodpt.projects.create({ Name: 'My Project' })
await window.nodpt.nodes.create({ NodeType: 'Director', ProjectId: 1 })
await window.nodpt.chat.send({ NodeId: '...', Message: 'hello' })

const off = window.nodpt.chat.onComplete(({ AiMessage }) => { /* ... */ })
```

The legacy renderer services (`projectApiService`, `chatApiService`, ...) call
into `api-plugin.js`, which translates the original WebAPI URLs into IPC calls
&mdash; so existing components keep working without modification.
