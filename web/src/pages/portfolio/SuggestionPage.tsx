import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Button from '../../components/ui/Button';
import AppLayout from '../../components/layout/AppLayout';
import { rankingService } from '../../services/rankingService';
import type { SuggestionResponse, SuggestionAsset } from '../../types/ranking';

const SUB_LABELS: Record<string, string> = {
  valor: 'Valor',
  dividendos: 'Dividendos',
  misto: 'Misto',
  renda: 'Renda',
  valorizacao: 'Valorização',
};

export default function SuggestionPage() {
  const navigate = useNavigate();
  const [suggestion, setSuggestion] = useState<SuggestionResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    loadSuggestion();
  }, []);

  const loadSuggestion = async () => {
    try {
      const data = await rankingService.getSuggestion();
      setSuggestion(data);
    } catch {
      setError('Não foi possível gerar a sugestão. Verifique se o ranking já foi gerado hoje.');
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading) {
    return (
      <AppLayout title="Sua Sugestão">
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '300px' }}>
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontSize: '40px', marginBottom: '12px' }}>⏳</div>
            <p style={{ color: 'var(--text-muted)' }}>Gerando sua sugestão personalizada...</p>
          </div>
        </div>
      </AppLayout>
    );
  }

  if (error || !suggestion) {
    return (
      <AppLayout title="Sua Sugestão">
        <div style={{
          background: 'var(--bg-card)',
          borderRadius: 'var(--radius-lg)',
          padding: '48px',
          textAlign: 'center',
          maxWidth: '420px',
          boxShadow: 'var(--shadow-sm)',
          border: '1px solid var(--border)',
        }}>
          <div style={{ fontSize: '40px', marginBottom: '12px' }}>😕</div>
          <p style={{ color: 'var(--text-muted)', marginBottom: '20px' }}>{error || 'Ocorreu um erro inesperado.'}</p>
          <Button onClick={() => navigate('/ranking')}>Voltar ao ranking</Button>
        </div>
      </AppLayout>
    );
  }

  const totalAtivos = suggestion.acoesRec.length + suggestion.fiisRec.length;

  return (
    <AppLayout
      title="Sua Sugestão"
      subtitle={`${totalAtivos} ativos selecionados com base no seu perfil e estratégia`}
    >
      <div style={{ maxWidth: '700px' }}>

        {/* Estratégias + aviso */}
        <div style={{ display: 'flex', gap: '8px', marginBottom: '20px', flexWrap: 'wrap' }}>
          <StrategyBadge
            label={`Ações: ${SUB_LABELS[suggestion.subEstrategiaAcoes] || suggestion.subEstrategiaAcoes}`}
            bg="var(--primary-light)"
            color="var(--primary)"
          />
          <StrategyBadge
            label={`FIIs: ${SUB_LABELS[suggestion.subEstrategiaFiis] || suggestion.subEstrategiaFiis}`}
            bg="#EEF2FF"
            color="var(--info)"
          />
        </div>

        <div style={{
          background: 'var(--warning-bg)',
          border: '1px solid #FDE68A',
          borderRadius: 'var(--radius-md)',
          padding: '12px 16px',
          marginBottom: '28px',
          display: 'flex',
          gap: '10px',
          alignItems: 'flex-start',
        }}>
          <span style={{ fontSize: '16px', flexShrink: 0 }}>⚠️</span>
          <p style={{ fontSize: '13px', color: '#92400e', margin: 0, lineHeight: 1.5 }}>
            Esta é uma <strong>sugestão educacional</strong>, não uma recomendação de investimento. Consulte um assessor antes de investir.
          </p>
        </div>

        {/* Ações */}
        {suggestion.acoesRec.length > 0 && (
          <SuggestionSection
            title="📊 Ações recomendadas"
            items={suggestion.acoesRec}
            accentColor="var(--primary)"
          />
        )}

        {/* FIIs */}
        {suggestion.fiisRec.length > 0 && (
          <SuggestionSection
            title="🏢 FIIs recomendados"
            items={suggestion.fiisRec}
            accentColor="var(--info)"
          />
        )}

        {/* CTAs */}
        <div style={{ marginTop: '28px', display: 'flex', flexDirection: 'column', gap: '12px' }}>
          <Button onClick={() => navigate('/portfolio/import')}>
            Já comprei — importar minha carteira →
          </Button>
          <div style={{ display: 'flex', justifyContent: 'center', gap: '24px', flexWrap: 'wrap' }}>
            <button
              onClick={() => navigate('/ranking')}
              style={{ background: 'none', border: 'none', color: 'var(--primary)', cursor: 'pointer', fontSize: '14px', fontWeight: 500 }}
            >
              ← Ver ranking completo
            </button>
            <button
              onClick={() => navigate('/dashboard')}
              style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer', fontSize: '14px' }}
            >
              Voltar ao dashboard
            </button>
          </div>
        </div>
      </div>
    </AppLayout>
  );
}

function SuggestionSection({ title, items, accentColor }: { title: string; items: SuggestionAsset[]; accentColor: string }) {
  return (
    <div style={{ marginBottom: '24px' }}>
      <h2 style={{ fontSize: '15px', fontWeight: 700, color: 'var(--text-heading)', marginBottom: '12px' }}>{title}</h2>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
        {items.map((item, idx) => (
          <SuggestionCard key={item.ticker} item={item} rank={idx + 1} accentColor={accentColor} />
        ))}
      </div>
    </div>
  );
}

function SuggestionCard({ item, rank, accentColor }: { item: SuggestionAsset; rank: number; accentColor: string }) {
  const [expanded, setExpanded] = useState(false);
  const score = Math.round(item.scoreTotal * 10) / 10;

  return (
    <div style={{
      background: 'var(--bg-card)',
      borderRadius: 'var(--radius-md)',
      borderLeft: `4px solid ${accentColor}`,
      padding: '14px 18px',
      boxShadow: 'var(--shadow-sm)',
      border: '1px solid var(--border)',
      borderLeftColor: accentColor,
      borderLeftWidth: '4px',
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
        <span style={{
          fontSize: '12px',
          fontWeight: 800,
          color: accentColor,
          width: '24px',
          flexShrink: 0,
          textAlign: 'center',
        }}>
          #{rank}
        </span>
        <div style={{ flex: 1, minWidth: 0 }}>
          <div style={{ fontWeight: 700, color: 'var(--text-heading)', fontSize: '15px' }}>{item.ticker}</div>
          <div style={{ color: 'var(--text-muted)', fontSize: '12px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
            {item.nome}
          </div>
        </div>
        <div style={{
          fontSize: '18px',
          fontWeight: 800,
          color: accentColor,
          flexShrink: 0,
          letterSpacing: '-0.02em',
        }}>
          {score}
        </div>
        {item.justificativa && (
          <button
            onClick={() => setExpanded(!expanded)}
            style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-muted)', fontSize: '14px', padding: '2px' }}
          >
            {expanded ? '▲' : '▼'}
          </button>
        )}
      </div>
      {expanded && item.justificativa && (
        <div style={{
          marginTop: '10px',
          padding: '10px 12px',
          background: 'var(--bg-page)',
          borderRadius: 'var(--radius-sm)',
          fontSize: '13px',
          color: 'var(--text-body)',
          lineHeight: 1.6,
          border: '1px solid var(--border)',
        }}>
          {item.justificativa}
        </div>
      )}
    </div>
  );
}

function StrategyBadge({ label, bg, color }: { label: string; bg: string; color: string }) {
  return (
    <span style={{
      background: bg,
      color: color,
      fontSize: '12px',
      fontWeight: 600,
      padding: '5px 12px',
      borderRadius: 'var(--radius-sm)',
    }}>
      {label}
    </span>
  );
}
