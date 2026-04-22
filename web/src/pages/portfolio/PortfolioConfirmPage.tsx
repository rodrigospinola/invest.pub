import { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import AppLayout from '../../components/layout/AppLayout';
import Button from '../../components/ui/Button';
import { rankingService } from '../../services/rankingService';
import type { AssetItem } from '../../types/ranking';

interface ImportResult {
  totalImportados: number;
  sugeridos: number;
  proprios: number;
}

const CLASSE_LABELS: Record<string, string> = {
  Acoes: 'Ações',
  FundosImobiliarios: 'FIIs',
  RFPos: 'Renda Fixa Pós',
  RFDinamica: 'Renda Fixa Dinâmica',
  Internacional: 'Internacional',
  FundosMultimercados: 'Multimercados',
  Alternativos: 'Alternativos',
};

export default function PortfolioConfirmPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const result = location.state as ImportResult | undefined;

  const [assets, setAssets] = useState<AssetItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (!result) {
      navigate('/portfolio/import');
      return;
    }
    loadAssets();
  }, []);

  const loadAssets = async () => {
    try {
      const data = await rankingService.getAssets();
      setAssets(data);
    } catch {
      // Silently fail — show partial data
    } finally {
      setIsLoading(false);
    }
  };

  if (!result) return null;

  const aderencia = result.totalImportados > 0
    ? Math.round((result.sugeridos / result.totalImportados) * 100)
    : 0;

  const totalValor = assets.reduce((acc, a) => acc + a.quantidade * a.precoMedio, 0);
  const valorFormatado = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(totalValor);

  const grouped = assets.reduce<Record<string, AssetItem[]>>((acc, a) => {
    const key = a.classe;
    if (!acc[key]) acc[key] = [];
    acc[key].push(a);
    return acc;
  }, {});

  const aderenciaColor = aderencia >= 70 ? 'var(--success)' : aderencia >= 40 ? 'var(--warning)' : 'var(--error)';

  return (
    <AppLayout title="Confirmar Carteira">
      <div style={{ maxWidth: '640px' }}>

        {/* Sucesso */}
        <div style={{
          background: 'var(--primary-light)',
          border: '1px solid var(--primary)',
          borderRadius: 'var(--radius-md)',
          padding: '24px',
          marginBottom: '24px',
          textAlign: 'center',
        }}>
          <div style={{ fontSize: '48px', marginBottom: '8px' }}>🎉</div>
          <h2 style={{ fontSize: '20px', fontWeight: 700, color: 'var(--primary)', margin: '0 0 6px' }}>
            Carteira importada com sucesso!
          </h2>
          <p style={{ color: 'var(--text-body)', fontSize: '14px', margin: 0 }}>
            {result.totalImportados} ativo{result.totalImportados > 1 ? 's' : ''} registrado{result.totalImportados > 1 ? 's' : ''}
          </p>
        </div>

        {/* Stats */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '12px', marginBottom: '24px' }}>
          <StatCard label="Total importado" value={result.totalImportados.toString()} icon="📦" />
          <StatCard label="Da sugestão" value={result.sugeridos.toString()} icon="✅" />
          <StatCard label="Aderência" value={`${aderencia}%`} icon="🎯" color={aderenciaColor} />
        </div>

        {/* Valor total */}
        {totalValor > 0 && (
          <div style={{
            background: 'var(--bg-card)',
            borderRadius: 'var(--radius-md)',
            padding: '16px 20px',
            border: '1px solid var(--border)',
            boxShadow: 'var(--shadow-sm)',
            marginBottom: '20px',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}>
            <span style={{ fontSize: '14px', color: 'var(--text-muted)' }}>Valor total da carteira</span>
            <span style={{ fontSize: '20px', fontWeight: 700, color: 'var(--text-heading)' }}>{valorFormatado}</span>
          </div>
        )}

        {/* Lista por classe */}
        {!isLoading && Object.keys(grouped).length > 0 && (
          <div style={{ marginBottom: '24px' }}>
            <h3 style={{ fontSize: '15px', fontWeight: 700, color: 'var(--text-heading)', marginBottom: '12px' }}>
              Sua carteira
            </h3>
            {Object.entries(grouped).map(([classe, items]) => (
              <ClasseGroup key={classe} classe={classe} items={items} />
            ))}
          </div>
        )}

        {/* Aviso de aderência baixa */}
        {aderencia < 50 && result.sugeridos > 0 && (
          <div style={{
            background: '#FFFBEB',
            border: '1px solid var(--warning)',
            borderRadius: 'var(--radius-sm)',
            padding: '12px 14px',
            marginBottom: '16px',
            display: 'flex',
            gap: '10px',
          }}>
            <span style={{ fontSize: '16px' }}>💡</span>
            <p style={{ fontSize: '13px', color: 'var(--text-body)', margin: 0, lineHeight: 1.5 }}>
              Você adicionou ativos fora da sugestão. Isso é totalmente válido — o sistema monitora todos igualmente.
            </p>
          </div>
        )}

        <Button onClick={() => navigate('/dashboard')}>
          Ir para o Dashboard →
        </Button>

        <div style={{ textAlign: 'center', marginTop: '12px' }}>
          <button
            onClick={() => navigate('/portfolio/import')}
            style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer', fontSize: '14px' }}
          >
            Adicionar mais ativos
          </button>
        </div>
      </div>
    </AppLayout>
  );
}

function StatCard({ label, value, icon, color }: { label: string; value: string; icon: string; color?: string }) {
  return (
    <div style={{
      background: 'var(--bg-card)',
      borderRadius: 'var(--radius-md)',
      padding: '14px',
      border: '1px solid var(--border)',
      boxShadow: 'var(--shadow-sm)',
      textAlign: 'center',
    }}>
      <div style={{ fontSize: '24px', marginBottom: '4px' }}>{icon}</div>
      <div style={{ fontSize: '20px', fontWeight: 700, color: color || 'var(--text-heading)' }}>{value}</div>
      <div style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '2px' }}>{label}</div>
    </div>
  );
}

function ClasseGroup({ classe, items }: { classe: string; items: AssetItem[] }) {
  const [open, setOpen] = useState(true);
  const totalValor = items.reduce((acc, a) => acc + a.quantidade * a.precoMedio, 0);
  const valorFormatado = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(totalValor);

  return (
    <div style={{
      background: 'var(--bg-card)',
      borderRadius: 'var(--radius-md)',
      border: '1px solid var(--border)',
      boxShadow: 'var(--shadow-sm)',
      marginBottom: '10px',
      overflow: 'hidden',
    }}>
      <button
        onClick={() => setOpen(!open)}
        style={{
          width: '100%',
          padding: '14px 16px',
          background: 'none',
          border: 'none',
          cursor: 'pointer',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          borderBottom: open ? '1px solid var(--border)' : 'none',
        }}
      >
        <span style={{ fontWeight: 600, color: 'var(--text-heading)', fontSize: '14px' }}>
          {CLASSE_LABELS[classe] || classe} ({items.length})
        </span>
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <span style={{ fontSize: '13px', color: 'var(--text-muted)' }}>{valorFormatado}</span>
          <span style={{ color: 'var(--text-muted)', fontSize: '12px' }}>{open ? '▲' : '▼'}</span>
        </div>
      </button>
      {open && items.map((item) => (
        <div key={item.id} style={{
          padding: '10px 16px',
          borderBottom: '1px solid var(--border)',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
        }}>
          <div>
            <div style={{ fontWeight: 600, color: 'var(--text-heading)', fontSize: '13px', display: 'flex', alignItems: 'center', gap: '6px' }}>
              {item.ticker}
              {item.origem === 'sugerido' && (
                <span style={{
                  background: 'var(--primary-light)',
                  color: 'var(--primary)',
                  fontSize: '10px',
                  fontWeight: 600,
                  padding: '1px 5px',
                  borderRadius: '4px',
                }}>
                  SUGERIDO
                </span>
              )}
            </div>
            <div style={{ color: 'var(--text-muted)', fontSize: '12px' }}>{item.nome}</div>
          </div>
          <div style={{ textAlign: 'right' }}>
            <div style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-heading)' }}>
              {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(item.quantidade * item.precoMedio)}
            </div>
            <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
              {item.quantidade} × {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(item.precoMedio)}
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
