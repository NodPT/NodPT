<template>
	<div class="file-explorer">
		<div class="explorer-header d-flex justify-content-between align-items-center mb-3">
			<h6 class="mb-0">Output Files</h6>
			<button @click="refreshFiles" class="btn btn-sm btn-outline-secondary">
				<i class="bi bi-arrow-clockwise"></i>
			</button>
		</div>
		
		<div class="file-tree">
			<div v-for="item in fileStructure" :key="item.id" class="file-item">
				<FileTreeNode :node="item" :level="0" @select="selectFile" />
			</div>
		</div>
		
		<div v-if="selectedFile" class="file-preview mt-3">
			<div class="preview-header">
				<strong>{{ selectedFile.name }}</strong>
				<small class="text-muted ms-2">{{ formatFileSize(selectedFile.size) }}</small>
			</div>
			<div class="preview-info">
				<small class="text-muted">Modified: {{ formatTime(selectedFile.modified) }}</small>
			</div>
		</div>
	</div>
</template>

<script>
import { ref, reactive } from 'vue';
import FileTreeNode from './FileTreeNode.vue';

export default {
	name: 'FileExplorer',
	components: {
		FileTreeNode
	},
	setup() {
		const selectedFile = ref(null);
		
		// Demo file structure for output folder
		const fileStructure = reactive([
			{
				id: 1,
				name: 'output',
				type: 'folder',
				expanded: true,
				children: [
					{
						id: 2,
						name: 'builds',
						type: 'folder',
						expanded: false,
						children: [
							{
								id: 3,
								name: 'app-v1.0.0.js',
								type: 'file',
								size: 1024000,
								modified: new Date('2024-01-15T10:30:00Z').toISOString()
							},
							{
								id: 4,
								name: 'app-v1.0.0.css',
								type: 'file',
								size: 156000,
								modified: new Date('2024-01-15T10:30:00Z').toISOString()
							}
						]
					},
					{
						id: 5,
						name: 'logs',
						type: 'folder',
						expanded: false,
						children: [
							{
								id: 6,
								name: 'build.log',
								type: 'file',
								size: 45000,
								modified: new Date('2024-01-15T11:45:00Z').toISOString()
							},
							{
								id: 7,
								name: 'error.log',
								type: 'file',
								size: 2300,
								modified: new Date('2024-01-15T11:32:00Z').toISOString()
							}
						]
					},
					{
						id: 8,
						name: 'reports',
						type: 'folder',
						expanded: false,
						children: [
							{
								id: 9,
								name: 'test-results.json',
								type: 'file',
								size: 12000,
								modified: new Date('2024-01-15T09:15:00Z').toISOString()
							},
							{
								id: 10,
								name: 'coverage-report.html',
								type: 'file',
								size: 890000,
								modified: new Date('2024-01-15T09:16:00Z').toISOString()
							}
						]
					},
					{
						id: 11,
						name: 'README.md',
						type: 'file',
						size: 3400,
						modified: new Date('2024-01-14T16:20:00Z').toISOString()
					},
					{
						id: 12,
						name: 'package.json',
						type: 'file',
						size: 1200,
						modified: new Date('2024-01-15T08:45:00Z').toISOString()
					}
				]
			}
		]);
		
		const selectFile = (file) => {
			selectedFile.value = file;
			console.log('Selected file:', file);
		};
		
		const refreshFiles = () => {
			console.log('Refreshing file structure...');
			// In a real app, this would reload from backend
		};
		
		const formatTime = (timestamp) => {
			if (!timestamp) return '';
			const date = new Date(timestamp);
			return date.toLocaleString();
		};
		
		const formatFileSize = (bytes) => {
			if (!bytes) return '';
			const sizes = ['B', 'KB', 'MB', 'GB'];
			const i = Math.floor(Math.log(bytes) / Math.log(1024));
			return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
		};
		
		return {
			fileStructure,
			selectedFile,
			selectFile,
			refreshFiles,
			formatTime,
			formatFileSize
		};
	}
};
</script>


