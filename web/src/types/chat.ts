export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
}

export interface AllocationPreviewItem {
  classe: string;
  percentual: number;
}

export interface ChatResponse {
  response: string;
  history: ChatMessage[];
  toolsCalled?: string[];
  suggestedReplies?: string[];
  allocationPreview?: AllocationPreviewItem[];
}
