import api from '@/api/httpClient';
import type { ProductSearchResponse } from '@/types/ecommerce';

export const ecommerceApi = {
  searchProducts(query: string, page = 1, pageSize = 20) {
    return api.get<ProductSearchResponse>('/api/ecommerce/products/search', {
      params: { query, page, pageSize }
    });
  }
};
