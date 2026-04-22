import { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import type { Profile } from '../../types/profile';
import DonutChart from '../../components/charts/DonutChart';
import AppLayout from '../../components/layout/AppLayout';
import Button from '../../components/ui/Button';
import { CLASS_COLORS } from '../../components/charts/DonutChart';
import { profileService } from '../../services/profileService';
import { usePhase } from '../../contexts/PhaseContext';

const PERFIL_LABELS: Record<string, string> = {
  conservador: 'Conservador',
  moderado: 'Moderado',
  arrojado: 'Arrojado',
};

const PERFIL_DESC: Record<string, string> = {
  conservador: 'Prioridade em segurança e liquidez. Menor exposição a ativos de risco.',
  moderado: 'Equilíbrio entre rentabilidade e risco. Boa diversificação entre renda fixa e variável.',
  arrojado: 'Maior exposição a renda variável em busca de rentabilidade superior no longo prazo.',
};

const FAIXA_LABELS: Record<string, string> = {
  ate_10k: 'Até R$10.000',
  '10k_100k': 'R$10.000 a R$100.000',
  acima_100k: 'Acima de R$100.000',
};

const BRL = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 });

export default function OnboardingResultPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { refreshPhase } = usePhase();
  const [profile, setProfile] = useState<Profile | null>(
    (location.state?.profile as Profile | undefined) ?? null,
  );
  const [isLoading, setIsLoading] = useState(!location.state?.profile);
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    // If profile was passed via router state (just finished onboarding), use it directly.
    // If navigating from the sidebar/directly, fetch from API.
    if (!profile) {
      profileService.getProfile()
        .then((p) => setProfile(p))
        .catch(() => navigate('/onboarding'))
        .finally(() => setIsLoading(false));
    }
  }, []);

  useEffect(() => {
    if (profile) {
      // Recarrega o estado real do backend para que o sidebar reflita os checks corretos
      refreshPhase();
      const t = setTimeout(() => setVisible(true), 80);
      return () => clearTimeout(t);
    }
  }, [profile]);

  if (isLoading) {
    return (
      <AppLayout title="Meu Perfil">
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '300px' }}>
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontSize: '36px', marginBottom: '10px' }}>⏳</div>
            <p style={{ color: 'var(--text-muted)' }}>Carregando perfil...</p>
          </div>
        </div>
      </AppLayout>
    );
  }

  if (!profile) return null;

  const mainClasse = profile.alocacaoAlvo.reduce(
    (best, cur) => (cur.percentual > best.percentual ? cur : best),
    profile.alocacaoAlvo[0],
  );
  const mainColor = CLASS_COLORS[mainClasse?.classe ?? ''] ?? 'var(--primary)';

  return (
    <AppLayout title="Seu Perfil de Investidor">
      <div
        style={{
          maxWidth: '640px',
          opacity: visible ? 1 : 0,
          transform: visible ? 'translateY(0)' : 'translateY(12px)',
          transition: 'opacity 0.45s ease, transform 0.45s ease',
        }}
      >
        {/* ── Hero banner ───────────────────────────────────────────────── */}
        <div style={{
          background: 'linear-gradient(135deg, #1B2559 0%, #2B3674 100%)',
          borderRadius: 'var(--radius-lg)',
          padding: '32px',
          marginBottom: '20px',
          color: '#fff',
          position: 'relative',
          overflow: 'hidden',
          boxShadow: '0 8px 32px rgba(27,37,89,0.25)',
        }}>
          {/* Decorative circle */}
          <div style={{
            position: 'absolute', top: '-40px', right: '-40px',
            width: '180px', height: '180px', borderRadius: '50%',
            background: `${mainColor}18`,
          }} />

          {/* Pill */}
          <div style={{
            display: 'inline-flex', alignItems: 'center', gap: '6px',
            background: 'rgba(0,178,169,0.15)', color: '#00B2A9',
            fontSize: '11px', fontWeight: 700, letterSpacing: '0.6px',
            padding: '5px 12px', borderRadius: '20px',
            marginBottom: '16px', textTransform: 'uppercase',
          }}>
            ✓ Perfil definido
          </div>

          <div style={{ display: 'flex', gap: '24px', flexWrap: 'wrap', alignItems: 'flex-start' }}>
            <div style={{ flex: 1, minWidth: '160px' }}>
              <div style={{ fontSize: '11px', color: 'rgba(163,174,208,0.7)', marginBottom: '4px', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                Seu perfil
              </div>
              <div style={{ fontSize: '28px', fontWeight: 800, color: '#fff', letterSpacing: '-0.03em', marginBottom: '6px' }}>
                {PERFIL_LABELS[profile.perfil]}
              </div>
              <div style={{ fontSize: '13px', color: 'rgba(255,255,255,0.65)', lineHeight: 1.5 }}>
                {PERFIL_DESC[profile.perfil]}
              </div>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '14px', flexShrink: 0 }}>
              <Stat label="Patrimônio" value={BRL.format(profile.valorTotal)} color="#00B2A9" />
              <Stat label="Faixa" value={FAIXA_LABELS[profile.faixa]} color="rgba(255,255,255,0.9)" />
              <Stat label="Classes" value={`${profile.alocacaoAlvo.length} classes`} color="rgba(255,255,255,0.9)" />
            </div>
          </div>
        </div>

        {/* ── Allocation card ───────────────────────────────────────────── */}
        <div style={{
          background: 'var(--bg-card)',
          borderRadius: 'var(--radius-lg)',
          border: '1px solid var(--border)',
          boxShadow: 'var(--shadow-sm)',
          overflow: 'hidden',
          marginBottom: '20px',
        }}>
          {/* Card header */}
          <div style={{ padding: '20px 24px 0' }}>
            <h3 style={{ fontSize: '15px', fontWeight: 700, color: 'var(--text-heading)', margin: '0 0 4px' }}>
              Alocação recomendada
            </h3>
            <p style={{ fontSize: '13px', color: 'var(--text-muted)', margin: '0 0 20px' }}>
              Distribuição ideal do seu patrimônio com base no perfil e faixa patrimonial.
            </p>
          </div>

          {/* Donut + bars in two-column layout */}
          <div style={{ display: 'flex', gap: '0', flexWrap: 'wrap' }}>
            {/* Left: Donut */}
            <div style={{ padding: '0 24px 24px', display: 'flex', alignItems: 'center', justifyContent: 'center', minWidth: '220px' }}>
              <DonutChart data={profile.alocacaoAlvo} valorTotal={profile.valorTotal} size={180} />
            </div>

            {/* Right: Allocation bars */}
            <div style={{ flex: 1, minWidth: '220px', padding: '0 24px 24px', borderLeft: '1px solid var(--border)' }}>
              <div style={{ fontSize: '11px', fontWeight: 700, color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.6px', marginBottom: '12px' }}>
                Detalhamento
              </div>
              {profile.alocacaoAlvo.map((item, idx) => {
                const color = CLASS_COLORS[item.classe] ?? '#9ca3af';
                const valor = (item.percentual / 100) * profile.valorTotal;
                return (
                  <div
                    key={item.classe}
                    style={{
                      marginBottom: idx < profile.alocacaoAlvo.length - 1 ? '12px' : 0,
                      opacity: visible ? 1 : 0,
                      transition: `opacity 0.4s ease ${0.1 + idx * 0.08}s`,
                    }}
                  >
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '7px' }}>
                        <div style={{ width: '10px', height: '10px', borderRadius: '3px', background: color, flexShrink: 0 }} />
                        <span style={{ fontSize: '13px', color: 'var(--text-body)', fontWeight: 500 }}>{item.classe}</span>
                      </div>
                      <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                        <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>{BRL.format(valor)}</span>
                        <span style={{ fontSize: '13px', fontWeight: 700, color }}>
                          {item.percentual}%
                        </span>
                      </div>
                    </div>
                    <div style={{ height: '6px', background: 'var(--border)', borderRadius: '3px', overflow: 'hidden' }}>
                      <div style={{
                        height: '100%',
                        width: visible ? `${item.percentual}%` : '0%',
                        background: color,
                        borderRadius: '3px',
                        transition: `width 0.7s ease ${0.2 + idx * 0.1}s`,
                      }} />
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </div>

        {/* ── CTA ────────────────────────────────────────────────────────── */}
        <Button onClick={() => navigate('/strategy')}>
          Continuar →
        </Button>
      </div>
    </AppLayout>
  );
}

function Stat({ label, value, color }: { label: string; value: string; color: string }) {
  return (
    <div>
      <div style={{ fontSize: '10px', color: 'rgba(163,174,208,0.6)', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.4px', marginBottom: '2px' }}>{label}</div>
      <div style={{ fontSize: '14px', fontWeight: 700, color }}>{value}</div>
    </div>
  );
}
