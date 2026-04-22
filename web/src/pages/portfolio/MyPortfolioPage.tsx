import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import AppLayout from '../../components/layout/AppLayout';
import DonutChart, { CLASS_COLORS } from '../../components/charts/DonutChart';
import Button from '../../components/ui/Button';
import { rankingService } from '../../services/rankingService';
import { profileService } from '../../services/profileService';
import { usePhase } from '../../contexts/PhaseContext';
import type { AssetItem } from '../../types/ranking';
import type { Profile } from '../../types/profile';

// ── Mapeamento enum API → nome de exibição (usado por DonutChart / CLASS_COLORS) ──
const CLASSE_DISPLAY: Record<string, string> = {
  RFDinamica:           'RF Dinâmica',
  RFPos:                'RF Pós',
  FundosImobiliarios:   'Fundos imobiliários',
  Acoes:                'Ações',
  Internacional:        'Internacional',
  FundosMultimercados:  'Fundos multimercados',
  Alternativos:         'Alternativos',
};

const BRL  = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 });
const PCT  = (n: number) => `${n >= 0 ? '+' : ''}${n.toFixed(1)}%`;

// ── Desvio: regras de negócio (CLAUDE.md) ─────────────────────────────────────
// 0–3%  → normal (verde)  |  3–5% → atenção (amarelo)  |  >5% → crítico (vermelho)
function desvioStatus(dev: number): { color: string; label: string; bg: string } {
  const abs = Math.abs(dev);
  if (abs <= 3) return { color: '#05CD44', label: 'OK',      bg: 'rgba(5,205,68,0.1)' };
  if (abs <= 5) return { color: '#FFB547', label: 'Atenção', bg: 'rgba(255,181,71,0.1)' };
  return         { color: '#EE5D50', label: 'Crítico', bg: 'rgba(238,93,80,0.1)' };
}

// ── Page ─────────────────────────────────────────────────────────────────────
export default function MyPortfolioPage() {
  const navigate = useNavigate();
  const { refreshPhase } = usePhase();

  const [assets,   setAssets]   = useState<AssetItem[]>([]);
  const [profile,  setProfile]  = useState<Profile | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => { loadData(); }, []);

  const loadData = async () => {
    try {
      const [a, p] = await Promise.all([
        rankingService.getAssets(),
        profileService.getProfile().catch(() => null),
      ]);
      setAssets(a);
      setProfile(p);
      if (a.length > 0) refreshPhase();
    } catch {
      /* silently degrade */
    } finally {
      setIsLoading(false);
    }
  };

  // ── Derivações ──────────────────────────────────────────────────────────────
  // Valor por classe (usando nome de exibição como chave)
  const classeValor: Record<string, number> = {};
  for (const a of assets) {
    const display = CLASSE_DISPLAY[a.classe] ?? a.classe;
    classeValor[display] = (classeValor[display] ?? 0) + Number(a.quantidade) * a.precoMedio;
  }

  const totalValor = Object.values(classeValor).reduce((s, v) => s + v, 0);

  // % real por classe
  const actualPct: Record<string, number> = {};
  for (const [k, v] of Object.entries(classeValor)) {
    actualPct[k] = totalValor > 0 ? (v / totalValor) * 100 : 0;
  }

  // % alvo por classe (do perfil)
  const targetPct: Record<string, number> = {};
  for (const item of profile?.alocacaoAlvo ?? []) {
    targetPct[item.classe] = item.percentual;
  }

  // Todas as classes presentes (união de alvo + real)
  const allClasses = Array.from(
    new Set([...Object.keys(targetPct), ...Object.keys(actualPct)])
  );

  // Dados para o donut da alocação real
  const donutData = Object.entries(classeValor)
    .filter(([, v]) => v > 0)
    .map(([classe, v]) => ({
      classe,
      percentual: Math.round((v / totalValor) * 100),
    }));

  // Ativos agrupados por classe (display name)
  const grouped: Record<string, AssetItem[]> = {};
  for (const a of assets) {
    const k = CLASSE_DISPLAY[a.classe] ?? a.classe;
    if (!grouped[k]) grouped[k] = [];
    grouped[k].push(a);
  }

  if (isLoading) {
    return (
      <AppLayout title="Minha Carteira">
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '300px' }}>
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontSize: '36px', marginBottom: '10px' }}>⏳</div>
            <p style={{ color: 'var(--text-muted)' }}>Carregando carteira...</p>
          </div>
        </div>
      </AppLayout>
    );
  }

  // Empty state
  if (assets.length === 0) {
    return (
      <AppLayout title="Minha Carteira">
        <div style={{ maxWidth: '480px' }}>
          <div style={{
            background: 'var(--bg-card)', border: '1px solid var(--border)',
            borderRadius: 'var(--radius-lg)', padding: '48px 32px',
            textAlign: 'center', boxShadow: 'var(--shadow-sm)',
          }}>
            <div style={{ fontSize: '52px', marginBottom: '16px' }}>📂</div>
            <h2 style={{ fontSize: '18px', fontWeight: 700, color: 'var(--text-heading)', margin: '0 0 10px' }}>
              Nenhum ativo importado ainda
            </h2>
            <p style={{ fontSize: '14px', color: 'var(--text-muted)', margin: '0 0 28px', lineHeight: 1.6 }}>
              Importe seu extrato da B3 ou adicione ativos manualmente para visualizar sua carteira e comparar com a alocação ideal.
            </p>
            <Button onClick={() => navigate('/portfolio/import')}>
              Importar carteira →
            </Button>
          </div>
        </div>
      </AppLayout>
    );
  }

  return (
    <AppLayout title="Minha Carteira">
      <div style={{ maxWidth: '860px' }}>

        {/* ── Hero ──────────────────────────────────────────────────────────── */}
        <div style={{
          background: 'linear-gradient(135deg, #1B2559 0%, #2B3674 100%)',
          borderRadius: 'var(--radius-lg)',
          padding: '28px 32px',
          marginBottom: '24px',
          boxShadow: '0 8px 32px rgba(27,37,89,0.2)',
          display: 'flex', alignItems: 'center', justifyContent: 'space-between',
          flexWrap: 'wrap', gap: '20px',
        }}>
          <div>
            <div style={{ fontSize: '11px', fontWeight: 700, letterSpacing: '0.8px', color: 'rgba(163,174,208,0.8)', textTransform: 'uppercase', marginBottom: '8px' }}>
              Patrimônio total investido
            </div>
            <div style={{ fontSize: '34px', fontWeight: 800, color: '#fff', letterSpacing: '-0.03em', marginBottom: '6px' }}>
              {BRL.format(totalValor)}
            </div>
            <div style={{ display: 'flex', gap: '16px', flexWrap: 'wrap' }}>
              <span style={{ fontSize: '13px', color: 'rgba(163,174,208,0.85)' }}>
                {assets.length} ativo{assets.length !== 1 ? 's' : ''}
              </span>
              <span style={{ fontSize: '13px', color: 'rgba(163,174,208,0.85)' }}>
                {Object.keys(grouped).length} classe{Object.keys(grouped).length !== 1 ? 's' : ''}
              </span>
            </div>
          </div>
          <button
            onClick={() => navigate('/portfolio/import')}
            style={{
              display: 'flex', alignItems: 'center', gap: '8px',
              padding: '10px 20px', borderRadius: 'var(--radius-md)',
              border: '1.5px solid rgba(255,255,255,0.25)',
              background: 'rgba(255,255,255,0.08)',
              color: '#fff', fontSize: '14px', fontWeight: 600,
              cursor: 'pointer', transition: 'background 0.15s', flexShrink: 0,
            }}
            onMouseEnter={e => (e.currentTarget.style.background = 'rgba(255,255,255,0.15)')}
            onMouseLeave={e => (e.currentTarget.style.background = 'rgba(255,255,255,0.08)')}
          >
            <span style={{ fontSize: '16px' }}>📥</span>
            Atualizar carteira
          </button>
        </div>

        {/* ── Donut + tabela de alocação ────────────────────────────────────── */}
        <div style={{
          display: 'grid', gridTemplateColumns: 'auto 1fr',
          gap: '0', marginBottom: '24px',
          background: 'var(--bg-card)', border: '1px solid var(--border)',
          borderRadius: 'var(--radius-lg)', boxShadow: 'var(--shadow-sm)',
          overflow: 'hidden',
        }}>
          {/* Donut — alocação real */}
          <div style={{
            padding: '28px 32px',
            display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center',
            borderRight: '1px solid var(--border)', minWidth: '200px',
          }}>
            <div style={{ fontSize: '11px', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.6px', color: 'var(--text-muted)', marginBottom: '16px' }}>
              Alocação real
            </div>
            <DonutChart data={donutData} valorTotal={totalValor} size={170} noLegend />
          </div>

          {/* Tabela comparativa */}
          <div style={{ padding: '24px 28px', overflowX: 'auto' }}>
            <div style={{ fontSize: '11px', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.6px', color: 'var(--text-muted)', marginBottom: '16px' }}>
              Comparativo: Real vs Alvo
            </div>

            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px' }}>
              <thead>
                <tr>
                  {['Classe', 'Alvo', 'Real', 'Desvio', 'Valor'].map(h => (
                    <th key={h} style={{
                      textAlign: h === 'Classe' ? 'left' : 'right',
                      padding: '0 8px 10px',
                      fontSize: '11px', fontWeight: 700,
                      color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.5px',
                      borderBottom: '1px solid var(--border)',
                      whiteSpace: 'nowrap',
                    }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {allClasses.map((classe) => {
                  const real   = actualPct[classe] ?? 0;
                  const alvo   = targetPct[classe] ?? 0;
                  const dev    = real - alvo;
                  const status = desvioStatus(dev);
                  const color  = CLASS_COLORS[classe] ?? '#9ca3af';
                  const valor  = classeValor[classe] ?? 0;

                  return (
                    <tr key={classe} style={{ borderBottom: '1px solid var(--border)' }}>
                      <td style={{ padding: '10px 8px' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                          <div style={{ width: '10px', height: '10px', borderRadius: '3px', background: color, flexShrink: 0 }} />
                          <span style={{ color: 'var(--text-body)', fontWeight: 500 }}>{classe}</span>
                        </div>
                      </td>
                      <td style={{ padding: '10px 8px', textAlign: 'right', color: 'var(--text-muted)' }}>
                        {alvo > 0 ? `${alvo}%` : '—'}
                      </td>
                      <td style={{ padding: '10px 8px', textAlign: 'right', fontWeight: 700, color }}>
                        {real.toFixed(1)}%
                      </td>
                      <td style={{ padding: '10px 8px', textAlign: 'right' }}>
                        {alvo > 0 ? (
                          <span style={{
                            display: 'inline-block', padding: '2px 8px',
                            borderRadius: '4px', fontSize: '12px', fontWeight: 700,
                            background: status.bg, color: status.color,
                          }}>
                            {PCT(dev)}
                          </span>
                        ) : (
                          <span style={{ color: 'var(--text-muted)', fontSize: '12px' }}>—</span>
                        )}
                      </td>
                      <td style={{ padding: '10px 8px', textAlign: 'right', color: 'var(--text-muted)', whiteSpace: 'nowrap' }}>
                        {BRL.format(valor)}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
              <tfoot>
                <tr>
                  <td colSpan={4} style={{ padding: '12px 8px 0', fontWeight: 700, color: 'var(--text-heading)', fontSize: '13px' }}>
                    Total
                  </td>
                  <td style={{ padding: '12px 8px 0', textAlign: 'right', fontWeight: 800, color: 'var(--primary)', fontSize: '14px', whiteSpace: 'nowrap' }}>
                    {BRL.format(totalValor)}
                  </td>
                </tr>
              </tfoot>
            </table>
          </div>
        </div>

        {/* ── Cards por classe ──────────────────────────────────────────────── */}
        {allClasses.map((classe) => {
          const items = grouped[classe] ?? [];
          const real  = actualPct[classe] ?? 0;
          const alvo  = targetPct[classe] ?? 0;
          const dev   = alvo > 0 ? real - alvo : null;
          const valor = classeValor[classe] ?? 0;
          const color = CLASS_COLORS[classe] ?? '#9ca3af';

          return (
            <ClassCard
              key={classe}
              classe={classe}
              color={color}
              items={items}
              valor={valor}
              realPct={real}
              alvoPct={alvo}
              desvio={dev}
            />
          );
        })}

      </div>
    </AppLayout>
  );
}

// ── ClassCard ─────────────────────────────────────────────────────────────────
function ClassCard({
  classe, color, items, valor, realPct, alvoPct, desvio,
}: {
  classe: string; color: string;
  items: AssetItem[]; valor: number;
  realPct: number; alvoPct: number; desvio: number | null;
}) {
  const [open, setOpen] = useState(true);
  const status = desvio !== null ? desvioStatus(desvio) : null;

  return (
    <div style={{
      background: 'var(--bg-card)',
      borderRadius: 'var(--radius-lg)',
      border: `1px solid ${status && Math.abs(desvio!) > 5 ? `${color}50` : 'var(--border)'}`,
      boxShadow: status && Math.abs(desvio!) > 5 ? `0 0 0 2px ${color}15` : 'var(--shadow-sm)',
      marginBottom: '14px',
      overflow: 'hidden',
      transition: 'border-color 0.2s',
    }}>
      {/* Header */}
      <button
        onClick={() => setOpen(!open)}
        style={{
          width: '100%', background: 'transparent', border: 'none', cursor: 'pointer',
          display: 'flex', alignItems: 'center', gap: '14px',
          padding: '16px 20px', textAlign: 'left',
          borderBottom: open ? '1px solid var(--border)' : 'none',
          transition: 'background 0.15s',
        }}
        onMouseEnter={e => { (e.currentTarget as HTMLElement).style.background = 'var(--bg-page)'; }}
        onMouseLeave={e => { (e.currentTarget as HTMLElement).style.background = 'transparent'; }}
      >
        <div style={{ width: '12px', height: '12px', borderRadius: '4px', background: color, flexShrink: 0 }} />

        <div style={{ flex: 1, minWidth: 0 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', flexWrap: 'wrap' }}>
            <span style={{ fontSize: '14px', fontWeight: 700, color: 'var(--text-heading)' }}>{classe}</span>
            {items.length > 0 && (
              <span style={{ fontSize: '11px', color: 'var(--text-muted)', background: 'var(--bg-page)', padding: '1px 7px', borderRadius: '4px' }}>
                {items.length} ativo{items.length !== 1 ? 's' : ''}
              </span>
            )}
            {status && (
              <span style={{ fontSize: '11px', fontWeight: 700, background: status.bg, color: status.color, padding: '2px 8px', borderRadius: '4px' }}>
                {desvio !== null ? PCT(desvio!) : ''} {status.label}
              </span>
            )}
          </div>
          {/* Mini progress: alvo vs real */}
          {alvoPct > 0 && (
            <div style={{ marginTop: '6px', display: 'flex', alignItems: 'center', gap: '8px' }}>
              <span style={{ fontSize: '11px', color: 'var(--text-muted)', whiteSpace: 'nowrap' }}>
                Alvo {alvoPct}%
              </span>
              <div style={{ flex: 1, height: '4px', background: 'var(--border)', borderRadius: '2px', maxWidth: '120px', position: 'relative' }}>
                {/* Alvo bar */}
                <div style={{ position: 'absolute', top: 0, left: 0, height: '100%', width: `${Math.min(alvoPct, 100)}%`, background: `${color}40`, borderRadius: '2px' }} />
                {/* Real bar */}
                <div style={{ position: 'absolute', top: 0, left: 0, height: '100%', width: `${Math.min(realPct, 100)}%`, background: color, borderRadius: '2px' }} />
              </div>
              <span style={{ fontSize: '11px', fontWeight: 700, color, whiteSpace: 'nowrap' }}>
                Real {realPct.toFixed(1)}%
              </span>
            </div>
          )}
        </div>

        <div style={{ textAlign: 'right', flexShrink: 0, marginRight: '8px' }}>
          <div style={{ fontSize: '15px', fontWeight: 800, color: 'var(--text-heading)' }}>{BRL.format(valor)}</div>
          <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{realPct.toFixed(1)}% da carteira</div>
        </div>

        <span style={{ color: 'var(--text-muted)', fontSize: '12px', flexShrink: 0 }}>{open ? '▲' : '▼'}</span>
      </button>

      {/* Assets list */}
      {open && items.length > 0 && (
        <div>
          {/* Sub-header */}
          <div style={{
            display: 'grid', gridTemplateColumns: '1fr repeat(3, auto)',
            gap: '0', padding: '8px 20px',
            background: 'var(--bg-page)',
            fontSize: '11px', fontWeight: 700, color: 'var(--text-muted)',
            textTransform: 'uppercase', letterSpacing: '0.5px',
          }}>
            <span>Ativo</span>
            <span style={{ textAlign: 'right', paddingRight: '16px' }}>Qtd</span>
            <span style={{ textAlign: 'right', paddingRight: '16px' }}>Preço</span>
            <span style={{ textAlign: 'right' }}>Valor</span>
          </div>

          {items.map((item, idx) => {
            const itemValor = Number(item.quantidade) * item.precoMedio;
            return (
              <div
                key={item.id}
                style={{
                  display: 'grid', gridTemplateColumns: '1fr repeat(3, auto)',
                  alignItems: 'center', gap: '0',
                  padding: '11px 20px',
                  borderTop: '1px solid var(--border)',
                  background: idx % 2 === 0 ? 'transparent' : 'var(--bg-page)',
                }}
              >
                {/* Ativo info */}
                <div style={{ display: 'flex', alignItems: 'center', gap: '10px', minWidth: 0 }}>
                  <div style={{ width: '8px', height: '8px', borderRadius: '50%', background: color, flexShrink: 0 }} />
                  <div style={{ minWidth: 0 }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                      <span style={{ fontSize: '13px', fontWeight: 700, color: 'var(--text-heading)' }}>{item.ticker}</span>
                      {item.origem === 'sugerido' && (
                        <span style={{ fontSize: '10px', fontWeight: 600, background: 'var(--primary-light)', color: 'var(--primary)', padding: '1px 5px', borderRadius: '4px' }}>
                          SUGERIDO
                        </span>
                      )}
                    </div>
                    <div style={{ fontSize: '11px', color: 'var(--text-muted)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', maxWidth: '260px' }}>
                      {item.nome !== item.ticker ? item.nome : ''}
                    </div>
                  </div>
                </div>

                {/* Quantidade */}
                <div style={{ textAlign: 'right', paddingRight: '16px', fontSize: '13px', color: 'var(--text-body)', whiteSpace: 'nowrap' }}>
                  {Number(item.quantidade) % 1 === 0
                    ? Number(item.quantidade).toLocaleString('pt-BR')
                    : Number(item.quantidade).toFixed(3)}
                </div>

                {/* Preço médio */}
                <div style={{ textAlign: 'right', paddingRight: '16px', fontSize: '13px', color: 'var(--text-muted)', whiteSpace: 'nowrap' }}>
                  {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(item.precoMedio)}
                </div>

                {/* Valor total */}
                <div style={{ textAlign: 'right', fontSize: '13px', fontWeight: 700, color: 'var(--text-heading)', whiteSpace: 'nowrap' }}>
                  {BRL.format(itemValor)}
                </div>
              </div>
            );
          })}

          {/* Subtotal da classe */}
          <div style={{
            display: 'flex', justifyContent: 'space-between', alignItems: 'center',
            padding: '10px 20px',
            borderTop: '1px solid var(--border)',
            background: `${color}08`,
          }}>
            <span style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-muted)' }}>
              Subtotal {classe}
            </span>
            <span style={{ fontSize: '14px', fontWeight: 800, color }}>
              {BRL.format(valor)}
            </span>
          </div>
        </div>
      )}

      {/* Empty class (in target but no assets) */}
      {open && items.length === 0 && (
        <div style={{ padding: '16px 20px', display: 'flex', alignItems: 'center', gap: '10px' }}>
          <span style={{ fontSize: '16px' }}>💡</span>
          <span style={{ fontSize: '13px', color: 'var(--text-muted)' }}>
            Nenhum ativo desta classe na carteira. Considere adicionar para atingir o alvo de {alvoPct}%.
          </span>
        </div>
      )}
    </div>
  );
}
