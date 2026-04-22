import api from './api';

export interface ParsedAsset {
  ticker: string;
  classe: string;
  quantidade: number;
  instituicao: string;
  precoMedio?: number | null;
}

export interface B3ImportResult {
  parsedAssets: ParsedAsset[];
  errors: string[];
  totalRowsProcessed: number;
}

export const portfolioService = {
  importB3: async (file: File): Promise<B3ImportResult> => {
    const formData = new FormData();
    formData.append('file', file);

    const { data } = await api.post<B3ImportResult>('/portfolio/import/b3', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return data;
  },
};
