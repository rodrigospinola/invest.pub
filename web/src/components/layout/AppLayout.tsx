import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { usePhase, type Phase } from '../../contexts/PhaseContext';
import { profileService } from '../../services/profileService';

// ── Types ──────────────────────────────────────────────────────────────────
// Phase type is now imported from PhaseContext

const PHASE_ORDER: Record<NonNullable<Phase>, number> = {
  onboarding: 0,
  sub_strategy: 1,
  portfolio_import: 2,
  monitoring: 3,
};

function phaseReached(current: Phase, required: NonNullable<Phase>): boolean {
  if (current === null) return true; // no phase info → don't lock
  return PHASE_ORDER[current] >= PHASE_ORDER[required];
}

// ── Helpers ────────────────────────────────────────────────────────────────
// normalize API suffixes like "misto_acoes" → "misto"
function normalizeAcoes(v: string | null) {
  if (!v) return null;
  return v.replace('_acoes', '');
}
function normalizeFiis(v: string | null) {
  if (!v) return null;
  return v.replace('_fiis', '');
}

// ── Section divider ────────────────────────────────────────────────────────
function SectionLabel({ label }: { label: string }) {
  return (
    <div style={{
      padding: '16px 14px 6px',
      fontSize: '10px',
      fontWeight: 700,
      letterSpacing: '0.8px',
      textTransform: 'uppercase',
      color: 'rgba(163,174,208,0.5)',
      userSelect: 'none',
    }}>
      {label}
    </div>
  );
}

// ── Nav item ───────────────────────────────────────────────────────────────
function NavItem({
  icon, label, isActive, isLocked, onClick, indent = false, badge,
}: {
  icon: string;
  label: string;
  isActive: boolean;
  isLocked: boolean;
  onClick: () => void;
  indent?: boolean;
  badge?: React.ReactNode;
}) {
  return (
    <button
      onClick={isLocked ? undefined : onClick}
      title={isLocked ? 'Complete a etapa anterior para desbloquear' : undefined}
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: '10px',
        padding: indent ? '8px 14px 8px 30px' : '10px 14px',
        borderRadius: 'var(--radius-sm)',
        border: 'none',
        background: isActive ? 'var(--sidebar-active-bg)' : 'transparent',
        color: isLocked
          ? 'rgba(163,174,208,0.35)'
          : isActive
            ? 'var(--sidebar-text-active)'
            : 'var(--sidebar-text)',
        fontSize: indent ? '13px' : '14px',
        fontWeight: isActive ? 600 : 400,
        cursor: isLocked ? 'not-allowed' : 'pointer',
        width: '100%',
        textAlign: 'left',
        transition: 'background 0.15s, color 0.15s',
        opacity: isLocked ? 0.55 : 1,
      }}
      onMouseEnter={e => {
        if (!isActive && !isLocked) {
          (e.currentTarget as HTMLElement).style.background = 'rgba(255,255,255,0.06)';
          (e.currentTarget as HTMLElement).style.color = '#fff';
        }
      }}
      onMouseLeave={e => {
        if (!isActive && !isLocked) {
          (e.currentTarget as HTMLElement).style.background = 'transparent';
          (e.currentTarget as HTMLElement).style.color = isLocked
            ? 'rgba(163,174,208,0.35)'
            : 'var(--sidebar-text)';
        }
      }}
    >
      <span style={{ fontSize: indent ? '14px' : '17px', lineHeight: 1, flexShrink: 0 }}>{icon}</span>
      <span style={{ flex: 1 }}>{label}</span>
      {badge}
      {isLocked && (
        <span style={{ fontSize: '11px', opacity: 0.6, marginLeft: 'auto' }}>🔒</span>
      )}
      {isActive && !isLocked && (
        <div style={{
          marginLeft: 'auto',
          width: '4px',
          height: '18px',
          borderRadius: '2px',
          background: 'var(--primary)',
          flexShrink: 0,
        }} />
      )}
    </button>
  );
}

// ── Props ──────────────────────────────────────────────────────────────────
interface AppLayoutProps {
  children: React.ReactNode;
  title: string;
  subtitle?: string;
}

// ── Component ──────────────────────────────────────────────────────────────
export default function AppLayout({ children, title, subtitle }: AppLayoutProps) {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const { phase, subStrategyAcoes, subStrategyFiis, isPhaseLoading } = usePhase();
  const [showResetModal, setShowResetModal] = useState(false);
  const [isResetting, setIsResetting] = useState(false);

  const initials = user?.nome
    ? user.nome.split(' ').map((n: string) => n[0]).slice(0, 2).join('').toUpperCase()
    : 'U';

  const normAcoes = normalizeAcoes(subStrategyAcoes);
  const normFiis  = normalizeFiis(subStrategyFiis);

  // Enquanto carrega, trava tudo para evitar estado inconsistente
  const strategyUnlocked = !isPhaseLoading && phaseReached(phase, 'sub_strategy');
  const cartUnlocked     = !isPhaseLoading && phaseReached(phase, 'portfolio_import');

  // Badges de conclusão — baseados no phase real do backend, NÃO na rota atual
  const profileCompleted   = !isPhaseLoading && phaseReached(phase, 'sub_strategy');
  const strategyCompleted  = !isPhaseLoading && strategyUnlocked && !!normAcoes && !!normFiis;

  const isAt = (path: string) =>
    location.pathname === path || location.pathname.startsWith(path + '/');

  const handleReset = async () => {
    setIsResetting(true);
    try {
      await profileService.resetProfile();
      setShowResetModal(false);
      navigate('/dashboard');
      window.location.reload();
    } catch {
      alert('Erro ao resetar perfil. Tente novamente.');
    } finally {
      setIsResetting(false);
    }
  };

  return (
    <div style={{ display: 'flex', minHeight: '100vh', background: 'var(--bg-page)' }}>

      {/* ── Sidebar ─────────────────────────────────────────────────────── */}
      <aside style={{
        width: '260px',
        flexShrink: 0,
        background: 'var(--sidebar-bg)',
        display: 'flex',
        flexDirection: 'column',
        position: 'fixed',
        top: 0, left: 0, bottom: 0,
        zIndex: 100,
        overflowY: 'auto',
      }}>

        {/* Logo */}
        <div style={{ padding: '28px 24px 20px', borderBottom: '1px solid rgba(255,255,255,0.06)', flexShrink: 0 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <div style={{
              width: '34px', height: '34px', borderRadius: '10px',
              background: 'var(--primary)', display: 'flex',
              alignItems: 'center', justifyContent: 'center',
              fontSize: '17px', fontWeight: 700, color: '#fff', flexShrink: 0,
            }}>R</div>
            <span style={{ fontSize: '17px', fontWeight: 700, color: '#fff', letterSpacing: '-0.02em' }}>
              Invest
            </span>
          </div>
        </div>

        {/* Nav */}
        <nav style={{ flex: 1, padding: '8px 12px 16px', display: 'flex', flexDirection: 'column' }}>

          {/* ── Principal ─── */}
          <SectionLabel label="Principal" />
          <NavItem
            icon="🏠"
            label="Dashboard"
            isActive={location.pathname === '/dashboard'}
            isLocked={false}
            onClick={() => navigate('/dashboard')}
          />

          {/* ── Perfil ─── */}
          <SectionLabel label="Perfil" />
          <NavItem
            icon="🎯"
            label="Meu Perfil"
            isActive={isAt('/onboarding')}
            isLocked={false}
            onClick={() => navigate(phaseReached(phase, 'sub_strategy') ? '/onboarding/result' : '/onboarding')}
            badge={profileCompleted
              ? <span style={{ fontSize: '10px', background: 'rgba(5,205,68,0.15)', color: '#05CD44', padding: '2px 6px', borderRadius: '4px', fontWeight: 600 }}>✓</span>
              : undefined
            }
          />

          {/* ── Estratégia ─── */}
          <SectionLabel label="Estratégia" />
          <NavItem
            icon="🗺️"
            label="Visão geral"
            isActive={isAt('/strategy')}
            isLocked={!strategyUnlocked}
            onClick={() => navigate('/strategy')}
            badge={strategyCompleted
              ? <span style={{ fontSize: '10px', background: 'rgba(5,205,68,0.15)', color: '#05CD44', padding: '2px 6px', borderRadius: '4px', fontWeight: 600 }}>✓</span>
              : undefined
            }
          />

          {/* ── Carteira ─── */}
          <SectionLabel label="Carteira" />
          <NavItem
            icon="📂"
            label="Minha Carteira"
            isActive={isAt('/portfolio/my')}
            isLocked={!cartUnlocked}
            onClick={() => navigate('/portfolio/my')}
          />
          <NavItem
            icon="📥"
            label="Importar"
            isActive={location.pathname === '/portfolio/import' || location.pathname === '/portfolio/confirm'}
            isLocked={!cartUnlocked}
            onClick={() => navigate('/portfolio/import')}
            indent
          />
          <NavItem
            icon="✨"
            label="Sugestão"
            isActive={isAt('/suggestion')}
            isLocked={!cartUnlocked}
            onClick={() => navigate('/suggestion')}
          />
          <NavItem
            icon="📊"
            label="Ranking"
            isActive={isAt('/ranking')}
            isLocked={!cartUnlocked}
            onClick={() => navigate('/ranking')}
          />
        </nav>

        {/* Footer actions */}
        <div style={{ padding: '12px', borderTop: '1px solid rgba(255,255,255,0.06)', display: 'flex', flexDirection: 'column', gap: '2px', flexShrink: 0 }}>
          <button
            onClick={() => setShowResetModal(true)}
            style={{
              display: 'flex', alignItems: 'center', gap: '10px',
              padding: '10px 14px', borderRadius: 'var(--radius-sm)',
              border: 'none', background: 'transparent',
              color: 'rgba(238,93,80,0.75)',
              fontSize: '14px', cursor: 'pointer', width: '100%', textAlign: 'left',
              transition: 'background 0.15s, color 0.15s',
            }}
            onMouseEnter={e => { e.currentTarget.style.background = 'rgba(238,93,80,0.1)'; e.currentTarget.style.color = '#EE5D50'; }}
            onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'rgba(238,93,80,0.75)'; }}
          >
            <span style={{ fontSize: '16px' }}>🔄</span>
            <span>Resetar perfil</span>
          </button>
          <button
            onClick={logout}
            style={{
              display: 'flex', alignItems: 'center', gap: '10px',
              padding: '10px 14px', borderRadius: 'var(--radius-sm)',
              border: 'none', background: 'transparent',
              color: 'var(--sidebar-text)',
              fontSize: '14px', cursor: 'pointer', width: '100%', textAlign: 'left',
              transition: 'background 0.15s',
            }}
            onMouseEnter={e => { e.currentTarget.style.background = 'rgba(255,255,255,0.06)'; }}
            onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; }}
          >
            <span style={{ fontSize: '16px' }}>🚪</span>
            <span>Sair</span>
          </button>
        </div>
      </aside>

      {/* ── Main area ────────────────────────────────────────────────────── */}
      <div style={{ marginLeft: '260px', flex: 1, display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
        <header style={{
          height: '70px',
          background: 'var(--bg-card)',
          boxShadow: 'var(--shadow-sm)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '0 32px',
          position: 'sticky',
          top: 0,
          zIndex: 50,
          borderBottom: '1px solid var(--border)',
        }}>
          <div>
            <h1 style={{ fontSize: '20px', fontWeight: 700, color: 'var(--text-heading)', margin: 0, letterSpacing: '-0.02em' }}>
              {title}
            </h1>
            {subtitle && (
              <p style={{ fontSize: '13px', color: 'var(--text-muted)', margin: 0 }}>{subtitle}</p>
            )}
          </div>

          {/* User info */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
            <div style={{ textAlign: 'right' }}>
              <div style={{ fontSize: '14px', fontWeight: 600, color: 'var(--text-heading)' }}>{user?.nome}</div>
              <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{user?.email}</div>
            </div>
            <div style={{
              width: '40px', height: '40px', borderRadius: '50%',
              background: 'var(--primary)', display: 'flex',
              alignItems: 'center', justifyContent: 'center',
              color: '#fff', fontSize: '14px', fontWeight: 700, flexShrink: 0,
            }}>
              {initials}
            </div>
          </div>
        </header>

        <main style={{ flex: 1, padding: '32px' }}>
          {children}
        </main>
      </div>

      {/* ── Reset modal ──────────────────────────────────────────────────── */}
      {showResetModal && (
        <div
          style={{
            position: 'fixed', inset: 0, zIndex: 1000,
            background: 'rgba(27,37,89,0.5)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            padding: '20px',
          }}
          onClick={(e) => { if (e.target === e.currentTarget) setShowResetModal(false); }}
        >
          <div style={{
            background: 'var(--bg-card)', borderRadius: 'var(--radius-lg)',
            padding: '32px', maxWidth: '420px', width: '100%',
            boxShadow: '0 20px 60px rgba(27,37,89,0.2)',
          }}>
            <div style={{
              width: '56px', height: '56px', borderRadius: '50%',
              background: '#FEF2F2', display: 'flex', alignItems: 'center',
              justifyContent: 'center', fontSize: '24px', margin: '0 auto 20px',
            }}>⚠️</div>
            <h2 style={{ fontSize: '18px', fontWeight: 700, color: 'var(--text-heading)', textAlign: 'center', margin: '0 0 10px' }}>
              Resetar perfil?
            </h2>
            <p style={{ fontSize: '14px', color: 'var(--text-muted)', textAlign: 'center', lineHeight: 1.6, margin: '0 0 8px' }}>
              Esta ação irá apagar permanentemente:
            </p>
            <ul style={{ fontSize: '13px', color: 'var(--text-body)', margin: '0 0 24px', paddingLeft: '20px', lineHeight: 2 }}>
              <li>Seu perfil de investidor</li>
              <li>Sua sub-estratégia (ações e FIIs)</li>
              <li>Todos os ativos da carteira</li>
            </ul>
            <p style={{ fontSize: '13px', color: 'var(--text-muted)', textAlign: 'center', margin: '0 0 28px' }}>
              Você voltará ao onboarding e poderá configurar tudo novamente.
            </p>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
              <button
                onClick={handleReset}
                disabled={isResetting}
                style={{
                  padding: '13px', borderRadius: 'var(--radius-md)',
                  background: isResetting ? '#f5a09a' : 'var(--error)',
                  color: '#fff', border: 'none', fontSize: '14px', fontWeight: 600,
                  cursor: isResetting ? 'wait' : 'pointer', transition: 'background 0.15s',
                }}
              >
                {isResetting ? '⏳ Resetando...' : '🔄 Sim, resetar tudo'}
              </button>
              <button
                onClick={() => setShowResetModal(false)}
                disabled={isResetting}
                style={{
                  padding: '13px', borderRadius: 'var(--radius-md)',
                  background: 'var(--bg-page)', color: 'var(--text-body)',
                  border: '1px solid var(--border)', fontSize: '14px', fontWeight: 500,
                  cursor: 'pointer', transition: 'background 0.15s',
                }}
              >
                Cancelar
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
