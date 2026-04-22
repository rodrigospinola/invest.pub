import { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { profileService } from '../services/profileService';
import { rankingService } from '../services/rankingService';
import { useAuth } from './AuthContext';

export type Phase = 'onboarding' | 'sub_strategy' | 'portfolio_import' | 'monitoring' | null;

interface PhaseContextValue {
  phase: Phase;
  subStrategyAcoes: string | null;
  subStrategyFiis: string | null;
  isPhaseLoading: boolean;
  updatePhase: (phase: Phase, acoes?: string | null, fiis?: string | null) => void;
  refreshPhase: () => Promise<void>;
}

const PhaseContext = createContext<PhaseContextValue>({
  phase: null,
  subStrategyAcoes: null,
  subStrategyFiis: null,
  isPhaseLoading: true,
  updatePhase: () => {},
  refreshPhase: async () => {},
});

export function PhaseProvider({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const [phase, setPhase] = useState<Phase>(null);
  const [subStrategyAcoes, setSubStrategyAcoes] = useState<string | null>(null);
  const [subStrategyFiis, setSubStrategyFiis] = useState<string | null>(null);
  const [isPhaseLoading, setIsPhaseLoading] = useState(true);

  const updatePhase = (newPhase: Phase, acoes?: string | null, fiis?: string | null) => {
    setPhase(newPhase);
    if (acoes !== undefined) setSubStrategyAcoes(acoes ?? null);
    if (fiis !== undefined) setSubStrategyFiis(fiis ?? null);
  };

  /** Busca o estado real do backend e determina a phase correta */
  const refreshPhase = useCallback(async () => {
    setIsPhaseLoading(true);
    try {
      // 1. Tenta buscar o perfil (só precisa verificar se existe)
      try {
        await profileService.getProfile();
      } catch {
        setPhase('onboarding');
        setSubStrategyAcoes(null);
        setSubStrategyFiis(null);
        return;
      }

      // 2. Tenta buscar sub-estratégia
      let subStrategy;
      try {
        subStrategy = await rankingService.getSubStrategy();
      } catch {
        setPhase('sub_strategy');
        setSubStrategyAcoes(null);
        setSubStrategyFiis(null);
        return;
      }

      // 3. Tenta buscar ativos da carteira
      let assets;
      try {
        assets = await rankingService.getAssets();
      } catch {
        assets = [];
      }

      const resolved: Phase = assets.length > 0 ? 'monitoring' : 'portfolio_import';
      setPhase(resolved);
      setSubStrategyAcoes(subStrategy.subEstrategiaAcoes ?? null);
      setSubStrategyFiis(subStrategy.subEstrategiaFiis ?? null);
    } catch {
      // fallback seguro
      setPhase('onboarding');
      setSubStrategyAcoes(null);
      setSubStrategyFiis(null);
    } finally {
      setIsPhaseLoading(false);
    }
  }, []);

  // Carrega a phase automaticamente quando o usuário está autenticado
  useEffect(() => {
    if (authLoading) return;
    if (!isAuthenticated) {
      setPhase(null);
      setSubStrategyAcoes(null);
      setSubStrategyFiis(null);
      setIsPhaseLoading(false);
      return;
    }
    refreshPhase();
  }, [isAuthenticated, authLoading, refreshPhase]);

  return (
    <PhaseContext.Provider value={{ phase, subStrategyAcoes, subStrategyFiis, isPhaseLoading, updatePhase, refreshPhase }}>
      {children}
    </PhaseContext.Provider>
  );
}

export const usePhase = () => useContext(PhaseContext);
