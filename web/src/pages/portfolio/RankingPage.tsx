import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Button from '../../components/ui/Button';
import AppLayout from '../../components/layout/AppLayout';
import { rankingService } from '../../services/rankingService';
import type { RankingItem, SubStrategy } from '../../types/ranking';

const SUB_ESTRATEGIA_LABELS: Record<string, string> = {
  valor: 'Ações de Valor',
  dividendos: 'Ações — Dividendos',
  misto: 'Ações — Misto',
  misto_acoes: 'Ações — Misto',
  renda: 'FIIs — Renda',
  valorizacao: 'FIIs — Valorização',
  misto_fiis: 'FIIs — Misto',
};

const TABS = [
  { key: 'acoes', label: '📊 Ações' },
  { key: 'fiis', label: '🏢 FIIs' },
];

export default function RankingPage() {
  const navigate = useNavigate();
  const [tab, setTab] = useState<'acoes' | 'fiis'>('acoes');
  const [subStrategy, setSubStrategy] = useState<SubStrategy | null>(null);
  const [ranking, setRanking] = useState<RankingItem[]>([]);
  const [dataRanking, setDataRanking] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    loadSubStrategy();
  }, []);

  useEffect(() => {
    if (subStrategy) loadRanking();
  }, [subStrategy, tab]);

  const loadSubStrategy = async () => {
    try {
      const sub = await rankingService.getSubStrategy();
      setSubStrategy(sub);
    } catch {
      setError('Não foi possível carregar sua sub-estratégia.');
      setIsLoading(false);
    }
  };

  const loadRanking = async () => {
    if (!subStrategy) return;
    setIsLoading(true);
    setError('');
    try {
      const sub = tab === 'acoes' ? subStrategy.subEstrategiaAcoes : subStrategy.subEstrategiaFiis;
      const resp = await rankingService.getTop20(sub);
      setRanking(resp.itens);
      setDataRanking(resp.dataRanking);
    } catch {
      setError('Não há ranking disponível no momento. O batch ainda não rodou hoje.');
      setRanking([]);
    } finally {
      setIsLoading(false);
    }
  };

  const currentSub = subStrategy
    ? tab === 'acoes' ? subStrategy.subEstrategiaAcoes : subStrategy.subEstrategiaFiis
    : '';

  const dataFormatada = dataRanking
    ? new Date(dataRanking).toLocaleDateString('pt-BR')
    : '';

  return (
    <AppLayout
      title="Top 20"
      subtitle={currentSub ? `${SUB_ESTRATEGIA_LABELS[currentSub] || currentSub}${dataFormatada ? ` · ${dataFormatada}` : ''}` : undefined}
    >
      <div style={{ maxWidth: '760px' }}>

        {/* Tabs + mudar estratégia */}
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '16px', marginBottom: '24px', flexWrap: 'wrap' }}>
          {/* Pill tabs */}
          <div style={{
            display: 'flex',
            gap: '4px',
            background: 'var(--border)',
            borderRadius: '10px',
            padding: '4px',
          }}>
            {TABS.map((t) => (
              <button
                key={t.key}
                onClick={() => setTab(t.key as 'acoes' | 'fiis')}
                style={{
                  padding: '8px 20px',
                  border: 'none',
                  borderRadius: '8px',
                  fontSize: '14px',
                  fontWeight: 600,
                  cursor: 'pointer',
                  background: tab === t.key ? '#fff' : 'transparent',
                  color: tab === t.key ? 'var(--primary)' : 'var(--text-muted)',
                  boxShadow: tab === t.key ? 'var(--shadow-sm)' : 'none',
                  transition: 'all 0.15s',
                }}
              >
                {t.label}
              </button>
            ))}
          </div>

          <button
            onClick={() => navigate('/sub-strategy')}
            style={{
              background: 'var(--bg-card)',
              border: '1px solid var(--border)',
              borderRadius: 'var(--radius-sm)',
              padding: '7px 14px',
              fontSize: '13px',
              color: 'var(--text-heading)',
              cursor: 'pointer',
              fontWeight: 500,
              boxShadow: 'var(--shadow-sm)',
            }}
          >
            Mudar estratégia
          </button>
        </div>

        {/* Content */}
        {isLoading ? (
          <LoadingSkeleton />
        ) : error ? (
          <div style={{
            background: 'var(--bg-card)',
            borderRadius: 'var(--radius-lg)',
            padding: '48px',
            textAlign: 'center',
            boxShadow: 'var(--shadow-sm)',
            border: '1px solid var(--border)',
          }}>
            <div style={{ fontSize: '40px', marginBottom: '12px' }}>📭</div>
            <p style={{ color: 'var(--text-muted)', margin: 0 }}>{error}</p>
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
            {ranking.map((item) => (
              <RankingCard key={item.ticker} item={item} />
            ))}
          </div>
        )}

        {/* CTA */}
        {!isLoading && !error && ranking.length > 0 && (
          <div style={{ marginTop: '28px', display: 'flex', flexDirection: 'column', gap: '12px' }}>
            <Button onClick={() => navigate('/suggestion')}>
              Ver minha sugestão personalizada →
            </Button>
            <div style={{ textAlign: 'center' }}>
              <button
                onClick={() => navigate('/dashboard')}
                style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer', fontSize: '14px' }}
              >
                Voltar ao dashboard
              </button>
            </div>
          </div>
        )}
      </div>
    </AppLayout>
  );
}

function RankingCard({ item }: { item: RankingItem }) {
  const [expanded, setExpanded] = useState(false);
  const score = Math.round(item.scoreTotal * 10) / 10;
  const scoreColor = score >= 7 ? 'var(--success)' : score >= 5 ? 'var(--warning)' : 'var(--error)';
  const scoreBarColor = score >= 7 ? '#00B2A9' : score >= 5 ? '#FFCE20' : '#EE5D50';

  return (
    <div style={{
      background: 'var(--bg-card)',
      borderRadius: 'var(--radius-md)',
      padding: '16px 20px',
      boxShadow: 'var(--shadow-sm)',
      border: '1px solid var(--border)',
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
        {/* Posição — badge */}
        <div style={{
          width: '36px',
          height: '36px',
          borderRadius: 'var(--radius-sm)',
          background: item.posicao <= 3 ? '#FFF8E1' : 'var(--bg-page)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontWeight: 800,
          fontSize: '14px',
          color: item.posicao <= 3 ? '#F59E0B' : 'var(--text-muted)',
          flexShrink: 0,
          border: item.posicao <= 3 ? '1px solid #FDE68A' : '1px solid var(--border)',
        }}>
          {item.posicao}
        </div>

        {/* Ticker + Nome */}
        <div style={{ flex: 1, minWidth: 0 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', flexWrap: 'wrap' }}>
            <span style={{ fontWeight: 800, color: 'var(--text-heading)', fontSize: '16px', letterSpacing: '-0.01em' }}>
              {item.ticker}
            </span>
            {item.entrouHoje && (
              <span style={{
                background: 'var(--primary-light)',
                color: 'var(--primary)',
                fontSize: '10px',
                fontWeight: 700,
                padding: '2px 7px',
                borderRadius: 'var(--radius-sm)',
                textTransform: 'uppercase',
              }}>
                NOVO ↑
              </span>
            )}
          </div>
          <div style={{ color: 'var(--text-muted)', fontSize: '13px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', marginTop: '2px' }}>
            {item.nome}
          </div>
        </div>

        {/* Score */}
        <div style={{ textAlign: 'right', flexShrink: 0 }}>
          <div style={{ fontSize: '22px', fontWeight: 900, color: scoreColor, letterSpacing: '-0.02em' }}>{score}</div>
          <div style={{ fontSize: '10px', color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.5px' }}>score</div>
        </div>

        {item.justificativa && (
          <button
            onClick={() => setExpanded(!expanded)}
            style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-muted)', fontSize: '16px', padding: '4px', flexShrink: 0 }}
          >
            {expanded ? '▲' : '▼'}
          </button>
        )}
      </div>

      {/* Score progress bar */}
      <div style={{ marginTop: '12px', paddingTop: '12px', borderTop: '1px solid var(--border)' }}>
        <div style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
          <ScorePill label="Quant" value={item.scoreQuantitativo} />
          <ScorePill label="Qual" value={item.scoreQualitativo} />
          <div style={{ flex: 1, height: '6px', background: 'var(--border)', borderRadius: '3px', overflow: 'hidden' }}>
            <div style={{
              height: '100%',
              width: `${score * 10}%`,
              background: scoreBarColor,
              borderRadius: '3px',
              transition: 'width 0.6s ease',
            }} />
          </div>
        </div>
      </div>

      {expanded && item.justificativa && (
        <div style={{
          marginTop: '12px',
          padding: '12px',
          background: 'var(--bg-page)',
          borderRadius: 'var(--radius-sm)',
          fontSize: '13px',
          color: 'var(--text-body)',
          lineHeight: 1.6,
          border: '1px solid var(--border)',
        }}>
          {item.justificativa}
        </div>
      )}
    </div>
  );
}

function ScorePill({ label, value }: { label: string; value: number }) {
  const v = Math.round(value * 10) / 10;
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
      <span style={{ fontSize: '11px', color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.4px' }}>{label}:</span>
      <span style={{ fontSize: '13px', fontWeight: 700, color: 'var(--text-heading)' }}>{v}</span>
    </div>
  );
}

function LoadingSkeleton() {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
      {Array.from({ length: 5 }).map((_, i) => (
        <div key={i} style={{
          background: 'var(--bg-card)',
          borderRadius: 'var(--radius-md)',
          padding: '20px',
          boxShadow: 'var(--shadow-sm)',
          border: '1px solid var(--border)',
          opacity: 1 - i * 0.12,
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
            <div style={{ width: '36px', height: '36px', borderRadius: 'var(--radius-sm)', background: 'var(--border)' }} />
            <div style={{ flex: 1 }}>
              <div style={{ height: '14px', background: 'var(--border)', borderRadius: '4px', marginBottom: '6px', width: '60%' }} />
              <div style={{ height: '12px', background: 'var(--border)', borderRadius: '4px', width: '40%' }} />
            </div>
            <div style={{ width: '40px', height: '40px', background: 'var(--border)', borderRadius: 'var(--radius-sm)' }} />
          </div>
        </div>
      ))}
    </div>
  );
}
