// ThickClient renderer: replaces the HTTP-based api-plugin from the original
// Frontend with a thin shim that routes calls through the Electron IPC bridge
// exposed by `src/preload/preload.js` as `window.nodpt`.
//
// The shim presents the same `get/post/put/delete/fetch` surface as the
// original axios-based api so the existing service layer (projectApiService,
// chatApiService, etc.) keeps working with minimal modification.

const ipc = (typeof window !== 'undefined' && window.nodpt) ? window.nodpt : null

function notInElectron() {
	throw new Error(
		'ThickClient api-plugin invoked outside of Electron. ' +
		'Make sure the renderer was loaded by the Electron main process.',
	)
}

function stripLeadingSlash(s) {
	return s.startsWith('/') ? s.slice(1) : s
}

function parseId(seg) {
	const n = Number(seg)
	return Number.isFinite(n) ? n : seg
}

/**
 * Translate the URL + method that the original frontend services would have
 * sent to the WebAPI into a call against the local IPC bridge.
 *
 * Supported routes (kept intentionally narrow - extend as needed):
 *   GET    /projects                       -> nodpt.projects.list()
 *   POST   /projects                       -> nodpt.projects.create(body)
 *   GET    /projects/:id                   -> nodpt.projects.get(id)
 *   PUT    /projects/:id/name              -> nodpt.projects.updateName(id, body.Name)
 *   DELETE /projects/:id                   -> nodpt.projects.delete(id)
 *   GET    /chat/node/:nodeId              -> nodpt.chat.listByNode(nodeId)
 *   POST   /chat/send                      -> nodpt.chat.send(body)
 *   POST   /chat/mark-solution             -> nodpt.chat.markSolution(body.MessageId)
 *   POST   /chat/retry                     -> nodpt.chat.retry(body.MessageId)
 *   GET    /templates                      -> nodpt.templates.list()
 *   GET    /templates/:id                  -> nodpt.templates.get(id)
 *   GET    /nodes/:projectId               -> nodpt.nodes.list(projectId)
 *   GET    /nodes/project/:projectId       -> nodpt.nodes.list(projectId)
 *   POST   /nodes                          -> nodpt.nodes.create(body)
 *   PUT    /nodes/:id                      -> nodpt.nodes.update(id, body)
 *   DELETE /nodes/:id                      -> nodpt.nodes.delete(id)
 */
async function dispatch(method, url, body) {
	if (!ipc) notInElectron()
	const path = stripLeadingSlash(url.split('?')[0])
	const parts = path.split('/').filter(Boolean)
	const [resource, a, b] = parts

	if (resource === 'projects') {
		if (method === 'GET' && !a) return ipc.projects.list()
		if (method === 'POST' && !a) return ipc.projects.create(body)
		if (method === 'GET' && a && !b) return ipc.projects.get(parseId(a))
		if (method === 'PUT' && b === 'name') return ipc.projects.updateName(parseId(a), body?.Name)
		if (method === 'DELETE' && a && !b) return ipc.projects.delete(parseId(a))
	}

	if (resource === 'chat') {
		if (method === 'GET' && a === 'node' && b) return ipc.chat.listByNode(b)
		if (method === 'POST' && a === 'send') return ipc.chat.send(body)
		if (method === 'POST' && a === 'mark-solution') return ipc.chat.markSolution(body?.MessageId)
		if (method === 'POST' && a === 'retry') return ipc.chat.retry(body?.MessageId)
	}

	if (resource === 'templates') {
		if (method === 'GET' && !a) return ipc.templates.list()
		if (method === 'GET' && a) return ipc.templates.get(parseId(a))
	}

	if (resource === 'nodes') {
		// /nodes/project/:projectId  - list nodes for a project
		if (method === 'GET' && a === 'project' && b) return ipc.nodes.list(parseId(b))
		// /nodes/:projectId          - same, alternative shape
		if (method === 'GET' && a && !b) {
			const id = parseId(a)
			// Numeric id => list-by-project; otherwise treat as a node id lookup.
			if (typeof id === 'number') return ipc.nodes.list(id)
			return ipc.nodes.get(a)
		}
		if (method === 'POST' && !a) return ipc.nodes.create(body)
		if (method === 'PUT' && a) return ipc.nodes.update(a, body)
		if (method === 'DELETE' && a) return ipc.nodes.delete(a)
	}

	throw new Error(`ThickClient api-plugin: unmapped route ${method} /${path}`)
}

const get = (url, params, _config) => {
	let qs = ''
	if (params && typeof params === 'object' && Object.keys(params).length) {
		qs = '?' + new URLSearchParams(params).toString()
	}
	return dispatch('GET', url + qs)
}
const post = (url, data) => dispatch('POST', url, data)
const put = (url, data) => dispatch('PUT', url, data)
const patch = (url, data) => dispatch('PATCH', url, data)
const del = (url) => dispatch('DELETE', url)
const fetch = (url, config = {}) => dispatch((config.method || 'GET').toUpperCase(), url, config.data)

const api = { get, post, put, patch, delete: del, fetch }

export default function apiPlugin(component) {
	component.$api = api
	component.api = api
	// Expose IPC bridge directly for components that need streaming / events.
	component.$nodpt = ipc
	component.nodpt = ipc
}

export { api }
