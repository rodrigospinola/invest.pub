import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { usePhase } from '../contexts/PhaseContext';
import Button from '../components/ui/Button';
import AppLayout from '../components/layout/AppLayout';
import StatCard from '../components/ui/StatCard';
import { profileService } from '../services/profileService';
import { rankingService } from '../services/rankingService';
import { dashboardService } from '../services/dashboardService';
import type { DashboardData, HistoryPoint } from '../services/dashboardService';
import AllocationComparison from '../components/dashboard/AllocationComparison';
import AlertList from '../components/dashboard/AlertList';
import HistoryChart from '../components/dashboard/HistoryChart';
import type { Profile } from '../types/profile';
import type { SubStrategy, AssetItem } from '../types/ranking';

type Phase = 'onboarding' | 'sub_strategy' | 'portfolio_import' | 'monitoring';

interface DashboardState {
  phase: Phase;
  profile: Profile | null;
  subStrategy: SubStrategy | null;
  assets: AssetItem[];
  stats: DashboardData | null;
  history: HistoryPoint[];
}

const PERFIL_LABELS: Record<string, string> = {
  conservador: 'Conservador',
  moderado: 'Moderado',
  arrojado: 'Arrojado',
};

const FAIXA_LABELS: Record<string, string> = {
  ate_10k: 'Até R$10k',
  '10k_100k': 'R$10k–R$100k',
  acima_100k: 'Acima de R$100k',
};

const SUB_LABELS: Record<string, string> = {
  valor: 'Valor',
  dividendos: 'Dividendos',
  misto: 'Misto',
  renda: 'Renda',
  valorizacao: 'Valorização',
};

export default function DashboardPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const { refreshPhase } = usePhase();
  const [state, setState] = useState<DashboardState>({
    phase: 'onboarding',
    profile: null,
    subStrategy: null,
    assets: [],
    stats: null,
    history: [],
  });
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    loadDashboard();
  }, []);

  const loadDashboard = async () => {
    try {
      let profile: Profile | null = null;
      try {
        profile = await profileService.getProfile();
      } catch {
        setState({ phase: 'onboarding', profile: null, subStrategy: null, assets: [], stats: null, history: [] });
        setIsLoading(false);
        return;
      }

      let subStrategy: SubStrategy | null = null;
      try {
        subStrategy = await rankingService.getSubStrategy();
      } catch {
        setState({ phase: 'sub_strategy', profile, subStrategy: null, assets: [], stats: null, history: [] });
        setIsLoading(false);
        return;
      }

      let assets: AssetItem[] = [];
      try {
        assets = await rankingService.getAssets();
      } catch {
        assets = [];
      }

      let stats: DashboardData | null = null;
      let history: HistoryPoint[] = [];
      try {
        stats = await dashboardService.getDashboard();
        const historyData = await dashboardService.getHistory(30);
        history = historyData.pontos;
      } catch (err) {
        console.error('Falha ao carregar estatísticas', err);
      }

      const phase: Phase = assets.length > 0 ? 'monitoring' : 'portfolio_import';
      refreshPhase();
      setState({ phase, profile, subStrategy, assets, stats, history });
    } catch {
      setState({ phase: 'onboarding', profile: null, subStrategy: null, assets: [], stats: null, history: [] });
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading) return <LoadingDashboard />;

  const { phase, profile, subStrategy, assets } = state;

  const totalValor = assets.reduce((acc, a) => acc + a.quantidade * a.precoMedio, 0);
  const progressoMeta = totalValor > 0 ? Math.min((totalValor / 500000) * 100, 100) : 0;

  const greeting = `Olá, ${user?.nome?.split(' ')[0] || 'investidor'}!`;

  return (
    <AppLayout title="Dashboard" subtitle={greeting}>
      <div style={{ maxWidth: '900px' }}>

        {/* Fase 1: Onboarding pendente */}
        {phase === 'onboarding' && (
          <StepCard
            icon="🎯"
            title="Complete seu perfil de investidor"
            desc="Responda algumas perguntas sobre seus objetivos e patrimônio para receber sua alocação personalizada."
            cta="Iniciar onboarding"
            onClick={() => navigate('/onboarding')}
            step={1}
            total={4}
          />
        )}

        {/* Fase 2a: Sub-estratégia pendente */}
        {phase === 'sub_strategy' && profile && (
          <>
            <ProfileCard profile={profile} />
            <StepCard
              icon="🎯"
              title="Escolha sua estratégia de investimento"
              desc="Defina como quer investir em ações e FIIs para ver o ranking dos melhores ativos."
              cta="Escolher estratégia"
              onClick={() => navigate('/sub-strategy')}
              step={2}
              total={4}
            />
          </>
        )}

        {/* Fase 2b: Carteira pendente */}
        {phase === 'portfolio_import' && profile && subStrategy && (
          <>
            <ProfileCard profile={profile} />
            <StrategyCard subStrategy={subStrategy} onViewRanking={() => navigate('/ranking')} />
            <StepCard
              icon="📥"
              title="Importe sua carteira"
              desc="Já comprou os ativos? Registre sua carteira para monitoramento e rastreio de aderência."
              cta="Ver sugestão e importar"
              onClick={() => navigate('/suggestion')}
              step={3}
              total={4}
              secondary={{ label: 'Ir para ranking', onClick: () => navigate('/ranking') }}
            />
          </>
        )}

        {/* Fase 3: Monitoramento ativo */}
        {phase === 'monitoring' && profile && subStrategy && (
          <>
            {/* Alertas */}
            <AlertList alerts={state.stats?.alertasRecentes || []} onRefresh={loadDashboard} />

            {/* Stat cards */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '16px', marginBottom: '24px' }}>
              <StatCard
                label="Patrimônio investido"
                value={new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(state.stats?.valorTotal || totalValor)}
                delta={state.stats?.rentabilidadeNoDia}
                deltaLabel="hoje"
                icon="💰"
              />
              <StatCard
                label="Rentabilidade total"
                value={`${(state.stats?.rentabilidadeAcumulada || 0).toFixed(2)}%`}
                icon="📈"
                color={((state.stats?.rentabilidadeAcumulada || 0) >= 0) ? 'var(--success)' : 'var(--error)'}
              />
              <StatCard
                label="Meta R$500k"
                value={`${(state.stats?.percentualMeta || progressoMeta).toFixed(1)}%`}
                icon="🎯"
                color="var(--primary)"
              />
            </div>

            {/* Progresso meta */}
            <div style={{
              background: 'var(--bg-card)',
              borderRadius: 'var(--radius-lg)',
              boxShadow: 'var(--shadow-sm)',
              border: '1px solid var(--border)',
              padding: '20px 24px',
              marginBottom: '20px',
            }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '10px' }}>
                <span style={{ fontSize: '13px', color: 'var(--text-muted)', fontWeight: 500 }}>Progresso rumo a R$500k</span>
                <span style={{ fontSize: '13px', fontWeight: 700, color: 'var(--primary)' }}>
                  {(state.stats?.percentualMeta || progressoMeta).toFixed(1)}%
                </span>
              </div>
              <div style={{ height: '10px', background: 'var(--border)', borderRadius: '5px', overflow: 'hidden' }}>
                <div style={{
                  height: '100%',
                  width: `${state.stats?.percentualMeta || progressoMeta}%`,
                  background: 'linear-gradient(90deg, #00B2A9, #009E96)',
                  borderRadius: '5px',
                  transition: 'width 0.8s ease',
                }} />
              </div>
            </div>

            <HistoryChart points={state.history} />
            <AllocationComparison deviations={state.stats?.alocacoes || []} />

            <ProfileCard profile={profile} compact />
            <StrategyCard subStrategy={subStrategy} onViewRanking={() => navigate('/ranking')} compact />

            {/* Ações rápidas */}
            <h3 style={{ fontSize: '15px', fontWeight: 700, color: 'var(--text-heading)', margin: '8px 0 12px' }}>
              Ações rápidas
            </h3>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <QuickAction icon="📊" label="Ver ranking" onClick={() => navigate('/ranking')} />
              <QuickAction icon="✨" label="Sugestão" onClick={() => navigate('/suggestion')} />
              <QuickAction icon="📥" label="Adicionar ativos" onClick={() => navigate('/portfolio/import')} />
              <QuickAction icon="🔄" label="Atualizar estratégia" onClick={() => navigate('/sub-strategy')} />
            </div>
          </>
        )}
      </div>
    </AppLayout>
  );
}

function StepCard({ icon, title, desc, cta, onClick, step, total, secondary }: {
  icon: string;
  title: string;
  desc: string;
  cta: string;
  onClick: () => void;
  step: number;
  total: number;
  secondary?: { label: string; onClick: () => void };
}) {
  return (
    <div style={{
      background: 'var(--bg-card)',
      borderRadius: 'var(--radius-lg)',
      padding: '28px',
      boxShadow: 'var(--shadow-sm)',
      border: '1px solid var(--border)',
      marginBottom: '20px',
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginBottom: '16px' }}>
        {Array.from({ length: total }).map((_, i) => (
          <div key={i} style={{
            height: '4px',
            flex: 1,
            borderRadius: '2px',
            background: i < step ? 'var(--primary)' : 'var(--border)',
          }} />
        ))}
        <span style={{ fontSize: '11px', color: 'var(--text-muted)', marginLeft: '4px', flexShrink: 0 }}>
          {step}/{total}
        </span>
      </div>
      <div style={{ fontSize: '40px', marginBottom: '12px' }}>{icon}</div>
      <h2 style={{ fontSize: '18px', fontWeight: 700, color: 'var(--text-heading)', margin: '0 0 8px' }}>{title}</h2>
      <p style={{ color: 'var(--text-muted)', fontSize: '14px', margin: '0 0 24px', lineHeight: 1.6 }}>{desc}</p>
      <Button onClick={onClick}>{cta} →</Button>
      {secondary && (
        <div style={{ textAlign: 'center', marginTop: '12px' }}>
          <button
            onClick={secondary.onClick}
            style={{ background: 'none', border: 'none', color: 'var(--primary)', cursor: 'pointer', fontSize: '13px', fontWeight: 500 }}
          >
            {secondary.label}
          </button>
        </div>
      )}
    </div>
  );
}

function ProfileCard({ profile, compact }: { profile: Profile; compact?: boolean }) {
  if (compact) {
    return (
      <div style={{
        background: 'var(--bg-card)',
        borderRadius: 'var(--radius-md)',
        padding: '14px 18px',
        boxShadow: 'var(--shadow-sm)',
        border: '1px solid var(--border)',
        marginBottom: '12px',
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
      }}>
        <span style={{ fontSize: '13px', color: 'var(--text-muted)' }}>
          Perfil: <strong style={{ color: 'var(--primary)' }}>{PERFIL_LABELS[profile.perfil]}</strong>
        </span>
        <span style={{ fontSize: '13px', color: 'var(--text-muted)' }}>{FAIXA_LABELS[profile.faixa]}</span>
      </div>
    );
  }
  return (
    <div style={{
      background: 'var(--bg-card)',
      borderRadius: 'var(--radius-lg)',
      padding: '20px 24px',
      boxShadow: 'var(--shadow-sm)',
      border: '1px solid var(--border)',
      marginBottom: '16px',
    }}>
      <h3 style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-muted)', margin: '0 0 12px', textTransform: 'uppercase', letterSpacing: '0.6px' }}>
        Seu perfil
      </h3>
      <div style={{ display: 'flex', gap: '24px', flexWrap: 'wrap' }}>
        <div>
          <div style={{ fontSize: '11px', color: 'var(--text-muted)', marginBottom: '2px' }}>Perfil</div>
          <div style={{ fontWeight: 700, color: 'var(--primary)', fontSize: '15px' }}>{PERFIL_LABELS[profile.perfil]}</div>
        </div>
        <div>
          <div style={{ fontSize: '11px', color: 'var(--text-muted)', marginBottom: '2px' }}>Faixa</div>
          <div style={{ fontWeight: 600, color: 'var(--text-heading)' }}>{FAIXA_LABELS[profile.faixa]}</div>
        </div>
        <div>
          <div style={{ fontSize: '11px', color: 'var(--text-muted)', marginBottom: '2px' }}>Patrimônio</div>
          <div style={{ fontWeight: 600, color: 'var(--text-heading)' }}>
            {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(profile.valorTotal)}
          </div>
        </div>
      </div>
    </div>
  );
}

function StrategyCard({ subStrategy, onViewRanking, compact }: { subStrategy: SubStrategy; onViewRanking: () => void; compact?: boolean }) {
  if (compact) {
    return (
      <div style={{
        background: 'var(--bg-card)',
        borderRadius: 'var(--radius-md)',
        padding: '14px 18px',
        boxShadow: 'var(--shadow-sm)',
        border: '1px solid var(--border)',
        marginBottom: '12px',
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
      }}>
        <span style={{ fontSize: '13px', color: 'var(--text-muted)' }}>
          Ações: <strong style={{ color: 'var(--text-heading)' }}>{SUB_LABELS[subStrategy.subEstrategiaAcoes] || subStrategy.subEstrategiaAcoes}</strong>
          {' · '}
          FIIs: <strong style={{ color: 'var(--text-heading)' }}>{SUB_LABELS[subStrategy.subEstrategiaFiis] || subStrategy.subEstrategiaFiis}</strong>
        </span>
        <button
          onClick={onViewRanking}
          style={{ background: 'none', border: 'none', color: 'var(--primary)', cursor: 'pointer', fontSize: '13px', fontWeight: 500 }}
        >
          Ranking →
        </button>
      </div>
    );
  }
  return (
    <div style={{
      background: 'var(--bg-card)',
      borderRadius: 'var(--radius-lg)',
      padding: '20px 24px',
      boxShadow: 'var(--shadow-sm)',
      border: '1px solid var(--border)',
      marginBottom: '16px',
    }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
        <h3 style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-muted)', margin: 0, textTransform: 'uppercase', letterSpacing: '0.6px' }}>
          Estratégia
        </h3>
        <button
          onClick={onViewRanking}
          style={{ background: 'none', border: 'none', color: 'var(--primary)', cursor: 'pointer', fontSize: '13px', fontWeight: 500 }}
        >
          Ver ranking →
        </button>
      </div>
      <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
        <span style={{ background: 'var(--primary-light)', color: 'var(--primary)', fontSize: '12px', fontWeight: 600, padding: '4px 12px', borderRadius: 'var(--radius-sm)' }}>
          Ações: {SUB_LABELS[subStrategy.subEstrategiaAcoes] || subStrategy.subEstrategiaAcoes}
        </span>
        <span style={{ background: '#EEF2FF', color: 'var(--info)', fontSize: '12px', fontWeight: 600, padding: '4px 12px', borderRadius: 'var(--radius-sm)' }}>
          FIIs: {SUB_LABELS[subStrategy.subEstrategiaFiis] || subStrategy.subEstrategiaFiis}
        </span>
      </div>
    </div>
  );
}

function QuickAction({ icon, label, onClick }: { icon: string; label: string; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      style={{
        background: 'var(--bg-card)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-md)',
        padding: '18px',
        cursor: 'pointer',
        display: 'flex',
        alignItems: 'center',
        gap: '12px',
        fontWeight: 600,
        color: 'var(--text-heading)',
        fontSize: '14px',
        transition: 'box-shadow 0.15s, border-color 0.15s',
        boxShadow: 'var(--shadow-sm)',
      }}
      onMouseEnter={e => {
        (e.currentTarget as HTMLElement).style.borderColor = 'var(--primary)';
        (e.currentTarget as HTMLElement).style.boxShadow = 'var(--shadow-md)';
      }}
      onMouseLeave={e => {
        (e.currentTarget as HTMLElement).style.borderColor = 'var(--border)';
        (e.currentTarget as HTMLElement).style.boxShadow = 'var(--shadow-sm)';
      }}
    >
      <span style={{ fontSize: '22px' }}>{icon}</span>
      {label}
    </button>
  );
}

function LoadingDashboard() {
  return (
    <div style={{ minHeight: '100vh', background: 'var(--bg-page)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
      <div style={{ textAlign: 'center' }}>
        <div style={{ fontSize: '40px', marginBottom: '12px' }}>⏳</div>
        <p style={{ color: 'var(--text-muted)' }}>Carregando dashboard...</p>
      </div>
    </div>
  );
}
