import api from './api';
import type { ChatMessage, ChatResponse } from '../types/chat';

export const chatService = {
  async sendMessage(
    message: string,
    context: 'onboarding' | 'comparison',
    history: ChatMessage[] = []
  ): Promise<ChatResponse> {
    const response = await api.post<ChatResponse>('/chat', {
      message,
      context,
      history,
    });
    return response.data;
  },
};
