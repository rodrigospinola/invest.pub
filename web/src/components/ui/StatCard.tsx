interface StatCardProps {
  label: string;
  value: string;
  delta?: number;
  deltaLabel?: string;
  icon?: string;
  color?: string;
}

export default function StatCard({ label, value, delta, deltaLabel, icon, color }: StatCardProps) {
  const isPositive = delta !== undefined && delta >= 0;

  return (
    <div style={{
      background: 'var(--bg-card)',
      borderRadius: 'var(--radius-lg)',
      boxShadow: 'var(--shadow-sm)',
      border: '1px solid var(--border)',
      padding: '20px 24px',
      display: 'flex',
      flexDirection: 'column',
      gap: '4px',
    }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
        <div>
          <div style={{ fontSize: '13px', color: 'var(--text-muted)', fontWeight: 500, marginBottom: '6px' }}>
            {label}
          </div>
          <div style={{
            fontSize: '26px',
            fontWeight: 800,
            color: color || 'var(--text-heading)',
            letterSpacing: '-0.03em',
            lineHeight: 1,
          }}>
            {value}
          </div>
          {delta !== undefined && (
            <div style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '4px',
              marginTop: '8px',
              padding: '3px 8px',
              borderRadius: 'var(--radius-sm)',
              background: isPositive ? 'var(--success-bg)' : 'var(--error-bg)',
            }}>
              <span style={{ fontSize: '11px', fontWeight: 700, color: isPositive ? 'var(--success)' : 'var(--error)' }}>
                {isPositive ? '▲' : '▼'} {Math.abs(delta).toFixed(2)}%
              </span>
              {deltaLabel && (
                <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>{deltaLabel}</span>
              )}
            </div>
          )}
        </div>
        {icon && (
          <div style={{
            width: '44px',
            height: '44px',
            borderRadius: 'var(--radius-md)',
            background: 'var(--primary-light)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontSize: '22px',
            flexShrink: 0,
          }}>
            {icon}
          </div>
        )}
      </div>
    </div>
  );
}
