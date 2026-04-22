export interface ProductRecommendation {
  nome: string;
  desc: string;
  tipo: string;
}

export interface ClassInfo {
  emoji: string;
  description: string;
  qtdMin: number;
  qtdMax: number;
  isDynamic: boolean;
  produtos: ProductRecommendation[];
}

export const CLASS_INFO: Record<string, ClassInfo> = {
  'RF Dinâmica': {
    emoji: '💵',
    description:
      'Liquidez e segurança. Títulos pós-fixados atrelados à taxa Selic — ideais para reserva de oportunidade com resgate rápido.',
    qtdMin: 1,
    qtdMax: 2,
    isDynamic: false,
    produtos: [
      { nome: 'Tesouro Selic', desc: 'Título público federal, liquidez diária, sem risco de crédito', tipo: 'Tesouro Direto' },
      { nome: 'CDB com liquidez diária', desc: '100–110% CDI com resgate a qualquer momento', tipo: 'Renda Fixa' },
      { nome: 'LCI / LCA', desc: 'Isento de IR, atrelado ao CDI, prazo mínimo de 90 dias', tipo: 'Renda Fixa' },
    ],
  },

  'RF Pós': {
    emoji: '📈',
    description:
      'Proteção contra a inflação. Títulos IPCA+ garantem poder de compra no longo prazo com rentabilidade real positiva.',
    qtdMin: 2,
    qtdMax: 4,
    isDynamic: false,
    produtos: [
      { nome: 'Tesouro IPCA+', desc: 'Proteção total contra inflação com juros reais garantidos', tipo: 'Tesouro Direto' },
      { nome: 'Debêntures IPCA+ incentivadas', desc: 'Isenção de IR, rendimento IPCA + spread privado', tipo: 'Renda Fixa' },
      { nome: 'CRI / CRA', desc: 'Isentos de IR, lastreados em recebíveis imobiliários ou do agronegócio', tipo: 'Renda Fixa' },
      { nome: 'LCI / LCA IPCA+', desc: 'Isento de IR com correção pela inflação', tipo: 'Renda Fixa' },
    ],
  },

  'Fundos multimercados': {
    emoji: '🌐',
    description:
      'Diversificação ativa. Gestores profissionais alocam em juros, câmbio e bolsa conforme o cenário macroeconômico.',
    qtdMin: 1,
    qtdMax: 2,
    isDynamic: false,
    produtos: [
      { nome: 'Fundo Macro', desc: 'Opera juros e câmbio com estratégia macroeconômica discrecional', tipo: 'Fundo' },
      { nome: 'Fundo Long & Short', desc: 'Posições compradas e vendidas em ações, baixa correlação com o Ibovespa', tipo: 'Fundo' },
      { nome: 'Fundo Multimercado Livre', desc: 'Ampla liberdade de alocação pelo gestor em múltiplas classes', tipo: 'Fundo' },
    ],
  },

  'Internacional': {
    emoji: '🌍',
    description:
      'Proteção cambial e diversificação global. Acesso a mercados internacionais e moedas fortes sem sair da B3.',
    qtdMin: 1,
    qtdMax: 2,
    isDynamic: false,
    produtos: [
      { nome: 'IVVB11', desc: 'ETF que replica o S&P 500 em reais via BDR — top 500 empresas dos EUA', tipo: 'ETF / BDR' },
      { nome: 'WRLD11', desc: 'ETF MSCI World — diversificação global em mercados desenvolvidos', tipo: 'ETF / BDR' },
      { nome: 'Fundo de Ações Internacional', desc: 'Fundo com exposição cambial e gestão ativa no exterior', tipo: 'Fundo' },
    ],
  },

  'Alternativos': {
    emoji: '🥇',
    description:
      'Hedge e descorrelação. Ativos com baixa correlação à bolsa brasileira, como ouro e commodities.',
    qtdMin: 1,
    qtdMax: 2,
    isDynamic: false,
    produtos: [
      { nome: 'GOLD11', desc: 'ETF de ouro físico negociado na B3 — proteção contra crises e inflação', tipo: 'ETF' },
      { nome: 'Fundo de Ouro', desc: 'Exposição ao preço do ouro em dólares com gestão passiva', tipo: 'Fundo' },
    ],
  },

  'Ações': {
    emoji: '📊',
    description:
      'Renda variável brasileira. Participação no crescimento de empresas listadas na B3 com potencial de valorização no longo prazo.',
    qtdMin: 5,
    qtdMax: 15,
    isDynamic: true,
    produtos: [],
  },

  'Fundos imobiliários': {
    emoji: '🏢',
    description:
      'FIIs distribuem rendimentos mensais isentos de IR para pessoa física. Exposição ao mercado imobiliário sem burocracia.',
    qtdMin: 5,
    qtdMax: 8,
    isDynamic: true,
    produtos: [],
  },
};
