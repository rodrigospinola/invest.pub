interface CardProps {
  children: React.ReactNode;
  title?: string;
  subtitle?: string;
  action?: React.ReactNode;
  padding?: string | number;
  style?: React.CSSProperties;
}

export default function Card({ children, title, subtitle, action, padding = '24px', style }: CardProps) {
  return (
    <div style={{
      background: 'var(--bg-card)',
      borderRadius: 'var(--radius-lg)',
      boxShadow: 'var(--shadow-sm)',
      border: '1px solid var(--border)',
      overflow: 'hidden',
      ...style,
    }}>
      {(title || action) && (
        <div style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'flex-start',
          padding: `20px ${typeof padding === 'number' ? `${padding}px` : padding} 0`,
          marginBottom: '16px',
        }}>
          <div>
            {title && (
              <h3 style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-heading)', margin: 0 }}>
                {title}
              </h3>
            )}
            {subtitle && (
              <p style={{ fontSize: '13px', color: 'var(--text-muted)', margin: '4px 0 0' }}>
                {subtitle}
              </p>
            )}
          </div>
          {action && <div>{action}</div>}
        </div>
      )}
      <div style={{ padding: typeof padding === 'number' ? `${padding}px` : padding }}>
        {children}
      </div>
    </div>
  );
}
