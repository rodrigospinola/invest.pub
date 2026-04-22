import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import AppLayout from '../../components/layout/AppLayout';
import Button from '../../components/ui/Button';
import { rankingService } from '../../services/rankingService';
import { usePhase } from '../../contexts/PhaseContext';

const ACOES_OPTIONS = [
  {
    value: 'valor',
    label: 'Ações de Valor',
    desc: 'Empresas sólidas com P/L atrativo, negociadas abaixo do seu valor intrínseco.',
    icon: '📊',
  },
  {
    value: 'dividendos',
    label: 'Dividendos',
    desc: 'Empresas com histórico consistente de distribuição de proventos e alto DY.',
    icon: '💰',
  },
  {
    value: 'misto',
    label: 'Misto (Valor + Dividendos)',
    desc: 'Equilíbrio entre crescimento de capital e renda passiva.',
    icon: '⚖️',
  },
];

const FIIS_OPTIONS = [
  {
    value: 'renda',
    label: 'Renda',
    desc: 'FIIs com foco em geração de renda mensal consistente e alto dividend yield.',
    icon: '🏢',
  },
  {
    value: 'valorizacao',
    label: 'Valorização',
    desc: 'FIIs com potencial de valorização das cotas negociadas com desconto (baixo P/VP).',
    icon: '📈',
  },
  {
    value: 'misto',
    label: 'Misto (Renda + Valorização)',
    desc: 'Equilíbrio entre renda mensal e valorização patrimonial.',
    icon: '🔀',
  },
];

export default function SubStrategyPage() {
  const navigate = useNavigate();
  const { refreshPhase } = usePhase();
  const [acoes, setAcoes] = useState('');
  const [fiis, setFiis] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async () => {
    if (!acoes || !fiis) {
      setError('Selecione uma sub-estratégia para cada classe antes de continuar.');
      return;
    }
    setError('');
    setIsLoading(true);
    try {
      await rankingService.createSubStrategy(acoes, fiis);
      refreshPhase();
      navigate('/ranking');
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Erro ao salvar sub-estratégia.';
      setError(message);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <AppLayout title="Estratégia de Investimento">
      <div style={{ maxWidth: '640px' }}>

        {/* Header */}
        <div style={{ textAlign: 'center', marginBottom: '36px' }}>
          <div style={{ fontSize: '48px', marginBottom: '8px' }}>🎯</div>
          <h2 style={{ fontSize: '22px', fontWeight: 700, color: 'var(--text-heading)', margin: '0 0 8px' }}>
            Escolha sua estratégia
          </h2>
          <p style={{ color: 'var(--text-muted)', margin: 0, fontSize: '15px' }}>
            Defina como quer investir em ações e FIIs. Você pode mudar depois.
          </p>
        </div>

        {/* Ações */}
        <div style={{ marginBottom: '32px' }}>
          <h3 style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-heading)', marginBottom: '12px' }}>
            📊 Ações — escolha sua abordagem
          </h3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
            {ACOES_OPTIONS.map((opt) => (
              <OptionCard
                key={opt.value}
                icon={opt.icon}
                label={opt.label}
                desc={opt.desc}
                selected={acoes === opt.value}
                onSelect={() => setAcoes(opt.value)}
              />
            ))}
          </div>
        </div>

        {/* FIIs */}
        <div style={{ marginBottom: '32px' }}>
          <h3 style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-heading)', marginBottom: '12px' }}>
            🏢 Fundos Imobiliários — escolha sua abordagem
          </h3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
            {FIIS_OPTIONS.map((opt) => (
              <OptionCard
                key={opt.value}
                icon={opt.icon}
                label={opt.label}
                desc={opt.desc}
                selected={fiis === opt.value}
                onSelect={() => setFiis(opt.value)}
              />
            ))}
          </div>
        </div>

        {error && (
          <div style={{
            background: '#FFF0EF',
            border: '1px solid var(--error)',
            borderRadius: 'var(--radius-sm)',
            padding: '12px',
            marginBottom: '16px',
            color: 'var(--error)',
            fontSize: '14px',
          }}>
            {error}
          </div>
        )}

        <Button onClick={handleSubmit} isLoading={isLoading} disabled={!acoes || !fiis}>
          Ver ranking de ativos →
        </Button>

        <div style={{ textAlign: 'center', marginTop: '16px' }}>
          <button
            onClick={() => navigate('/dashboard')}
            style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer', fontSize: '14px' }}
          >
            Voltar ao dashboard
          </button>
        </div>
      </div>
    </AppLayout>
  );
}

function OptionCard({
  icon, label, desc, selected, onSelect,
}: {
  icon: string;
  label: string;
  desc: string;
  selected: boolean;
  onSelect: () => void;
}) {
  return (
    <button
      onClick={onSelect}
      style={{
        background: selected ? 'var(--primary-light)' : 'var(--bg-card)',
        border: `2px solid ${selected ? 'var(--primary)' : 'var(--border)'}`,
        borderRadius: 'var(--radius-sm)',
        padding: '16px',
        textAlign: 'left',
        cursor: 'pointer',
        display: 'flex',
        alignItems: 'flex-start',
        gap: '14px',
        transition: 'border-color 0.15s, background 0.15s',
        width: '100%',
      }}
    >
      <span style={{ fontSize: '28px', lineHeight: 1 }}>{icon}</span>
      <div>
        <div style={{ fontWeight: 600, color: selected ? 'var(--primary)' : 'var(--text-heading)', fontSize: '15px', marginBottom: '4px' }}>
          {label}
        </div>
        <div style={{ color: 'var(--text-muted)', fontSize: '13px', lineHeight: 1.5 }}>{desc}</div>
      </div>
      <div style={{ marginLeft: 'auto', flexShrink: 0 }}>
        <div style={{
          width: '20px', height: '20px', borderRadius: '50%',
          border: `2px solid ${selected ? 'var(--primary)' : 'var(--border)'}`,
          background: selected ? 'var(--primary)' : 'transparent',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
        }}>
          {selected && <div style={{ width: '8px', height: '8px', borderRadius: '50%', background: '#fff' }} />}
        </div>
      </div>
    </button>
  );
}
