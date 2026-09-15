import { api } from './api';

export const authService = {
  login: (credentials) => api.post('/auth/login', credentials),
  register: (payload) => api.post('/auth/register', payload),
  logout: () => api.post('/auth/logout'),
};
