<template>
  <div class="rete-minimap" ref="minimapContainer"></div>
</template>

<script>
import { onMounted, ref } from 'vue';
import { listenEvent, EVENT_TYPES } from '../rete/eventBus';

export default {
  name: 'Minimap',
  props: {
    visible: {
      type: Boolean,
      default: false
    }
  },
  setup(props) {
    const minimapContainer = ref(null);
    
    onMounted(() => {
      // The minimap is actually rendered by Rete.js plugin
      // This is just a placeholder component for positioning
      console.log('Minimap component mounted');
    });

    return {
      minimapContainer
    };
  }
}
</script>


