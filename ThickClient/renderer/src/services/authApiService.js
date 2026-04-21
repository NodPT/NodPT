// ThickClient stub: the desktop client is single-user and does not perform
// remote authentication. This module preserves the surface of the original
// `authApiService` so existing renderer components keep working without
// changes, but every operation is a local no-op.

class AuthApiService {
	setApi() {}
	getUserData() {
		return { Email: 'local@nodpt.local', DisplayName: 'Local User', PhotoUrl: null }
	}
	isAuthenticated() {
		return true
	}
	async loginAndStore() {
		return this.getUserData()
	}
	async logout() {
		return true
	}
}

export default new AuthApiService()
