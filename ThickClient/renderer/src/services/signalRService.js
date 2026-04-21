// ThickClient replacement for the original SignalR-based realtime service.
//
// In the desktop client we don't run a hub - instead the Electron preload
// bridge exposes the same kind of "stream / complete / error" events directly
// from the local executor on `window.nodpt.chat`. This module adapts those
// events to the SignalR-style API used by existing renderer components, so
// they keep working without modification.

const ipc = (typeof window !== 'undefined' && window.nodpt) ? window.nodpt : null

class SignalRService {
	constructor() {
		this.connection = null
		this.handlers = new Map()
		this.unsubs = []
	}

	get connectionId() {
		// The desktop client doesn't use a remote hub; surface a stable local id.
		return 'local-thickclient'
	}

	async start() {
		if (!ipc) return
		this.unsubs.push(ipc.chat.onStream((p) => this.emit('chatStream', p)))
		this.unsubs.push(ipc.chat.onComplete((p) => this.emit('chatComplete', p)))
		this.unsubs.push(ipc.chat.onError((p) => this.emit('chatError', p)))
		try { localStorage.setItem('connectionId', this.connectionId) } catch (_) {}
	}

	async stop() {
		this.unsubs.forEach((u) => { try { u() } catch (_) {} })
		this.unsubs = []
	}

	on(eventName, handler) {
		if (!this.handlers.has(eventName)) this.handlers.set(eventName, new Set())
		this.handlers.get(eventName).add(handler)
	}

	off(eventName, handler) {
		const set = this.handlers.get(eventName)
		if (set) set.delete(handler)
	}

	emit(eventName, payload) {
		const set = this.handlers.get(eventName)
		if (!set) return
		for (const h of set) {
			try { h(payload) } catch (e) { console.error(e) }
		}
	}
}

export default new SignalRService()
