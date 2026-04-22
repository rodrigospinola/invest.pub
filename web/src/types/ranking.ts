export interface RankingItem {
  posicao: number;
  ticker: string;
  nome: string;
  scoreTotal: number;
  scoreQuantitativo: number;
  scoreQualitativo: number;
  justificativa?: string;
  indicadores?: Record<string, unknown>;
  entrouHoje: boolean;
  saiuHoje: boolean;
}

export interface RankingResponse {
  subEstrategia: string;
  dataRanking: string;
  itens: RankingItem[];
}

export interface SuggestionAsset {
  posicao: number;
  ticker: string;
  nome: string;
  scoreTotal: number;
  justificativa?: string;
}

export interface SuggestionResponse {
  userId: string;
  subEstrategiaAcoes: string;
  subEstrategiaFiis: string;
  acoesRec: SuggestionAsset[];
  fiisRec: SuggestionAsset[];
}

export interface SubStrategy {
  userId: string;
  subEstrategiaAcoes: string;
  subEstrategiaFiis: string;
  createdAt: string;
}

export interface ImportAsset {
  ticker: string;
  nome: string;
  classe: string;
  quantidade: number;
  precoMedio: number;
}

export interface AssetItem {
  id: string;
  ticker: string;
  nome: string;
  classe: string;
  quantidade: number;
  precoMedio: number;
  origem: string;
  ativo: boolean;
  createdAt: string;
}
