// ThickClient stub: no remote user accounts in the desktop client.

class UserApiService {
	setApi() {}
	async getCurrentUser() {
		return { Email: 'local@nodpt.local', DisplayName: 'Local User', PhotoUrl: null }
	}
}

export default new UserApiService()
