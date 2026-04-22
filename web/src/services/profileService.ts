import api from './api';
import type { Profile, AllocationResponse } from '../types/profile';

export const profileService = {
  async getProfile(): Promise<Profile> {
    const response = await api.get<Profile>('/profile');
    return response.data;
  },

  async createProfile(data: {
    perfil: string;
    valorTotal: number;
    temCarteiraExistente: boolean;
    carteiraAnterior?: Record<string, number>;
  }): Promise<Profile> {
    const response = await api.post<Profile>('/profile', data);
    return response.data;
  },

  async getAllocation(perfil: string, valor: number): Promise<AllocationResponse> {
    const response = await api.get<AllocationResponse>('/allocation', {
      params: { perfil, valor },
    });
    return response.data;
  },

  /** Remove perfil, sub-estratégia e todos os ativos. O usuário volta ao onboarding. */
  async resetProfile(): Promise<void> {
    await api.delete('/profile/reset');
  },
};
