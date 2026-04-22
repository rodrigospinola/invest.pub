import type { AlertSummary } from '../../services/dashboardService';
import { alertService } from '../../services/alertService';
import { useState } from 'react';
import Card from '../ui/Card';

interface AlertListProps {
  alerts: AlertSummary[];
  onRefresh: () => void;
}

const TIPO_LABELS: Record<string, string> = {
  Deviation: 'Desvio',
  RankingChange: 'Ranking',
  Rebalancing: 'Rebalanceamento',
  Milestone: 'Conquista',
};

const TIPO_ICONS: Record<string, string> = {
  Deviation: '⚠️',
  RankingChange: '📢',
  Rebalancing: '🔄',
  Milestone: '🏆',
};

function getTipoTextColor(tipo: string): string {
  if (tipo === 'Deviation') return 'var(--error)';
  if (tipo === 'RankingChange') return 'var(--primary)';
  return 'var(--text-muted)';
}

function getTipoBadgeStyle(tipo: string): React.CSSProperties {
  if (tipo === 'Deviation') {
    return {
      background: '#FFF0EF',
      color: 'var(--error)',
      border: '1px solid var(--error)',
    };
  }
  if (tipo === 'RankingChange') {
    return {
      background: 'var(--primary-light)',
      color: 'var(--primary)',
      border: '1px solid var(--primary)',
    };
  }
  return {
    background: 'var(--bg-page)',
    color: 'var(--text-muted)',
    border: '1px solid var(--border)',
  };
}

export default function AlertList({ alerts, onRefresh }: AlertListProps) {
  const [isReading, setIsReading] = useState<string | null>(null);

  const handleRead = async (id: string) => {
    setIsReading(id);
    try {
      await alertService.markAsRead(id);
      onRefresh();
    } finally {
      setIsReading(null);
    }
  };

  if (alerts.length === 0) return null;

  return (
    <Card title={`Alertas Recentes (${alerts.length})`} style={{ marginBottom: '16px' }}>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
        {alerts.map((a) => (
          <div
            key={a.id}
            style={{
              background: 'var(--bg-page)',
              borderRadius: 'var(--radius-sm)',
              padding: '12px',
              display: 'flex',
              gap: '12px',
            }}
          >
            <div style={{ fontSize: '24px', flexShrink: 0 }}>{TIPO_ICONS[a.tipo] || '🔔'}</div>
            <div style={{ flex: 1 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '4px', flexWrap: 'wrap', gap: '4px' }}>
                <span style={{
                  fontSize: '11px',
                  fontWeight: 600,
                  textTransform: 'uppercase',
                  padding: '2px 8px',
                  borderRadius: '20px',
                  ...getTipoBadgeStyle(a.tipo),
                }}>
                  {TIPO_LABELS[a.tipo] || a.tipo}
                </span>
                <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
                  {new Date(a.createdAt).toLocaleDateString('pt-BR')}
                </span>
              </div>
              <h4 style={{
                fontSize: '14px',
                fontWeight: 700,
                color: getTipoTextColor(a.tipo),
                margin: '0 0 4px',
              }}>
                {a.titulo}
              </h4>
              <p style={{ fontSize: '13px', color: 'var(--text-body)', margin: '0 0 12px', lineHeight: 1.4 }}>
                {a.mensagem}
              </p>

              <button
                onClick={() => handleRead(a.id)}
                disabled={isReading === a.id}
                style={{
                  background: 'var(--primary-light)',
                  border: 'none',
                  borderRadius: 'var(--radius-sm)',
                  padding: '6px 12px',
                  color: 'var(--primary)',
                  fontSize: '12px',
                  fontWeight: 600,
                  cursor: 'pointer',
                }}
              >
                {isReading === a.id ? '...' : 'Marcar como lido'}
              </button>
            </div>
          </div>
        ))}
      </div>
    </Card>
  );
}
