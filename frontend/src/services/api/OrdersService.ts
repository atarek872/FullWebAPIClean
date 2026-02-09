import api from '@/api/httpClient';

export interface CheckoutCustomFieldValueRequest {
  key: string;
  value: string;
}

export interface CheckoutItemRequest {
  productId: string;
  quantity: number;
  promoCode?: string;
  customFields?: CheckoutCustomFieldValueRequest[];
}

export interface CheckoutRequest {
  buyerName: string;
  buyerEmail: string;
  items: CheckoutItemRequest[];
}

export const OrdersService = {
  checkout(payload: CheckoutRequest) {
    return api.post('/api/ecommerce/checkout', payload);
  }
};
