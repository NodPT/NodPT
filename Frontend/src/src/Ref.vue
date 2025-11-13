<template>
	<div v-bind="$props" ref="element"></div>
</template>

<script>
export default {
	props: ['data', 'emit'],
	mounted() {
		this.emit({ type: 'render', data: { ...this.data, element: this.$refs.element } });
	},
	beforeUnmount() {
		this.emit({ type: 'unmount', data: { element: this.$refs.element } });
	},
};
</script>
