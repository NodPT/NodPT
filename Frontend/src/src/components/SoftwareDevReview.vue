<template>
	<div class="software-dev-review">
		<div class="d-flex justify-content-between align-items-center mb-3">
			<h6 class="mb-0">Preview</h6>
			<div class="review-controls">
				<button 
					@click="setDesktopView" 
					:class="['btn', 'btn-sm', 'me-2', currentView === 'desktop' ? 'btn-primary' : 'btn-outline-primary']"
				>
					<i class="bi bi-laptop"></i>
				</button>
				<button 
					@click="setMobileView" 
					:class="['btn', 'btn-sm', 'me-2', currentView === 'mobile' ? 'btn-primary' : 'btn-outline-primary']"
				>
					<i class="bi bi-phone"></i>
				</button>
				<button @click="refreshPreview" class="btn btn-sm btn-outline-success">
					<i class="bi bi-arrow-clockwise"></i>
				</button>
			</div>
		</div>

		<!-- App Preview Frame -->
		<div :class="['app-preview-container', currentView]">
			<div class="app-preview-header">
				<div class="browser-controls">
					<span class="control-dot red"></span>
					<span class="control-dot yellow"></span>
					<span class="control-dot green"></span>
				</div>
				<div class="url-bar">
					<i class="bi bi-shield-lock text-success me-1"></i>
					<span class="url-text">{{ previewUrl }}</span>
				</div>
				<div class="browser-actions">
					<i class="bi bi-arrow-left text-muted"></i>
					<i class="bi bi-arrow-right text-muted ms-1"></i>
				</div>
			</div>
			
			<div class="app-preview-content">
				<iframe 
					ref="previewFrame"
					:src="previewUrl" 
					class="preview-iframe"
					@load="onIframeLoad"
					@error="onIframeError"
				></iframe>
			</div>
		</div>

		<!-- Development Info -->
		<div class="dev-info mt-3">
			<div class="row">
				<div class="col-md-6">
					<div class="info-card">
						<div class="info-header">
							<i class="bi bi-gear text-primary me-2"></i>
							<strong>Build Status</strong>
						</div>
						<div class="info-body">
							<span :class="['badge', buildStatus === 'success' ? 'bg-success' : buildStatus === 'building' ? 'bg-warning' : 'bg-danger']">
								{{ buildStatus === 'success' ? 'Built Successfully' : buildStatus === 'building' ? 'Building...' : 'Build Failed' }}
							</span>
						</div>
					</div>
				</div>
				<div class="col-md-6">
					<div class="info-card">
						<div class="info-header">
							<i class="bi bi-speedometer2 text-info me-2"></i>
							<strong>Performance</strong>
						</div>
						<div class="info-body">
							<small class="text-muted">Load time: {{ loadTime }}ms</small>
						</div>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<script>
import { ref, onMounted } from 'vue';

export default {
	name: 'SoftwareDevReview',
	setup() {
		const previewUrl = ref('https://www.apple.com');
		const currentView = ref('desktop');
		const buildStatus = ref('success');
		const loadTime = ref(245);
		const previewFrame = ref(null);

		const setDesktopView = () => {
			currentView.value = 'desktop';
			console.log('Switched to desktop view');
		};

		const setMobileView = () => {
			currentView.value = 'mobile';
			console.log('Switched to mobile view');
		};

		const refreshPreview = () => {
			console.log('Refreshing preview...');
			buildStatus.value = 'building';
			
			// Reload the iframe
			if (previewFrame.value) {
				previewFrame.value.src = previewFrame.value.src;
			}
			
			setTimeout(() => {
				buildStatus.value = 'success';
				loadTime.value = Math.floor(Math.random() * 500) + 100;
			}, 1000);
		};

		const onIframeLoad = () => {
			console.log('Preview loaded successfully');
			buildStatus.value = 'success';
		};

		const onIframeError = () => {
			console.error('Failed to load preview');
			buildStatus.value = 'error';
		};

		onMounted(() => {
			// Set to apple.com for testing as required
			previewUrl.value = 'https://www.apple.com';
		});

		return {
			previewUrl,
			currentView,
			buildStatus,
			loadTime,
			previewFrame,
			setDesktopView,
			setMobileView,
			refreshPreview,
			onIframeLoad,
			onIframeError,
		};
	},
};
</script>


