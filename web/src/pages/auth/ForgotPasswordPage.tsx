import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { authService } from '../../services/authService';
import Input from '../../components/ui/Input';
import Button from '../../components/ui/Button';
import AuthLayout from '../../components/layout/AuthLayout';

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [message, setMessage] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    try {
      const response = await authService.forgotPassword(email);
      setMessage(response.message);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <AuthLayout>
      <div>
        <div style={{ marginBottom: '32px' }}>
          <h2 style={{ fontSize: '26px', fontWeight: 800, color: 'var(--text-heading)', margin: '0 0 8px', letterSpacing: '-0.03em' }}>
            Recuperar senha
          </h2>
          <p style={{ color: 'var(--text-muted)', margin: 0, fontSize: '14px' }}>
            Informe seu email e enviaremos as instruções para redefinir sua senha.
          </p>
        </div>

        {message ? (
          <div style={{
            background: 'var(--success-bg)',
            border: '1px solid #86efac',
            borderRadius: 'var(--radius-md)',
            padding: '16px',
            color: '#166534',
            fontSize: '14px',
            lineHeight: 1.5,
          }}>
            {message}
          </div>
        ) : (
          <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <Input
              label="Email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="seu@email.com"
              required
            />
            <Button type="submit" isLoading={isLoading}>
              Enviar instruções
            </Button>
          </form>
        )}

        <div style={{ textAlign: 'center', marginTop: '24px', fontSize: '14px' }}>
          <Link to="/auth/login" style={{ color: 'var(--primary)', textDecoration: 'none', fontWeight: 500 }}>
            ← Voltar para o login
          </Link>
        </div>
      </div>
    </AuthLayout>
  );
}
