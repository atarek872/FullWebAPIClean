import api from '@/api/httpClient';

export interface UpsertSellerProfileRequest {
  storeName: string;
  storeDescription?: string;
  slug?: string;
  isPublished: boolean;
}

export const SellerProfilesService = {
  upsert(payload: UpsertSellerProfileRequest) {
    return api.post('/api/ecommerce/seller/profile', payload);
  }
};
