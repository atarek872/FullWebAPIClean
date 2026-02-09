<script setup lang="ts">
interface Props {
  open: boolean;
  title: string;
  message: string;
  loading?: boolean;
}

defineProps<Props>();
const emit = defineEmits<{
  cancel: [];
  confirm: [];
}>();
</script>

<template>
  <div v-if="open" class="modal-backdrop">
    <div class="modal-panel card">
      <h3>{{ title }}</h3>
      <p>{{ message }}</p>
      <div style="display:flex;justify-content:flex-end;gap:0.5rem;">
        <button class="btn" @click="emit('cancel')">Cancel</button>
        <button class="btn btn-danger" :disabled="loading" @click="emit('confirm')">Delete</button>
      </div>
    </div>
  </div>
</template>
