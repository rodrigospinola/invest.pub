import api from './api';
import axios from 'axios';
import type { LoginRequest, RegisterRequest } from '../types/auth';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

// Resposta pública do backend (sem refreshToken — ele viaja só em cookie httpOnly)
interface PublicAuthResponse {
  accessToken: string;
  expiresAt: string;
  user: import('../types/auth').User;
}

export const authService = {
  async login(data: LoginRequest): Promise<PublicAuthResponse> {
    const response = await api.post<PublicAuthResponse>('/auth/login', data);
    return response.data;
  },

  async register(data: RegisterRequest): Promise<PublicAuthResponse> {
    const response = await api.post<PublicAuthResponse>('/auth/register', data);
    return response.data;
  },

  async refresh(): Promise<PublicAuthResponse> {
    const response = await axios.post<PublicAuthResponse>(
      `${API_URL}/auth/refresh`,
      {},
      { withCredentials: true }
    );
    return response.data;
  },

  async logout(): Promise<void> {
    await api.post('/auth/logout');
  },

  async forgotPassword(email: string): Promise<{ message: string }> {
    const response = await api.post<{ message: string }>('/auth/forgot-password', { email });
    return response.data;
  },
};
