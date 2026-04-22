import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import AppLayout from '../../components/layout/AppLayout';
import DonutChart, { CLASS_COLORS } from '../../components/charts/DonutChart';
import Button from '../../components/ui/Button';
import { profileService } from '../../services/profileService';
import { rankingService } from '../../services/rankingService';
import { usePhase } from '../../contexts/PhaseContext';
import { CLASS_INFO, type ClassInfo } from '../../utils/allocationInfo';
import type { Profile } from '../../types/profile';
import type { SubStrategy, SuggestionResponse, SuggestionAsset } from '../../types/ranking';

// ── Constants ───────────────────────────────────────────────────────────────
const BRL = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 });

const ACOES_OPTIONS = [
  { value: 'valor',      label: 'Valor',      icon: '📊', desc: 'Empresas com P/L atrativo e fundamentos sólidos' },
  { value: 'dividendos', label: 'Dividendos', icon: '💰', desc: 'Alto dividend yield e histórico consistente' },
  { value: 'misto',      label: 'Misto',      icon: '⚖️', desc: 'Equilíbrio entre crescimento e renda passiva' },
];
const FIIS_OPTIONS = [
  { value: 'renda',       label: 'Renda',       icon: '🏢', desc: 'Alto yield mensal e distribuições consistentes' },
  { value: 'valorizacao', label: 'Valorização', icon: '📈', desc: 'Cotas negociadas com desconto ao P/VP' },
  { value: 'misto',       label: 'Misto',       icon: '🔀', desc: 'Equilíbrio entre renda e valorização patrimonial' },
];

// ── Page ────────────────────────────────────────────────────────────────────
export default function StrategyPage() {
  const navigate = useNavigate();
  const { refreshPhase } = usePhase();

  const [profile, setProfile]         = useState<Profile | null>(null);
  const [subStrategy, setSubStrategy] = useState<SubStrategy | null>(null);
  const [suggestion, setSuggestion]   = useState<SuggestionResponse | null>(null);
  const [isLoading, setIsLoading]     = useState(true);

  // Pending (local unsaved) selections
  const [pendingAcoes, setPendingAcoes] = useState<string | null>(null);
  const [pendingFiis, setPendingFiis]   = useState<string | null>(null);
  const [isSaving, setIsSaving]         = useState(false);
  const [saveError, setSaveError]       = useState('');

  useEffect(() => { loadData(); }, []);

  const loadData = async () => {
    try {
      const [p, ss] = await Promise.all([
        profileService.getProfile(),
        rankingService.getSubStrategy().catch(() => null),
      ]);
      setProfile(p);
      setSubStrategy(ss);
      if (ss) {
        const acoes = ss.subEstrategiaAcoes?.replace('_acoes', '') ?? null;
        const fiis  = ss.subEstrategiaFiis?.replace('_fiis', '') ?? null;
        setPendingAcoes(acoes);
        setPendingFiis(fiis);
        try { setSuggestion(await rankingService.getSuggestion()); } catch { /* ranking not generated yet */ }
      }
    } catch {
      navigate('/dashboard');
    } finally {
      setIsLoading(false);
    }
  };

  // Saved (committed) values
  const savedAcoes = subStrategy?.subEstrategiaAcoes?.replace('_acoes', '') ?? null;
  const savedFiis  = subStrategy?.subEstrategiaFiis?.replace('_fiis', '') ?? null;

  // Dirty = pending differs from saved
  const isDirty = pendingAcoes !== savedAcoes || pendingFiis !== savedFiis;
  const canSave = !!pendingAcoes && !!pendingFiis;

  const handleSave = async () => {
    if (!pendingAcoes || !pendingFiis) return;
    setIsSaving(true);
    setSaveError('');
    const isFirstSave = !savedAcoes;
    try {
      const ss = await rankingService.createSubStrategy(pendingAcoes, pendingFiis);
      setSubStrategy(ss);
      refreshPhase();
      if (isFirstSave) {
        navigate('/portfolio/import');
        return;
      }
      // Editing existing strategy: stay on page and refresh suggestion
      try { setSuggestion(await rankingService.getSuggestion()); } catch { setSuggestion(null); }
    } catch {
      setSaveError('Erro ao salvar estratégia. Tente novamente.');
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <AppLayout title="Minha Estratégia">
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '300px' }}>
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontSize: '40px', marginBottom: '12px' }}>⏳</div>
            <p style={{ color: 'var(--text-muted)' }}>Carregando estratégia...</p>
          </div>
        </div>
      </AppLayout>
    );
  }

  if (!profile) return null;

  return (
    <AppLayout
      title="Minha Estratégia"
      subtitle="Alocação ideal, ativos recomendados e sua estratégia de investimento"
    >
      <div style={{ maxWidth: '820px' }}>

        {/* ── Hero: donut + allocation list ───────────────────────────────── */}
        <div style={{
          background: 'linear-gradient(135deg, #1B2559 0%, #2B3674 100%)',
          borderRadius: 'var(--radius-lg)',
          padding: '28px 32px',
          marginBottom: '28px',
          boxShadow: '0 8px 32px rgba(27,37,89,0.2)',
        }}>
          <div style={{ display: 'flex', gap: '32px', flexWrap: 'wrap', alignItems: 'center' }}>
            <div style={{ flexShrink: 0 }}>
              <DonutChart data={profile.alocacaoAlvo} valorTotal={profile.valorTotal} size={164} darkBg noLegend />
            </div>
            <div style={{ flex: 1, minWidth: '220px' }}>
              <div style={{ fontSize: '10px', fontWeight: 700, letterSpacing: '0.8px', color: 'rgba(163,174,208,0.9)', marginBottom: '14px', textTransform: 'uppercase' }}>
                Distribuição da carteira
              </div>
              {profile.alocacaoAlvo.map((item) => (
                <div key={item.classe} style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '9px' }}>
                  <div style={{ width: '10px', height: '10px', borderRadius: '3px', background: CLASS_COLORS[item.classe] ?? '#9ca3af', flexShrink: 0 }} />
                  <span style={{ fontSize: '13px', color: 'rgba(255,255,255,0.9)', flex: 1 }}>{item.classe}</span>
                  <span style={{ fontSize: '13px', fontWeight: 700, color: '#fff', marginRight: '8px' }}>{item.percentual}%</span>
                  <span style={{ fontSize: '12px', color: 'rgba(163,174,208,0.9)', minWidth: '72px', textAlign: 'right' }}>
                    {BRL.format((item.percentual / 100) * profile.valorTotal)}
                  </span>
                </div>
              ))}
              <div style={{ borderTop: '1px solid rgba(255,255,255,0.1)', marginTop: '12px', paddingTop: '10px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ fontSize: '12px', fontWeight: 600, color: 'rgba(163,174,208,0.7)' }}>Total investido</span>
                <span style={{ fontSize: '15px', fontWeight: 800, color: '#00B2A9' }}>{BRL.format(profile.valorTotal)}</span>
              </div>
            </div>
          </div>
        </div>

        {/* ── Class cards ─────────────────────────────────────────────────── */}
        {profile.alocacaoAlvo.map((item) => {
          const info  = CLASS_INFO[item.classe];
          const valor = (item.percentual / 100) * profile.valorTotal;
          const color = CLASS_COLORS[item.classe] ?? '#9ca3af';
          const isAcoes = item.classe === 'Ações';
          const isFiis  = item.classe === 'Fundos imobiliários';

          return (
            <ClassCard
              key={item.classe}
              classe={item.classe}
              percentual={item.percentual}
              valor={valor}
              color={color}
              info={info}
              isAcoes={isAcoes}
              isFiis={isFiis}
              // For inline selection
              pendingAcoes={pendingAcoes}
              pendingFiis={pendingFiis}
              savedAcoes={savedAcoes}
              savedFiis={savedFiis}
              onSelectAcoes={setPendingAcoes}
              onSelectFiis={setPendingFiis}
              suggestion={suggestion}
              onViewSuggestion={() => navigate('/suggestion')}
              onViewRanking={() => navigate('/ranking')}
            />
          );
        })}

        {/* ── Save bar (sticky bottom) ─────────────────────────────────── */}
        {(isDirty || !savedAcoes) && canSave && (
          <div style={{
            position: 'sticky', bottom: '24px',
            background: 'var(--bg-card)',
            border: '1px solid var(--border)',
            borderRadius: 'var(--radius-lg)',
            padding: '16px 24px',
            boxShadow: '0 8px 32px rgba(27,37,89,0.15)',
            display: 'flex', alignItems: 'center', gap: '16px', flexWrap: 'wrap',
            marginTop: '12px',
          }}>
            <div style={{ flex: 1, minWidth: '200px' }}>
              <div style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-heading)' }}>
                {savedAcoes ? 'Alterar estratégia' : 'Confirmar estratégia'}
              </div>
              <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                Ações: <strong>{pendingAcoes}</strong> · FIIs: <strong>{pendingFiis}</strong>
              </div>
            </div>
            {saveError && (
              <span style={{ fontSize: '12px', color: 'var(--error)' }}>{saveError}</span>
            )}
            <Button onClick={handleSave} isLoading={isSaving}>
              {savedAcoes ? 'Salvar alterações' : 'Salvar estratégia'} →
            </Button>
          </div>
        )}

        {/* ── Continuar ───────────────────────────────────────────────── */}
        {savedAcoes && !isDirty && (
          <div style={{ marginTop: '16px' }}>
            <Button onClick={() => navigate('/portfolio/import')}>
              Continuar →
            </Button>
          </div>
        )}
      </div>
    </AppLayout>
  );
}

// ── ClassCard ───────────────────────────────────────────────────────────────
function ClassCard({
  classe, percentual, valor, color, info,
  isAcoes, isFiis,
  pendingAcoes, pendingFiis, savedAcoes, savedFiis,
  onSelectAcoes, onSelectFiis,
  suggestion, onViewSuggestion, onViewRanking,
}: {
  classe: string; percentual: number; valor: number; color: string;
  info: ClassInfo | undefined;
  isAcoes: boolean; isFiis: boolean;
  pendingAcoes: string | null; pendingFiis: string | null;
  savedAcoes: string | null; savedFiis: string | null;
  onSelectAcoes: (v: string) => void; onSelectFiis: (v: string) => void;
  suggestion: SuggestionResponse | null;
  onViewSuggestion: () => void; onViewRanking: () => void;
}) {
  const [expanded, setExpanded] = useState(true);
  const isDynamic = info?.isDynamic ?? false;

  const pendingSel = isAcoes ? pendingAcoes : (isFiis ? pendingFiis : null);
  const savedSel   = isAcoes ? savedAcoes   : (isFiis ? savedFiis   : null);
  const options    = isAcoes ? ACOES_OPTIONS : (isFiis ? FIIS_OPTIONS : []);
  const sugItems: SuggestionAsset[] = isAcoes
    ? (suggestion?.acoesRec ?? [])
    : (isFiis ? (suggestion?.fiisRec ?? []) : []);

  const isUnsaved = isDynamic && pendingSel !== savedSel;

  return (
    <div style={{
      background: 'var(--bg-card)',
      borderRadius: 'var(--radius-lg)',
      border: `1px solid ${isUnsaved ? `${color}50` : 'var(--border)'}`,
      boxShadow: isUnsaved ? `0 0 0 2px ${color}20` : 'var(--shadow-sm)',
      marginBottom: '16px',
      overflow: 'hidden',
      transition: 'border-color 0.2s, box-shadow 0.2s',
    }}>
      {/* Header */}
      <button
        onClick={() => setExpanded(!expanded)}
        style={{
          width: '100%', background: 'transparent', border: 'none', cursor: 'pointer',
          display: 'flex', alignItems: 'center', gap: '14px',
          padding: '18px 24px', textAlign: 'left',
          borderBottom: expanded ? '1px solid var(--border)' : 'none',
          transition: 'background 0.15s',
        }}
        onMouseEnter={e => { (e.currentTarget as HTMLElement).style.background = 'var(--bg-page)'; }}
        onMouseLeave={e => { (e.currentTarget as HTMLElement).style.background = 'transparent'; }}
      >
        <div style={{ width: '14px', height: '14px', borderRadius: '4px', background: color, flexShrink: 0 }} />

        <div style={{ flex: 1, minWidth: 0 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', flexWrap: 'wrap' }}>
            <span style={{ fontSize: '15px', fontWeight: 700, color: 'var(--text-heading)' }}>{classe}</span>
            {info && (
              <span style={{ fontSize: '10px', background: `${color}1A`, color, padding: '2px 8px', borderRadius: '4px', fontWeight: 600 }}>
                {info.emoji} {isDynamic ? `${info.qtdMin}–${info.qtdMax} ativos` : `${info.qtdMin}–${info.qtdMax} produtos`}
              </span>
            )}
            {isDynamic && savedSel && (
              <span style={{ fontSize: '10px', background: 'rgba(0,178,169,0.1)', color: 'var(--primary)', padding: '2px 8px', borderRadius: '4px', fontWeight: 600 }}>
                ✓ {savedSel.charAt(0).toUpperCase() + savedSel.slice(1)}
              </span>
            )}
            {isUnsaved && (
              <span style={{ fontSize: '10px', background: `${color}15`, color, padding: '2px 8px', borderRadius: '4px', fontWeight: 600 }}>
                ● não salvo
              </span>
            )}
          </div>
          {info && (
            <div style={{ fontSize: '12px', color: 'var(--text-body)', marginTop: '2px' }}>{info.description}</div>
          )}
        </div>

        <div style={{ textAlign: 'right', flexShrink: 0, marginRight: '8px' }}>
          <div style={{ fontSize: '18px', fontWeight: 800, color, letterSpacing: '-0.03em' }}>{percentual}%</div>
          <div style={{ fontSize: '12px', color: 'var(--text-muted)', fontWeight: 600 }}>{BRL.format(valor)}</div>
        </div>

        <span style={{ color: 'var(--text-muted)', fontSize: '12px', flexShrink: 0 }}>
          {expanded ? '▲' : '▼'}
        </span>
      </button>

      {/* Body */}
      {expanded && (
        <div style={{ padding: '20px 24px' }}>

          {/* ── FIXED CLASSES ──────────────────────────────────────────── */}
          {!isDynamic && info && info.produtos.length > 0 && (
            <>
              <div style={{ fontSize: '11px', fontWeight: 700, color: 'var(--text-muted)', letterSpacing: '0.5px', textTransform: 'uppercase', marginBottom: '12px' }}>
                Produtos recomendados
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(210px, 1fr))', gap: '10px' }}>
                {info.produtos.map((prod) => (
                  <div key={prod.nome} style={{
                    background: 'var(--bg-page)', border: '1px solid var(--border)',
                    borderRadius: 'var(--radius-md)', padding: '14px',
                    borderLeft: `3px solid ${color}`,
                  }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '5px', gap: '6px' }}>
                      <span style={{ fontSize: '13px', fontWeight: 700, color: 'var(--text-heading)' }}>{prod.nome}</span>
                      <span style={{ fontSize: '10px', fontWeight: 600, color, background: `${color}15`, padding: '2px 7px', borderRadius: '4px', flexShrink: 0 }}>
                        {prod.tipo}
                      </span>
                    </div>
                    <div style={{ fontSize: '12px', color: 'var(--text-muted)', lineHeight: 1.5 }}>{prod.desc}</div>
                  </div>
                ))}
              </div>
              <div style={{ marginTop: '14px', padding: '10px 14px', background: '#FFFBEB', border: '1px solid #FDE68A', borderRadius: 'var(--radius-sm)', fontSize: '12px', color: '#92400E', display: 'flex', gap: '8px' }}>
                <span>⚠️</span>
                <span>Sugestão educacional. Consulte um assessor de investimentos antes de aplicar.</span>
              </div>
            </>
          )}

          {/* ── DYNAMIC CLASSES ────────────────────────────────────────── */}
          {isDynamic && (
            <>
              {/* Strategy selector (inline) */}
              <div style={{ marginBottom: '16px' }}>
                <div style={{ fontSize: '11px', fontWeight: 700, color: 'var(--text-muted)', letterSpacing: '0.5px', textTransform: 'uppercase', marginBottom: '10px' }}>
                  {isAcoes ? 'Escolha a estratégia de ações' : 'Escolha a estratégia de FIIs'}
                </div>
                <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                  {options.map((opt) => {
                    const isSelected = pendingSel === opt.value;
                    return (
                      <button
                        key={opt.value}
                        onClick={() => isAcoes ? onSelectAcoes(opt.value) : onSelectFiis(opt.value)}
                        style={{
                          display: 'flex', alignItems: 'center', gap: '8px',
                          padding: '10px 16px', borderRadius: 'var(--radius-sm)',
                          border: `2px solid ${isSelected ? color : 'var(--border)'}`,
                          background: isSelected ? `${color}12` : 'var(--bg-page)',
                          cursor: 'pointer', transition: 'all 0.15s',
                          flex: '1 1 auto',
                        }}
                      >
                        <span style={{ fontSize: '18px', flexShrink: 0 }}>{opt.icon}</span>
                        <div style={{ textAlign: 'left', flex: 1 }}>
                          <div style={{ fontSize: '13px', fontWeight: isSelected ? 700 : 500, color: isSelected ? color : 'var(--text-body)' }}>
                            {opt.label}
                          </div>
                          <div style={{ fontSize: '11px', color: 'var(--text-muted)', lineHeight: 1.4 }}>{opt.desc}</div>
                        </div>
                        {isSelected && (
                          <div style={{ width: '18px', height: '18px', borderRadius: '50%', background: color, display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                            <span style={{ color: '#fff', fontSize: '10px', fontWeight: 700 }}>✓</span>
                          </div>
                        )}
                      </button>
                    );
                  })}
                </div>
              </div>

              {/* Suggested tickers */}
              {sugItems.length > 0 && savedSel ? (
                <>
                  <div style={{ fontSize: '11px', fontWeight: 700, color: 'var(--text-muted)', letterSpacing: '0.5px', textTransform: 'uppercase', marginBottom: '10px' }}>
                    Ativos sugeridos pelo ranking
                  </div>
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', marginBottom: '12px' }}>
                    {sugItems.slice(0, 8).map((asset, idx) => (
                      <TickerChip key={asset.ticker} asset={asset} rank={idx + 1} color={color} />
                    ))}
                  </div>
                  <div style={{ display: 'flex', gap: '16px' }}>
                    <button onClick={onViewSuggestion} style={{ fontSize: '13px', fontWeight: 600, color: 'var(--primary)', background: 'none', border: 'none', cursor: 'pointer', padding: 0 }}>
                      Ver sugestão completa →
                    </button>
                    <button onClick={onViewRanking} style={{ fontSize: '13px', color: 'var(--text-muted)', background: 'none', border: 'none', cursor: 'pointer', padding: 0 }}>
                      Ver ranking top 20
                    </button>
                  </div>
                </>
              ) : savedSel ? (
                <div style={{ padding: '14px', background: 'var(--bg-page)', border: '1px solid var(--border)', borderRadius: 'var(--radius-sm)', fontSize: '13px', color: 'var(--text-muted)', display: 'flex', gap: '10px', alignItems: 'center' }}>
                  <span style={{ fontSize: '20px' }}>📊</span>
                  <div>
                    <div style={{ fontWeight: 600, color: 'var(--text-body)' }}>Ranking não gerado ainda</div>
                    <div style={{ fontSize: '12px' }}>O batch de IA processa dados em dias úteis. Verifique o ranking amanhã.</div>
                  </div>
                </div>
              ) : null}

              {/* Asset count guidance */}
              {info && (
                <div style={{ marginTop: '12px', padding: '10px 14px', background: `${color}0D`, border: `1px solid ${color}30`, borderRadius: 'var(--radius-sm)', fontSize: '12px', color: 'var(--text-body)', display: 'flex', gap: '8px', alignItems: 'center' }}>
                  <span style={{ fontSize: '16px' }}>{info.emoji}</span>
                  <span>
                    Diversificação recomendada:{' '}
                    <strong style={{ color }}>{info.qtdMin}–{info.qtdMax} {isAcoes ? 'ações' : 'FIIs'}</strong>
                    {' '}para equilibrar risco e retorno sem diluição excessiva.
                  </span>
                </div>
              )}
            </>
          )}
        </div>
      )}
    </div>
  );
}

// ── TickerChip ───────────────────────────────────────────────────────────────
function TickerChip({ asset, rank, color }: { asset: SuggestionAsset; rank: number; color: string }) {
  return (
    <div style={{
      background: 'var(--bg-page)', border: '1px solid var(--border)',
      borderLeft: `3px solid ${color}`, borderRadius: 'var(--radius-sm)',
      padding: '8px 12px', minWidth: '90px',
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '5px', marginBottom: '2px' }}>
        <span style={{ fontSize: '10px', fontWeight: 700, color }}>#{rank}</span>
        <span style={{ fontSize: '14px', fontWeight: 800, color: 'var(--text-heading)' }}>{asset.ticker}</span>
      </div>
      <div style={{ fontSize: '11px', color: 'var(--text-muted)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', maxWidth: '110px' }}>
        {asset.nome}
      </div>
      <div style={{ marginTop: '3px', fontSize: '10px', fontWeight: 700, color }}>
        Score {Math.round(asset.scoreTotal * 10) / 10}
      </div>
    </div>
  );
}
