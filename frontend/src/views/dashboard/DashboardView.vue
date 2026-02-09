<script setup lang="ts">
import { onMounted, ref } from 'vue';
import BaseCard from '@/components/common/BaseCard.vue';
import { HelloService } from '@/services/api';

const helloMessage = ref('Loading...');

onMounted(async () => {
  const { data } = await HelloService.get();
  helloMessage.value = `${data.message} (${new Date(data.timestamp).toLocaleString()})`;
});
</script>

<template>
  <BaseCard>
    <h2 style="margin-top:0">Dashboard</h2>
    <p>This frontend uses Vue 3 + Vite + Pinia + Axios and is ready to connect to your API.</p>
    <p><strong>API hello:</strong> {{ helloMessage }}</p>
  </BaseCard>
</template>
