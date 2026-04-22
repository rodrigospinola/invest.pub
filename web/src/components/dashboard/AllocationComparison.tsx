import type { Deviation } from '../../services/dashboardService';
import Card from '../ui/Card';

interface AllocationComparisonProps {
  deviations: Deviation[];
}

function getDesvioColor(diferenca: number): string {
  const abs = Math.abs(diferenca);
  if (abs > 5) return 'var(--error)';
  if (abs > 3) return 'var(--warning)';
  return 'var(--success)';
}

export default function AllocationComparison({ deviations }: AllocationComparisonProps) {
  return (
    <Card title="Alocação da Carteira" style={{ marginBottom: '16px' }}>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
        {deviations.map((d) => (
          <div key={d.classe}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '6px' }}>
              <span style={{ fontSize: '13px', color: 'var(--text-heading)', fontWeight: 600 }}>{d.classe}</span>
              <div style={{ display: 'flex', gap: '10px', fontSize: '12px', color: 'var(--text-muted)' }}>
                <span>Real: <strong>{d.real}%</strong></span>
                <span>Alvo: <strong>{d.alvo}%</strong></span>
              </div>
            </div>

            <div style={{
              height: '18px',
              background: 'var(--border)',
              borderRadius: '4px',
              overflow: 'hidden',
              position: 'relative',
            }}>
              {/* Barra Alvo (tracejada) */}
              <div style={{
                position: 'absolute',
                top: 0,
                left: 0,
                height: '100%',
                width: `${d.alvo}%`,
                borderRight: '2px dashed var(--text-muted)',
                zIndex: 1,
              }} />

              {/* Barra Real */}
              <div style={{
                height: '100%',
                width: `${d.real}%`,
                background: 'var(--primary)',
                borderRadius: '4px',
                transition: 'width 0.8s ease',
              }} />
            </div>

            {Math.abs(d.diferenca) > 1 && (
              <div style={{
                fontSize: '11px',
                marginTop: '4px',
                color: getDesvioColor(d.diferenca),
                textAlign: 'right',
                fontWeight: 600,
              }}>
                {d.diferenca > 0 ? `+${d.diferenca}% acima` : `${d.diferenca}% abaixo`} do alvo
              </div>
            )}
          </div>
        ))}
      </div>
    </Card>
  );
}
