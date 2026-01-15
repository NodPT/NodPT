// Use environment variable if available, otherwise fallback to localhost
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5049/api';

class TemplateApiService {
	constructor() {
		this.baseURL = `${API_BASE_URL}/templates`;
		this.api = null;
	}

	setApi(api) {
		this.api = api;
	}

	async getTemplates() {
		if (!this.api) {
			throw new Error('API not initialized. Call setApi() first.');
		}

		try {
			const response = await this.api.get(this.baseURL);
			return response;
		} catch (error) {
			console.error('Error fetching templates:', error);
			throw error;
		}
	}

	async getTemplate(id) {
		if (!this.api) {
			throw new Error('API not initialized. Call setApi() first.');
		}

		try {
			const response = await this.api.get(`${this.baseURL}/${id}`);
			return response;
		} catch (error) {
			console.error('Error fetching template:', error);
			throw error;
		}
	}

	async createTemplate(templateData) {
		if (!this.api) {
			throw new Error('API not initialized. Call setApi() first.');
		}

		try {
			const response = await this.api.post(this.baseURL, templateData);
			return response;
		} catch (error) {
			console.error('Error creating template:', error);
			throw error;
		}
	}

	async updateTemplate(id, templateData) {
		if (!this.api) {
			throw new Error('API not initialized. Call setApi() first.');
		}

		try {
			const response = await this.api.put(`${this.baseURL}/${id}`, templateData);
			return response;
		} catch (error) {
			console.error('Error updating template:', error);
			throw error;
		}
	}

	async deleteTemplate(id) {
		if (!this.api) {
			throw new Error('API not initialized. Call setApi() first.');
		}

		try {
			const response = await this.api.delete(`${this.baseURL}/${id}`);
			return response;
		} catch (error) {
			console.error('Error deleting template:', error);
			throw error;
		}
	}
}

export default new TemplateApiService();
