import { defineStore } from 'pinia';
import { ecommerceApi } from '@/api/ecommerceApi';
import type { ProductSearchItem } from '@/types/ecommerce';

interface ProductState {
  loading: boolean;
  items: ProductSearchItem[];
  total: number;
}

export const useProductsStore = defineStore('products', {
  state: (): ProductState => ({
    loading: false,
    items: [],
    total: 0
  }),
  actions: {
    async search(query: string, page = 1, pageSize = 20) {
      this.loading = true;
      try {
        const { data } = await ecommerceApi.searchProducts(query, page, pageSize);
        this.items = data.items;
        this.total = data.total;
      } finally {
        this.loading = false;
      }
    }
  }
});
