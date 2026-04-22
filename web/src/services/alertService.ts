import api from './api';
import type { AlertSummary } from './dashboardService';

export const alertService = {
  getAlerts: async (): Promise<AlertSummary[]> => {
    const response = await api.get<AlertSummary[]>('/alerts');
    return response.data;
  },

  markAsRead: async (id: string): Promise<void> => {
    await api.post(`/alerts/${id}/read`);
  },
};
