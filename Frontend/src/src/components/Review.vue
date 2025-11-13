<template>
	<div class="review-container">
		<!-- Dynamic Review Component -->
		<div class="review-content">
			<SoftwareDevReview />
		</div>
	</div>
</template>

<script>
import SoftwareDevReview from './SoftwareDevReview.vue';

export default {
	name: 'Review',
	components: {
		SoftwareDevReview,
	},
};
</script>


