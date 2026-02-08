import { describe, it, expect } from 'vitest';
import { readFileSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const srcDir = resolve(__dirname, '..');

function loadFormat(nodeType) {
	const filePath = resolve(srcDir, nodeType, 'format.json');
	const raw = readFileSync(filePath, 'utf-8');
	return JSON.parse(raw);
}

describe('Director format', () => {
	const format = loadFormat('Director');

	it('should have a content field of type string', () => {
		expect(format).toHaveProperty('content');
		expect(typeof format.content).toBe('string');
	});

	it('should have a managers array', () => {
		expect(format).toHaveProperty('managers');
		expect(Array.isArray(format.managers)).toBe(true);
	});

	it('each manager should have name and job fields', () => {
		for (const manager of format.managers) {
			expect(manager).toHaveProperty('name');
			expect(manager).toHaveProperty('job');
			expect(typeof manager.name).toBe('string');
			expect(typeof manager.job).toBe('string');
		}
	});
});

describe('Manager format', () => {
	const format = loadFormat('Manager');

	it('should have a content field of type string', () => {
		expect(format).toHaveProperty('content');
		expect(typeof format.content).toBe('string');
	});

	it('should have a supervisors array', () => {
		expect(format).toHaveProperty('supervisors');
		expect(Array.isArray(format.supervisors)).toBe(true);
	});

	it('each supervisor should have name and job fields', () => {
		for (const supervisor of format.supervisors) {
			expect(supervisor).toHaveProperty('name');
			expect(supervisor).toHaveProperty('job');
			expect(typeof supervisor.name).toBe('string');
			expect(typeof supervisor.job).toBe('string');
		}
	});
});

describe('Supervisor format', () => {
	const format = loadFormat('Supervisor');

	it('should have a content field of type string', () => {
		expect(format).toHaveProperty('content');
		expect(typeof format.content).toBe('string');
	});

	it('should have an agents array', () => {
		expect(format).toHaveProperty('agents');
		expect(Array.isArray(format.agents)).toBe(true);
	});

	it('each agent should have name and job fields', () => {
		for (const agent of format.agents) {
			expect(agent).toHaveProperty('name');
			expect(agent).toHaveProperty('job');
			expect(typeof agent.name).toBe('string');
			expect(typeof agent.job).toBe('string');
		}
	});
});

describe('Agent format', () => {
	const format = loadFormat('Agent');

	it('should have a content field of type string', () => {
		expect(format).toHaveProperty('content');
		expect(typeof format.content).toBe('string');
	});

	it('should have a files array', () => {
		expect(format).toHaveProperty('files');
		expect(Array.isArray(format.files)).toBe(true);
	});

	it('each file should have filename and content fields', () => {
		for (const file of format.files) {
			expect(file).toHaveProperty('filename');
			expect(file).toHaveProperty('content');
			expect(typeof file.filename).toBe('string');
			expect(typeof file.content).toBe('string');
		}
	});
});

describe('Node hierarchy consistency', () => {
	it('director delegates to managers', () => {
		const director = loadFormat('Director');
		expect(director.managers.length).toBeGreaterThan(0);
		expect(director).not.toHaveProperty('supervisors');
		expect(director).not.toHaveProperty('agents');
		expect(director).not.toHaveProperty('files');
	});

	it('manager delegates to supervisors', () => {
		const manager = loadFormat('Manager');
		expect(manager.supervisors.length).toBeGreaterThan(0);
		expect(manager).not.toHaveProperty('managers');
		expect(manager).not.toHaveProperty('agents');
		expect(manager).not.toHaveProperty('files');
	});

	it('supervisor delegates to agents', () => {
		const supervisor = loadFormat('Supervisor');
		expect(supervisor.agents.length).toBeGreaterThan(0);
		expect(supervisor).not.toHaveProperty('managers');
		expect(supervisor).not.toHaveProperty('supervisors');
		expect(supervisor).not.toHaveProperty('files');
	});

	it('agent produces files as the last node', () => {
		const agent = loadFormat('Agent');
		expect(agent.files.length).toBeGreaterThan(0);
		expect(agent).not.toHaveProperty('managers');
		expect(agent).not.toHaveProperty('supervisors');
		expect(agent).not.toHaveProperty('agents');
	});

	it('all formats have a content field', () => {
		const types = ['Director', 'Manager', 'Supervisor', 'Agent'];
		for (const type of types) {
			const format = loadFormat(type);
			expect(format).toHaveProperty('content');
			expect(typeof format.content).toBe('string');
			expect(format.content.length).toBeGreaterThan(0);
		}
	});
});
