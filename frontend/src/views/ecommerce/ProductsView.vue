<script setup lang="ts">
import { ref } from 'vue';
import { useProductsStore } from '@/stores/products';
import BaseCard from '@/components/common/BaseCard.vue';

const productsStore = useProductsStore();
const query = ref('');

const runSearch = async () => {
  await productsStore.search(query.value, 1, 20);
};
</script>

<template>
  <BaseCard>
    <h2 style="margin-top:0">Product Search</h2>
    <div style="display:flex; gap:0.5rem; margin-bottom:1rem;">
      <input v-model="query" class="input" type="text" placeholder="Search products" @keyup.enter="runSearch" />
      <button class="btn btn-primary" @click="runSearch" :disabled="productsStore.loading">
        {{ productsStore.loading ? 'Searching...' : 'Search' }}
      </button>
    </div>

    <p v-if="productsStore.total">{{ productsStore.total }} results</p>
    <ul style="list-style:none;padding:0;display:grid;gap:0.75rem;">
      <li v-for="item in productsStore.items" :key="item.id" class="card">
        <strong>{{ item.name }}</strong>
        <div>{{ item.category }} • {{ item.seller }}</div>
        <div>${{ item.basePrice.toFixed(2) }}</div>
      </li>
    </ul>
  </BaseCard>
</template>
