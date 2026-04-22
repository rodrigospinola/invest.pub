import { useState, type FormEvent } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { authService } from '../../services/authService';
import { useAuth } from '../../contexts/AuthContext';
import Input from '../../components/ui/Input';
import Button from '../../components/ui/Button';
import AuthLayout from '../../components/layout/AuthLayout';

export default function LoginPage() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [form, setForm] = useState({ email: '', password: '' });
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);
    try {
      const response = await authService.login(form);
      login(response.accessToken, response.user);
      navigate('/dashboard');
    } catch (err: unknown) {
      const axiosError = err as { response?: { data?: { error?: { message?: string } } } };
      setError(axiosError.response?.data?.error?.message || 'Erro ao fazer login. Tente novamente.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <AuthLayout>
      <div>
        <div style={{ marginBottom: '32px' }}>
          <h2 style={{ fontSize: '26px', fontWeight: 800, color: 'var(--text-heading)', margin: '0 0 8px', letterSpacing: '-0.03em' }}>
            Bem-vindo de volta
          </h2>
          <p style={{ color: 'var(--text-muted)', margin: 0, fontSize: '14px' }}>
            Entre na sua conta para continuar
          </p>
        </div>

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <Input
            label="Email"
            type="email"
            value={form.email}
            onChange={(e) => setForm({ ...form, email: e.target.value })}
            placeholder="seu@email.com"
            required
          />
          <Input
            label="Senha"
            type="password"
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
            placeholder="Sua senha"
            required
          />

          {error && (
            <div style={{
              background: 'var(--error-bg)',
              border: '1px solid #fecaca',
              borderRadius: 'var(--radius-sm)',
              padding: '12px 14px',
              color: 'var(--error)',
              fontSize: '14px',
            }}>
              {error}
            </div>
          )}

          <Button type="submit" isLoading={isLoading} style={{ marginTop: '4px' }}>
            Entrar
          </Button>
        </form>

        <div style={{ textAlign: 'center', marginTop: '20px', fontSize: '14px' }}>
          <Link to="/auth/forgot-password" style={{ color: 'var(--primary)', textDecoration: 'none', fontWeight: 500 }}>
            Esqueceu sua senha?
          </Link>
        </div>
        <div style={{
          textAlign: 'center',
          marginTop: '16px',
          fontSize: '14px',
          color: 'var(--text-muted)',
          paddingTop: '20px',
          borderTop: '1px solid var(--border)',
        }}>
          Não tem uma conta?{' '}
          <Link to="/auth/register" style={{ color: 'var(--primary)', fontWeight: 600, textDecoration: 'none' }}>
            Criar conta grátis
          </Link>
        </div>
      </div>
    </AuthLayout>
  );
}
