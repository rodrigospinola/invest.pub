import api from './api';

export interface Deviation {
  classe: string;
  real: number;
  alvo: number;
  diferenca: number;
  diferencaPercentual: number;
  isAlertaExtraordinario: boolean;
}

export interface AlertSummary {
  id: string;
  titulo: string;
  mensagem: string;
  tipo: string;
  status: string;
  metadataJson: string | null;
  createdAt: string;
}

export interface DashboardData {
  valorTotal: number;
  rentabilidadeNoDia: number;
  rentabilidadeAcumulada: number;
  distanciaMeta: number;
  percentualMeta: number;
  alocacoes: Deviation[];
  alertasRecentes: AlertSummary[];
}

export interface HistoryPoint {
  data: string;
  valorTotal: number;
  rentabilidadeAcumulada: number;
}

export interface BenchmarkPoint {
  data: string;
  valor: number;
  variacaoNoDia: number;
}

export interface Benchmark {
  nome: string;
  pontos: BenchmarkPoint[];
}

export interface HistoryData {
  pontos: HistoryPoint[];
  benchmarks: Benchmark[];
}

export const dashboardService = {
  getDashboard: async (): Promise<DashboardData> => {
    const response = await api.get<DashboardData>('/dashboard');
    return response.data;
  },

  getHistory: async (lastDays: number = 30): Promise<HistoryData> => {
    const response = await api.get<HistoryData>(`/dashboard/history?lastDays=${lastDays}`);
    return response.data;
  },

  getAssetHistory: async (ticker: string, lastDays: number = 30) => {
    const response = await api.get(`/dashboard/asset-history/${ticker}?lastDays=${lastDays}`);
    return response.data;
  },
};
