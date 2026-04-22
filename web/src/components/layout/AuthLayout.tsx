interface AuthLayoutProps {
  children: React.ReactNode;
}

const FEATURES = [
  { icon: '🎯', text: 'Perfil de investidor personalizado via IA' },
  { icon: '📊', text: 'Ranking semanal de ações e FIIs' },
  { icon: '🔔', text: 'Alertas de desvio de alocação em tempo real' },
  { icon: '📈', text: 'Acompanhamento rumo aos R$500k' },
];

export default function AuthLayout({ children }: AuthLayoutProps) {
  return (
    <div style={{ display: 'flex', minHeight: '100vh' }}>
      {/* Left panel — hero */}
      <div style={{
        flex: '0 0 480px',
        background: 'linear-gradient(135deg, #00B2A9 0%, #1B2559 100%)',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        padding: '60px 56px',
        color: '#fff',
      }}
        className="auth-hero"
      >
        {/* Logo */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '48px' }}>
          <div style={{
            width: '40px',
            height: '40px',
            borderRadius: '12px',
            background: 'rgba(255,255,255,0.2)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontSize: '20px',
            fontWeight: 700,
          }}>
            R
          </div>
          <span style={{ fontSize: '22px', fontWeight: 700, letterSpacing: '-0.02em' }}>Invest</span>
        </div>

        {/* Headline */}
        <h1 style={{
          fontSize: '36px',
          fontWeight: 800,
          lineHeight: 1.2,
          margin: '0 0 16px',
          letterSpacing: '-0.03em',
          color: '#fff',
        }}>
          Do primeiro investimento<br />a R$500k
        </h1>
        <p style={{ fontSize: '16px', color: 'rgba(255,255,255,0.75)', margin: '0 0 40px', lineHeight: 1.6 }}>
          Sua plataforma inteligente de investimentos, do primeiro aporte até a independência financeira.
        </p>

        {/* Features */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          {FEATURES.map((f, i) => (
            <div key={i} style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
              <div style={{
                width: '36px',
                height: '36px',
                borderRadius: '10px',
                background: 'rgba(255,255,255,0.15)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontSize: '18px',
                flexShrink: 0,
              }}>
                {f.icon}
              </div>
              <span style={{ fontSize: '14px', color: 'rgba(255,255,255,0.9)', lineHeight: 1.4 }}>{f.text}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Right panel — form */}
      <div style={{
        flex: 1,
        background: 'var(--bg-page)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '40px 32px',
        overflowY: 'auto',
      }}>
        <div style={{ width: '100%', maxWidth: '420px' }}>
          {children}
        </div>
      </div>

      <style>{`
        @media (max-width: 768px) {
          .auth-hero { display: none !important; }
        }
      `}</style>
    </div>
  );
}
