<script setup lang="ts">
import { onMounted } from 'vue';
import { useProductsStore } from '@/stores/products';
import BaseCard from '@/components/common/BaseCard.vue';

const productsStore = useProductsStore();

onMounted(async () => {
  await productsStore.search();
});

const runSearch = async () => {
  productsStore.page = 1;
  await productsStore.search();
};

const goNext = async () => {
  productsStore.page += 1;
  await productsStore.search();
};

const goPrev = async () => {
  productsStore.page -= 1;
  await productsStore.search();
};
</script>

<template>
  <BaseCard>
    <h2 style="margin-top:0">Product Search</h2>
    <div style="display:grid;grid-template-columns:1fr auto auto auto; gap:0.5rem; margin-bottom:1rem; align-items:end;">
      <label style="display:grid;gap:0.25rem;">
        <span>Search</span>
        <input v-model="productsStore.query" class="input" type="text" placeholder="Search products" @keyup.enter="runSearch" />
      </label>
      <label style="display:grid;gap:0.25rem;">
        <span>Sort By</span>
        <select v-model="productsStore.sortBy" class="input">
          <option value="name">Name</option>
          <option value="category">Category</option>
          <option value="basePrice">Base Price</option>
          <option value="seller">Seller</option>
        </select>
      </label>
      <label style="display:grid;gap:0.25rem;">
        <span>Direction</span>
        <select v-model="productsStore.sortDirection" class="input">
          <option value="asc">Ascending</option>
          <option value="desc">Descending</option>
        </select>
      </label>
      <button class="btn btn-primary" @click="runSearch" :disabled="productsStore.loading">
        {{ productsStore.loading ? 'Searching...' : 'Search' }}
      </button>
    </div>

    <p v-if="productsStore.total">{{ productsStore.total }} results</p>
    <ul style="list-style:none;padding:0;display:grid;gap:0.75rem;">
      <li v-for="item in productsStore.sortedItems" :key="item.id" class="card">
        <strong>{{ item.name }}</strong>
        <div>{{ item.category }} • {{ item.seller }}</div>
        <div>${{ item.basePrice.toFixed(2) }}</div>
      </li>
    </ul>

    <div style="display:flex;justify-content:flex-end;gap:0.5rem;margin-top:0.75rem;">
      <button class="btn" :disabled="productsStore.page <= 1 || productsStore.loading" @click="goPrev">Previous</button>
      <span>Page {{ productsStore.page }}</span>
      <button
        class="btn"
        :disabled="productsStore.loading || productsStore.page * productsStore.pageSize >= productsStore.total"
        @click="goNext"
      >
        Next
      </button>
    </div>
  </BaseCard>
</template>
