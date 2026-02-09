import { HelloService } from '@/services/api/HelloService';

export const helloApi = {
  getHello: HelloService.get
};
