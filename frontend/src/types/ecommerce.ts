export interface ProductSearchItem {
  id: string;
  name: string;
  description?: string;
  category: string;
  basePrice: number;
  discountPercentage?: number;
  seller: string;
  thumbnail?: string;
  searchScore: number;
}

export interface ProductSearchResponse {
  total: number;
  page: number;
  pageSize: number;
  items: ProductSearchItem[];
}
