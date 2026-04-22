export interface AllocationClasse {
  classe: string;
  percentual: number;
}

export interface Profile {
  id: string;
  userId: string;
  perfil: 'conservador' | 'moderado' | 'arrojado';
  valorTotal: number;
  faixa: 'ate_10k' | '10k_100k' | 'acima_100k';
  temCarteiraExistente: boolean;
  alocacaoAlvo: AllocationClasse[];
  createdAt: string;
  updatedAt: string;
}

export interface AllocationResponse {
  faixa: string;
  perfil: string;
  classes: AllocationClasse[];
}
