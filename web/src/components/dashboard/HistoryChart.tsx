import type { HistoryPoint } from '../../services/dashboardService';

interface HistoryChartProps {
  points: HistoryPoint[];
}

export default function HistoryChart({ points }: HistoryChartProps) {
  if (points.length < 2) {
    return (
      <div style={{
        background: 'var(--bg-card)',
        borderRadius: 'var(--radius-lg)',
        padding: '24px',
        boxShadow: 'var(--shadow-sm)',
        border: '1px solid var(--border)',
        marginBottom: '16px',
        textAlign: 'center',
        color: 'var(--text-muted)',
        fontSize: '14px'
      }}>
        Aguardando dados históricos para gerar o gráfico...
      </div>
    );
  }

  const width = 600;
  const height = 150;
  const padding = 20;

  const maxVal = Math.max(...points.map(p => p.valorTotal));
  const minVal = Math.min(...points.map(p => p.valorTotal));
  const range = maxVal - minVal || 1;

  const getX = (index: number) => (index / (points.length - 1)) * (width - 2 * padding) + padding;
  const getY = (val: number) => height - ((val - minVal) / range) * (height - 2 * padding) - padding;

  const path = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${getX(i)} ${getY(p.valorTotal)}`).join(' ');
  const areaPath = `${path} L ${getX(points.length - 1)} ${height} L ${getX(0)} ${height} Z`;

  return (
    <div style={{
      background: 'var(--bg-card)',
      borderRadius: 'var(--radius-lg)',
      padding: '20px 24px',
      boxShadow: 'var(--shadow-sm)',
      border: '1px solid var(--border)',
      marginBottom: '16px',
    }}>
      <h3 style={{ fontSize: '15px', fontWeight: 700, color: 'var(--text-heading)', margin: '0 0 16px' }}>
        Evolução do Patrimônio
      </h3>
      <div style={{ position: 'relative', width: '100%', overflow: 'hidden' }}>
        <svg viewBox={`0 0 ${width} ${height}`} width="100%" height={height} preserveAspectRatio="none">
          <defs>
            <linearGradient id="chartGradient" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#00B2A9" stopOpacity="0.25" />
              <stop offset="100%" stopColor="#00B2A9" stopOpacity="0" />
            </linearGradient>
          </defs>
          <path d={areaPath} fill="url(#chartGradient)" />
          <path d={path} fill="none" stroke="#00B2A9" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '8px' }}>
        <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>{new Date(points[0].data).toLocaleDateString()}</span>
        <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>{new Date(points[points.length - 1].data).toLocaleDateString()}</span>
      </div>
    </div>
  );
}
