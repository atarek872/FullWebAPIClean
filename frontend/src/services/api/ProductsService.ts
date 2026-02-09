import api from '@/api/httpClient';
import type { ProductSearchResponse } from '@/types/ecommerce';

export interface CreateProductImageRequest {
  url: string;
  altText?: string;
  sortOrder?: number;
}

export interface CreateProductCustomFieldRequest {
  key: string;
  label: string;
  isRequired: boolean;
}

export interface CreatePromoCodeRequest {
  code: string;
  discountPercentage: number;
  expiresAtUtc?: string;
}

export interface CreateProductRequest {
  name: string;
  description?: string;
  category: string;
  basePrice: number;
  discountPercentage?: number;
  images?: CreateProductImageRequest[];
  customFields?: CreateProductCustomFieldRequest[];
  promoCodes?: CreatePromoCodeRequest[];
}

export const ProductsService = {
  search(query: string, page = 1, pageSize = 20) {
    return api.get<ProductSearchResponse>('/api/ecommerce/products/search', {
      params: { query, page, pageSize }
    });
  },
  create(payload: CreateProductRequest) {
    return api.post('/api/ecommerce/products', payload);
  }
};
