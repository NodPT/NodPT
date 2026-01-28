<template>
	<div class="tree-node">
		<div 
			:class="['node-content', { 'folder': node.type === 'folder' }]"
			:style="{ paddingLeft: (level * 20) + 'px' }"
			@click="selectNode"
		>
			<i 
				v-if="node.type === 'folder'" 
				:class="['toggle-icon', 'bi', isExpanded ? 'bi-chevron-down' : 'bi-chevron-right']"
			></i>
			<i :class="['file-icon', 'bi', getIcon()]"></i>
			<span class="node-name">{{ node.name }}</span>
			<small v-if="node.type === 'file'" class="file-size text-muted">{{ formatFileSize(node.size) }}</small>
		</div>
		<div v-if="isExpanded && node.children" class="node-children">
			<FileTreeNode 
				v-for="child in node.children" 
				:key="child.id"
				:node="child" 
				:level="level + 1"
				@select="$emit('select', $event)"
			/>
		</div>
	</div>
</template>

<script>
import { ref } from 'vue';

export default {
	name: 'FileTreeNode',
	props: {
		node: {
			type: Object,
			required: true
		},
		level: {
			type: Number,
			default: 0
		}
	},
	emits: ['select'],
	setup(props, { emit }) {
		const isExpanded = ref(props.node.type === 'folder' ? props.node.expanded || false : false);
		
		const toggleExpanded = () => {
			if (props.node.type === 'folder') {
				isExpanded.value = !isExpanded.value;
			}
		};
		
		const selectNode = () => {
			emit('select', props.node);
			if (props.node.type === 'folder') {
				toggleExpanded();
			}
		};
		
		const getIcon = () => {
			if (props.node.type === 'folder') {
				return isExpanded.value ? 'bi-folder2-open' : 'bi-folder2';
			}
			
			const ext = props.node.name.split('.').pop().toLowerCase();
			switch (ext) {
				case 'js': case 'ts': case 'jsx': case 'tsx':
					return 'bi-filetype-js';
				case 'vue':
					return 'bi-filetype-vue';
				case 'json':
					return 'bi-filetype-json';
				case 'css': case 'scss': case 'sass':
					return 'bi-filetype-css';
				case 'html':
					return 'bi-filetype-html';
				case 'md':
					return 'bi-filetype-md';
				case 'txt':
					return 'bi-filetype-txt';
				case 'pdf':
					return 'bi-filetype-pdf';
				case 'png': case 'jpg': case 'jpeg': case 'gif': case 'svg':
					return 'bi-file-earmark-image';
				default:
					return 'bi-file-earmark';
			}
		};
		
		const formatFileSize = (bytes) => {
			if (!bytes) return '';
			const sizes = ['B', 'KB', 'MB', 'GB'];
			const i = Math.floor(Math.log(bytes) / Math.log(1024));
			return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
		};
		
		return {
			isExpanded,
			toggleExpanded,
			selectNode,
			getIcon,
			formatFileSize
		};
	}
};
</script>


