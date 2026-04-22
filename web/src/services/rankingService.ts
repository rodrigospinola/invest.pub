import api from './api';
import type { RankingResponse, SuggestionResponse, SubStrategy, ImportAsset, AssetItem } from '../types/ranking';

export const rankingService = {
  async getTop20(subEstrategia: string): Promise<RankingResponse> {
    const response = await api.get<RankingResponse>('/ranking/top20', {
      params: { subEstrategia },
    });
    return response.data;
  },

  async getSuggestion(): Promise<SuggestionResponse> {
    const response = await api.get<SuggestionResponse>('/ranking/suggestion');
    return response.data;
  },

  async getSubStrategy(): Promise<SubStrategy> {
    const response = await api.get<SubStrategy>('/sub-strategy');
    return response.data;
  },

  async createSubStrategy(subEstrategiaAcoes: string, subEstrategiaFiis: string): Promise<SubStrategy> {
    const response = await api.post<SubStrategy>('/sub-strategy', {
      subEstrategiaAcoes,
      subEstrategiaFiis,
    });
    return response.data;
  },

  async importPortfolio(ativos: ImportAsset[]): Promise<{ totalImportados: number; sugeridos: number; proprios: number }> {
    const response = await api.post<{ totalImportados: number; sugeridos: number; proprios: number }>('/portfolio/import', { ativos });
    return response.data;
  },

  async getAssets(): Promise<AssetItem[]> {
    const response = await api.get<{ ativos: AssetItem[] }>('/portfolio/assets');
    return response.data.ativos;
  },
};
