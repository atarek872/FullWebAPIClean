import { defineStore } from 'pinia';
import { ProductsService } from '@/services/api';
import type { ProductSearchItem } from '@/types/ecommerce';

interface ProductState {
  loading: boolean;
  items: ProductSearchItem[];
  total: number;
  query: string;
  page: number;
  pageSize: number;
  sortBy: 'name' | 'category' | 'basePrice' | 'seller';
  sortDirection: 'asc' | 'desc';
}

export const useProductsStore = defineStore('products', {
  state: (): ProductState => ({
    loading: false,
    items: [],
    total: 0,
    query: '',
    page: 1,
    pageSize: 10,
    sortBy: 'name',
    sortDirection: 'asc'
  }),
  getters: {
    sortedItems(state) {
      return [...state.items].sort((a, b) => {
        const left = String(a[state.sortBy]).toLowerCase();
        const right = String(b[state.sortBy]).toLowerCase();
        const result = left > right ? 1 : -1;
        return state.sortDirection === 'asc' ? result : -result;
      });
    }
  },
  actions: {
    async search() {
      this.loading = true;
      try {
        const { data } = await ProductsService.search(this.query, this.page, this.pageSize);
        this.items = data.items;
        this.total = data.total;
      } finally {
        this.loading = false;
      }
    }
  }
});
