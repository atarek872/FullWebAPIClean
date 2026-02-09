import api from '@/api/httpClient';

export interface HelloResponse {
  message: string;
  timestamp: string;
}

export const HelloService = {
  get() {
    return api.get<HelloResponse>('/api/hello');
  },
  getByName(name: string) {
    return api.get<HelloResponse>(`/api/hello/${name}`);
  }
};
