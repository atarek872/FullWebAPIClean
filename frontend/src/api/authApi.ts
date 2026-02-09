import { AuthService } from '@/services/api/AuthService';

export const authApi = {
  login: AuthService.login,
  revokeAll: AuthService.revokeAll
};
