import api from '@/api/httpClient';
import type { ProductSearchResponse } from '@/types/ecommerce';

export interface CreateProductImageRequest {
  imageUrl: string;
  altText?: string;
}

export interface CreateProductCustomFieldRequest {
  key: string;
  label: string;
  inputType: string;
  isRequired: boolean;
  placeholder?: string;
  allowedOptions?: string[];
}

export interface CreatePromoCodeRequest {
  code: string;
  discountPercentage: number;
  startsAtUtc: string;
  endsAtUtc: string;
  isActive: boolean;
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
