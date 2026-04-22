import { useState, useRef, useEffect, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useOnboarding } from '../../hooks/useOnboarding';
import ChatBubble from '../../components/ui/ChatBubble';
import DonutChart from '../../components/charts/DonutChart';
import Button from '../../components/ui/Button';
import Input from '../../components/ui/Input';

const INITIAL_MESSAGE = {
  role: 'assistant' as const,
  content: 'Olá! Sou o assistente do Invest. Vou te ajudar a montar a carteira ideal para o seu perfil. Para começar, me conta: quanto você tem disponível para investir agora?',
};

export default function OnboardingPage() {
  const navigate = useNavigate();
  const { messages, isLoading, profile, error, sendMessage, suggestedReplies, allocationPreview, profileSaved } = useOnboarding();
  const [input, setInput] = useState('');
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const allMessages = messages.length === 0 ? [INITIAL_MESSAGE] : messages;

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [allMessages, isLoading]);

  // Quando o perfil for salvo, redireciona para a tela de resultado
  useEffect(() => {
    if (profile) {
      navigate('/onboarding/result', { state: { profile } });
    }
  }, [profile, navigate]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const text = input.trim();
    if (!text || isLoading) return;
    setInput('');
    await sendMessage(text);
  };

  return (
    <div style={{ minHeight: '100vh', background: 'var(--bg-page)', display: 'flex', flexDirection: 'column' }}>
      {/* Header */}
      <div style={{
        background: 'var(--sidebar-bg)',
        padding: '16px 20px',
        display: 'flex',
        alignItems: 'center',
        gap: '12px',
      }}>
        <div style={{
          width: '36px',
          height: '36px',
          borderRadius: '50%',
          background: 'var(--primary)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}>
          <span style={{ color: '#fff', fontSize: '16px', fontWeight: 700 }}>R</span>
        </div>
        <div>
          <div style={{ fontWeight: 700, color: '#fff', fontSize: '15px' }}>Invest</div>
          <div style={{ fontSize: '12px', color: 'var(--primary)' }}>● Online</div>
        </div>
      </div>

      {/* Messages */}
      <div style={{ flex: 1, overflowY: 'auto', padding: '20px', maxWidth: '720px', width: '100%', margin: '0 auto' }}>
        {allMessages.map((msg, i) => (
          <ChatBubble key={i} message={msg} />
        ))}

        {/* Gráfico inline após o assistente apresentar a alocação */}
        {allocationPreview && !isLoading && (
          <div style={{
            background: 'var(--bg-card)',
            borderRadius: 'var(--radius-md)',
            padding: '16px',
            marginBottom: '12px',
            border: '1px solid var(--border)',
            boxShadow: 'var(--shadow-sm)',
            maxWidth: '78%',
          }}>
            <div style={{ fontSize: '12px', color: 'var(--text-muted)', marginBottom: '12px', fontWeight: 500 }}>
              Sua alocação sugerida
            </div>
            <DonutChart data={allocationPreview} size={140} compact />
          </div>
        )}
        {isLoading && (
          <div style={{ display: 'flex', justifyContent: 'flex-start', marginBottom: '12px' }}>
            <div style={{
              background: 'var(--primary-light)',
              padding: '12px 16px',
              borderRadius: '16px 16px 16px 4px',
              border: '1px solid var(--border)',
            }}>
              <span style={{ color: 'var(--text-muted)', fontSize: '14px' }}>Digitando...</span>
            </div>
          </div>
        )}

        {/* Fallback CTA — shown when save_profile was called but navigation hasn't triggered yet */}
        {profileSaved && !profile && !isLoading && (
          <div style={{
            background: 'var(--primary-light)',
            border: '1px solid var(--primary)',
            borderRadius: 'var(--radius-md)',
            padding: '16px 20px',
            marginBottom: '12px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: '12px',
            flexWrap: 'wrap',
          }}>
            <div>
              <div style={{ fontWeight: 700, color: 'var(--primary)', fontSize: '14px' }}>
                ✅ Perfil salvo com sucesso!
              </div>
              <div style={{ fontSize: '12px', color: 'var(--text-muted)', marginTop: '2px' }}>
                Clique para ver sua alocação recomendada.
              </div>
            </div>
            <button
              onClick={() => navigate('/onboarding/result')}
              style={{
                background: 'var(--primary)', color: '#fff',
                border: 'none', borderRadius: 'var(--radius-sm)',
                padding: '9px 18px', fontSize: '14px', fontWeight: 600,
                cursor: 'pointer', flexShrink: 0,
              }}
            >
              Ver meu perfil →
            </button>
          </div>
        )}
        {error && (
          <div style={{
            background: '#FFF0EF',
            border: '1px solid var(--error)',
            borderRadius: 'var(--radius-sm)',
            padding: '12px',
            color: 'var(--error)',
            fontSize: '14px',
            marginBottom: '12px',
          }}>
            {error}
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>

      {/* Input */}
      <div style={{
        background: 'var(--bg-card)',
        borderTop: '1px solid var(--border)',
        padding: '12px 20px 16px',
      }}>
        <div style={{ maxWidth: '720px', margin: '0 auto' }}>
          {/* Quick reply chips */}
          {suggestedReplies.length > 0 && !isLoading && (
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', marginBottom: '10px' }}>
              {suggestedReplies.map((reply, i) => (
                <button
                  key={i}
                  onClick={() => { sendMessage(reply); }}
                  style={{
                    padding: '6px 14px',
                    borderRadius: '20px',
                    border: '1.5px solid var(--primary)',
                    background: '#fff',
                    color: 'var(--primary)',
                    fontSize: '13px',
                    cursor: 'pointer',
                    fontWeight: 500,
                    transition: 'all 0.15s',
                  }}
                  onMouseEnter={e => {
                    (e.currentTarget as HTMLElement).style.background = 'var(--primary-light)';
                  }}
                  onMouseLeave={e => {
                    (e.currentTarget as HTMLElement).style.background = '#fff';
                  }}
                >
                  {reply}
                </button>
              ))}
            </div>
          )}
          <form onSubmit={handleSubmit} style={{ display: 'flex', gap: '12px' }}>
            <div style={{ flex: 1 }}>
              <Input
                value={input}
                onChange={(e) => setInput(e.target.value)}
                placeholder="Digite sua mensagem..."
                disabled={isLoading}
              />
            </div>
            <Button
              type="submit"
              disabled={!input.trim() || isLoading}
              style={{ width: 'auto', padding: '10px 20px' }}
            >
              Enviar
            </Button>
          </form>
        </div>
      </div>
    </div>
  );
}
