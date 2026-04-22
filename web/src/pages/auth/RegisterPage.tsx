import { useState, type FormEvent } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { authService } from '../../services/authService';
import { useAuth } from '../../contexts/AuthContext';
import Input from '../../components/ui/Input';
import Button from '../../components/ui/Button';
import AuthLayout from '../../components/layout/AuthLayout';

export default function RegisterPage() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [form, setForm] = useState({ nome: '', email: '', password: '', confirmPassword: '', telefone: '' });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(false);

  const validate = () => {
    const newErrors: Record<string, string> = {};
    if (!form.nome.trim()) newErrors.nome = 'Nome é obrigatório.';
    if (!form.email.trim()) newErrors.email = 'Email é obrigatório.';
    if (form.password.length < 8) newErrors.password = 'Senha deve ter no mínimo 8 caracteres.';
    if (form.password !== form.confirmPassword) newErrors.confirmPassword = 'Senhas não conferem.';
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!validate()) return;
    setIsLoading(true);
    try {
      const response = await authService.register({
        nome: form.nome,
        email: form.email,
        password: form.password,
        telefone: form.telefone || undefined,
      });
      login(response.accessToken, response.user);
      navigate('/dashboard');
    } catch (err: unknown) {
      const axiosError = err as { response?: { data?: { error?: { message?: string; field?: string } } } };
      const apiError = axiosError.response?.data?.error;
      if (apiError?.field) {
        setErrors({ [apiError.field.toLowerCase()]: apiError.message || 'Erro de validação.' });
      } else {
        setErrors({ general: apiError?.message || 'Erro ao criar conta. Tente novamente.' });
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <AuthLayout>
      <div>
        <div style={{ marginBottom: '28px' }}>
          <h2 style={{ fontSize: '26px', fontWeight: 800, color: 'var(--text-heading)', margin: '0 0 8px', letterSpacing: '-0.03em' }}>
            Crie sua conta
          </h2>
          <p style={{ color: 'var(--text-muted)', margin: 0, fontSize: '14px' }}>
            Comece sua jornada rumo à independência financeira
          </p>
        </div>

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          <Input label="Nome completo" value={form.nome} onChange={(e) => setForm({ ...form, nome: e.target.value })} error={errors.nome} required />
          <Input label="Email" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} error={errors.email} placeholder="seu@email.com" required />
          <Input label="Telefone (opcional)" type="tel" value={form.telefone} onChange={(e) => setForm({ ...form, telefone: e.target.value })} placeholder="(11) 99999-9999" />
          <Input label="Senha" type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} error={errors.password} placeholder="Mínimo 8 caracteres" required />
          <Input label="Confirmar senha" type="password" value={form.confirmPassword} onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })} error={errors.confirmPassword} placeholder="Repita a senha" required />

          {errors.general && (
            <div style={{
              background: 'var(--error-bg)',
              border: '1px solid #fecaca',
              borderRadius: 'var(--radius-sm)',
              padding: '12px 14px',
              color: 'var(--error)',
              fontSize: '14px',
            }}>
              {errors.general}
            </div>
          )}

          <Button type="submit" isLoading={isLoading} style={{ marginTop: '4px' }}>
            Criar conta
          </Button>
        </form>

        <div style={{
          textAlign: 'center',
          marginTop: '20px',
          fontSize: '14px',
          color: 'var(--text-muted)',
          paddingTop: '20px',
          borderTop: '1px solid var(--border)',
        }}>
          Já tem uma conta?{' '}
          <Link to="/auth/login" style={{ color: 'var(--primary)', fontWeight: 600, textDecoration: 'none' }}>
            Fazer login
          </Link>
        </div>
      </div>
    </AuthLayout>
  );
}
