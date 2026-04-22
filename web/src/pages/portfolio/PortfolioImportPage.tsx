import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import AppLayout from '../../components/layout/AppLayout';
import Button from '../../components/ui/Button';
import Input from '../../components/ui/Input';
import { rankingService } from '../../services/rankingService';
import type { ImportAsset } from '../../types/ranking';
import B3ImportZone from '../../components/portfolio/B3ImportZone';
import type { ParsedAsset } from '../../services/portfolioService';

const CLASSE_OPTIONS = [
  { value: 'Acoes',               label: 'Ações' },
  { value: 'FundosImobiliarios',  label: 'FIIs' },
  { value: 'RFPos',               label: 'Renda Fixa Pós' },
  { value: 'RFDinamica',          label: 'Renda Fixa Dinâmica' },
  { value: 'Internacional',       label: 'Internacional' },
  { value: 'FundosMultimercados', label: 'Multimercados' },
  { value: 'Alternativos',        label: 'Alternativos' },
];

interface AssetForm {
  ticker: string;
  nome: string;
  classe: string;
  quantidade: string;
  precoMedio: string;
}

const EMPTY_ASSET: AssetForm = {
  ticker: '', nome: '', classe: 'Acoes', quantidade: '', precoMedio: '',
};

export default function PortfolioImportPage() {
  const navigate = useNavigate();
  const [ativos, setAtivos] = useState<AssetForm[]>([{ ...EMPTY_ASSET }]);
  const [isLoading, setIsLoading] = useState(false);
  const [globalError, setGlobalError] = useState('');
  const [fieldErrors, setFieldErrors] = useState<Record<number, Partial<AssetForm>>>({});
  // Controla banner de aviso pós-import B3 (sem preços)
  const [b3ImportedCount, setB3ImportedCount] = useState<number | null>(null);

  const updateAtivo = (index: number, field: keyof AssetForm, value: string) => {
    setAtivos((prev) => prev.map((a, i) => (i === index ? { ...a, [field]: value } : a)));
    // Limpa o erro do campo ao editar
    setFieldErrors((prev) => {
      const next = { ...prev };
      if (next[index]) {
        const { [field]: _, ...rest } = next[index];
        next[index] = rest;
      }
      return next;
    });
  };

  const addAtivo = () => {
    setAtivos((prev) => [...prev, { ...EMPTY_ASSET }]);
    setB3ImportedCount(null);
  };

  const removeAtivo = (index: number) => {
    setAtivos((prev) => prev.filter((_, i) => i !== index));
    setFieldErrors((prev) => {
      const next: Record<number, Partial<AssetForm>> = {};
      Object.entries(prev).forEach(([k, v]) => {
        const n = parseInt(k);
        if (n < index) next[n] = v;
        else if (n > index) next[n - 1] = v;
      });
      return next;
    });
  };

  const handleB3ImportSuccess = (parsedAssets: ParsedAsset[]) => {
    const formAssets: AssetForm[] = parsedAssets.map((a) => ({
      ticker:     a.ticker,
      nome:       a.ticker,           // Nome = ticker por padrão; usuário pode corrigir
      classe:     a.classe,
      quantidade: a.quantidade.toString(),
      // Pre-fill price when the Excel had a "Valor de Fechamento" column
      precoMedio: a.precoMedio != null && a.precoMedio > 0 ? a.precoMedio.toString() : '',
    }));

    setAtivos(formAssets);
    setFieldErrors({});              // Não validar imediatamente — mostrar banner orientador
    setB3ImportedCount(formAssets.length);
    setGlobalError('');
  };

  const validate = (currentAtivos: AssetForm[] = ativos): boolean => {
    const errors: Record<number, Partial<AssetForm>> = {};
    currentAtivos.forEach((a, i) => {
      const e: Partial<AssetForm> = {};
      if (!a.ticker.trim())   e.ticker   = 'Obrigatório';
      if (!a.nome.trim())     e.nome     = 'Obrigatório';
      if (!a.quantidade || isNaN(parseFloat(a.quantidade)) || parseFloat(a.quantidade) <= 0)
        e.quantidade = 'Quantidade inválida';
      if (!a.precoMedio || isNaN(parseFloat(a.precoMedio)) || parseFloat(a.precoMedio) <= 0)
        e.precoMedio = 'Informe o preço médio';
      if (Object.keys(e).length > 0) errors[i] = e;
    });
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async () => {
    setGlobalError('');
    if (!validate()) {
      // Conta quantos campos de preço estão faltando para mensagem específica
      const semPreco = ativos.filter(
        (a) => !a.precoMedio || isNaN(parseFloat(a.precoMedio)) || parseFloat(a.precoMedio) <= 0
      ).length;
      if (semPreco > 0) {
        setGlobalError(
          `Preencha o preço médio de ${semPreco} ativo${semPreco > 1 ? 's' : ''} para continuar.`
        );
      }
      return;
    }

    setIsLoading(true);
    try {
      const payload: ImportAsset[] = ativos.map((a) => ({
        ticker:     a.ticker.trim().toUpperCase(),
        nome:       a.nome.trim(),
        classe:     a.classe,
        quantidade: parseFloat(a.quantidade),
        precoMedio: parseFloat(a.precoMedio),
      }));

      const result = await rankingService.importPortfolio(payload);
      navigate('/portfolio/confirm', { state: result });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Erro ao importar carteira.';
      setGlobalError(message);
    } finally {
      setIsLoading(false);
    }
  };

  // Quantos ativos já têm preço preenchido
  const ativosComPreco  = ativos.filter((a) => parseFloat(a.precoMedio) > 0).length;
  const ativosSemPreco  = ativos.length - ativosComPreco;
  const valorEstimado   = ativos.reduce((acc, a) => {
    return acc + (parseFloat(a.quantidade) || 0) * (parseFloat(a.precoMedio) || 0);
  }, 0);

  return (
    <AppLayout title="Importar Carteira">
      <div style={{ maxWidth: '720px' }}>

        {/* Descrição */}
        <p style={{ color: 'var(--text-muted)', fontSize: '14px', margin: '0 0 28px' }}>
          Importe seu extrato da B3 para preencher tickers e quantidades automaticamente,
          ou adicione os ativos manualmente.
        </p>

        {/* Zone de upload */}
        <B3ImportZone onImportSuccess={handleB3ImportSuccess} />

        {/* Banner pós-import B3 */}
        {b3ImportedCount !== null && (
          <div style={{
            background: '#EFF8FF',
            border: '1px solid #3965FF',
            borderRadius: 'var(--radius-md)',
            padding: '14px 16px',
            marginTop: '16px',
            marginBottom: '8px',
            display: 'flex',
            gap: '12px',
            alignItems: 'flex-start',
          }}>
            <span style={{ fontSize: '20px', flexShrink: 0 }}>✅</span>
            <div>
              <p style={{ fontSize: '14px', fontWeight: 600, color: '#3965FF', margin: '0 0 4px' }}>
                {b3ImportedCount} ativo{b3ImportedCount > 1 ? 's' : ''} importado{b3ImportedCount > 1 ? 's' : ''} com sucesso!
              </p>
              <p style={{ fontSize: '13px', color: 'var(--text-body)', margin: 0, lineHeight: 1.5 }}>
                {ativosSemPreco > 0 ? (
                  <>
                    O preço de fechamento foi preenchido automaticamente quando disponível.{' '}
                    <strong>Confira e preencha o "Preço médio" dos {ativosSemPreco} ativo{ativosSemPreco > 1 ? 's' : ''} pendente{ativosSemPreco > 1 ? 's' : ''}</strong> com o valor da sua corretora.
                  </>
                ) : (
                  <>Preços preenchidos a partir do valor de fechamento. Confirme os valores e clique em importar.</>
                )}
              </p>
            </div>
          </div>
        )}

        {/* Separador */}
        <div style={{ display: 'flex', alignItems: 'center', margin: '28px 0 20px' }}>
          <div style={{ flex: 1, height: '1px', background: 'var(--border)' }} />
          <span style={{ padding: '0 14px', color: 'var(--text-muted)', fontSize: '13px', fontWeight: 500 }}>
            {ativos.length > 1 || b3ImportedCount !== null
              ? `${ativos.length} ativo${ativos.length > 1 ? 's' : ''} na lista`
              : 'Ou adicione manualmente'}
          </span>
          <div style={{ flex: 1, height: '1px', background: 'var(--border)' }} />
        </div>

        {/* Resumo de progresso (só quando tem ativos suficientes) */}
        {ativos.length > 1 && (
          <div style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(3, 1fr)',
            gap: '10px',
            marginBottom: '20px',
          }}>
            <SummaryPill label="Total de ativos" value={ativos.length.toString()} color="var(--primary)" />
            <SummaryPill
              label="Preços pendentes"
              value={ativosSemPreco.toString()}
              color={ativosSemPreco > 0 ? 'var(--warning)' : 'var(--success)'}
            />
            <SummaryPill
              label="Valor estimado"
              value={valorEstimado > 0
                ? new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 }).format(valorEstimado)
                : '—'
              }
              color="var(--text-heading)"
            />
          </div>
        )}

        {/* Lista de ativos */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', marginBottom: '16px' }}>
          {ativos.map((ativo, index) => (
            <AssetRow
              key={index}
              index={index}
              ativo={ativo}
              errors={fieldErrors[index] || {}}
              canRemove={ativos.length > 1}
              needsPrice={b3ImportedCount !== null && !ativo.precoMedio}
              onChange={updateAtivo}
              onRemove={removeAtivo}
            />
          ))}
        </div>

        {/* Botão adicionar ativo */}
        <button
          type="button"
          onClick={addAtivo}
          style={{
            width: '100%',
            padding: '13px',
            border: '2px dashed var(--border)',
            borderRadius: 'var(--radius-md)',
            background: 'none',
            color: 'var(--text-muted)',
            cursor: 'pointer',
            fontSize: '14px',
            fontWeight: 500,
            marginBottom: '24px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: '8px',
            transition: 'all 0.15s',
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.borderColor = 'var(--primary)';
            e.currentTarget.style.color = 'var(--primary)';
            e.currentTarget.style.background = 'var(--primary-light)';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.borderColor = 'var(--border)';
            e.currentTarget.style.color = 'var(--text-muted)';
            e.currentTarget.style.background = 'none';
          }}
        >
          <span style={{ fontSize: '18px' }}>+</span> Adicionar ativo manualmente
        </button>

        {/* Erro global */}
        {globalError && (
          <div style={{
            background: '#FEF2F2',
            border: '1px solid var(--error)',
            borderRadius: 'var(--radius-md)',
            padding: '12px 16px',
            marginBottom: '16px',
            color: 'var(--error)',
            fontSize: '14px',
            display: 'flex',
            gap: '8px',
          }}>
            <span>⚠️</span>
            <span>{globalError}</span>
          </div>
        )}

        {/* CTA principal */}
        <Button onClick={handleSubmit} isLoading={isLoading} size="lg">
          {isLoading ? 'Salvando...' : `Importar ${ativos.length} ativo${ativos.length > 1 ? 's' : ''} →`}
        </Button>

        <div style={{ display: 'flex', justifyContent: 'center', gap: '20px', marginTop: '14px' }}>
          <button
            type="button"
            onClick={() => navigate('/suggestion')}
            style={{ background: 'none', border: 'none', color: 'var(--primary)', cursor: 'pointer', fontSize: '14px', fontWeight: 500 }}
          >
            ← Ver sugestão
          </button>
          <button
            type="button"
            onClick={() => navigate('/dashboard')}
            style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer', fontSize: '14px' }}
          >
            Pular por agora
          </button>
        </div>
      </div>
    </AppLayout>
  );
}

// ─── SummaryPill ─────────────────────────────────────────────────────────────

function SummaryPill({ label, value, color }: { label: string; value: string; color: string }) {
  return (
    <div style={{
      background: 'var(--bg-card)',
      border: '1px solid var(--border)',
      borderRadius: 'var(--radius-md)',
      padding: '12px 14px',
      textAlign: 'center',
      boxShadow: 'var(--shadow-sm)',
    }}>
      <div style={{ fontSize: '16px', fontWeight: 700, color, marginBottom: '2px' }}>{value}</div>
      <div style={{ fontSize: '11px', color: 'var(--text-muted)', fontWeight: 500 }}>{label}</div>
    </div>
  );
}

// ─── AssetRow ─────────────────────────────────────────────────────────────────

interface AssetRowProps {
  index: number;
  ativo: AssetForm;
  errors: Partial<AssetForm>;
  canRemove: boolean;
  needsPrice: boolean;
  onChange: (i: number, field: keyof AssetForm, value: string) => void;
  onRemove: (i: number) => void;
}

function AssetRow({ index, ativo, errors, canRemove, needsPrice, onChange, onRemove }: AssetRowProps) {
  const hasPreco = parseFloat(ativo.precoMedio) > 0;

  return (
    <div style={{
      background: 'var(--bg-card)',
      borderRadius: 'var(--radius-md)',
      border: `1px solid ${needsPrice && !hasPreco ? 'var(--warning)' : 'var(--border)'}`,
      boxShadow: 'var(--shadow-sm)',
      overflow: 'hidden',
    }}>
      {/* Header do card */}
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        padding: '12px 16px',
        borderBottom: '1px solid var(--border)',
        background: 'var(--bg-page)',
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <div style={{
            width: '28px', height: '28px',
            borderRadius: '50%',
            background: 'var(--primary-light)',
            color: 'var(--primary)',
            fontSize: '12px',
            fontWeight: 700,
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            flexShrink: 0,
          }}>
            {index + 1}
          </div>
          <span style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-heading)' }}>
            {ativo.ticker || 'Novo ativo'}
          </span>
          {needsPrice && !hasPreco && (
            <span style={{
              background: '#FFFBEB',
              color: 'var(--warning)',
              border: '1px solid var(--warning)',
              fontSize: '11px',
              fontWeight: 600,
              padding: '1px 7px',
              borderRadius: '20px',
            }}>
              Preço pendente
            </span>
          )}
          {hasPreco && (
            <span style={{
              background: 'var(--success-bg, #E6FBF0)',
              color: 'var(--success)',
              fontSize: '11px',
              fontWeight: 600,
              padding: '1px 7px',
              borderRadius: '20px',
            }}>
              ✓ Completo
            </span>
          )}
        </div>
        {canRemove && (
          <button
            type="button"
            onClick={() => onRemove(index)}
            style={{
              background: 'none', border: 'none',
              color: 'var(--text-muted)', cursor: 'pointer',
              fontSize: '13px', padding: '2px 8px',
              borderRadius: 'var(--radius-sm)',
              transition: 'color 0.1s',
            }}
            onMouseEnter={(e) => (e.currentTarget.style.color = 'var(--error)')}
            onMouseLeave={(e) => (e.currentTarget.style.color = 'var(--text-muted)')}
          >
            ✕ Remover
          </button>
        )}
      </div>

      {/* Campos */}
      <div style={{ padding: '14px 16px', display: 'flex', flexDirection: 'column', gap: '10px' }}>

        {/* Linha 1: Ticker + Nome */}
        <div style={{ display: 'grid', gridTemplateColumns: '120px 1fr', gap: '10px' }}>
          <Input
            label="Ticker *"
            placeholder="PETR4"
            value={ativo.ticker}
            onChange={(e) => onChange(index, 'ticker', e.target.value.toUpperCase())}
            error={errors.ticker}
          />
          <Input
            label="Nome / Descrição *"
            placeholder="Ex: Petrobras PN"
            value={ativo.nome}
            onChange={(e) => onChange(index, 'nome', e.target.value)}
            error={errors.nome}
          />
        </div>

        {/* Linha 2: Classe */}
        <div>
          <label style={{
            fontSize: '13px', fontWeight: 500, color: 'var(--text-heading)',
            display: 'block', marginBottom: '4px',
          }}>
            Classe *
          </label>
          <select
            value={ativo.classe}
            onChange={(e) => onChange(index, 'classe', e.target.value)}
            style={{
              width: '100%',
              padding: '10px 12px',
              border: '1px solid var(--border)',
              borderRadius: 'var(--radius-md)',
              fontSize: '14px',
              background: 'var(--bg-card)',
              color: 'var(--text-body)',
              cursor: 'pointer',
              outline: 'none',
              appearance: 'none',
              backgroundImage: `url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 24 24' fill='none' stroke='%23A3AED0' stroke-width='2'%3E%3Cpolyline points='6 9 12 15 18 9'%3E%3C/polyline%3E%3C/svg%3E")`,
              backgroundRepeat: 'no-repeat',
              backgroundPosition: 'right 12px center',
              paddingRight: '32px',
            }}
          >
            {CLASSE_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>{opt.label}</option>
            ))}
          </select>
        </div>

        {/* Linha 3: Quantidade + Preço médio */}
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px' }}>
          <Input
            label="Quantidade *"
            type="number"
            min="0"
            step="any"
            placeholder="100"
            value={ativo.quantidade}
            onChange={(e) => onChange(index, 'quantidade', e.target.value)}
            error={errors.quantidade}
          />

          {/* Preço médio com destaque especial quando necessário */}
          <div>
            <label style={{
              fontSize: '13px', fontWeight: 600,
              color: needsPrice && !hasPreco ? 'var(--warning)' : 'var(--text-heading)',
              display: 'block', marginBottom: '4px',
            }}>
              Preço médio (R$) *
              {needsPrice && !hasPreco && (
                <span style={{ fontSize: '11px', fontWeight: 400, marginLeft: '4px' }}>
                  — informe o valor da sua corretora
                </span>
              )}
            </label>
            <input
              type="number"
              min="0"
              step="0.01"
              placeholder="Ex: 28.50"
              value={ativo.precoMedio}
              onChange={(e) => onChange(index, 'precoMedio', e.target.value)}
              style={{
                width: '100%',
                padding: '10px 12px',
                border: `1px solid ${
                  errors.precoMedio ? 'var(--error)'
                  : needsPrice && !hasPreco ? 'var(--warning)'
                  : 'var(--border)'
                }`,
                borderRadius: 'var(--radius-md)',
                fontSize: '14px',
                outline: 'none',
                boxSizing: 'border-box',
                background: needsPrice && !hasPreco && !errors.precoMedio ? '#FFFBEB' : 'var(--bg-card)',
                color: 'var(--text-body)',
                transition: 'border-color 0.15s',
              }}
              onFocus={(e) => (e.target.style.borderColor = 'var(--primary)')}
              onBlur={(e) => {
                e.target.style.borderColor = errors.precoMedio
                  ? 'var(--error)'
                  : needsPrice && !hasPreco
                    ? 'var(--warning)'
                    : 'var(--border)';
              }}
            />
            {errors.precoMedio && (
              <span style={{ fontSize: '12px', color: 'var(--error)', marginTop: '3px', display: 'block' }}>
                {errors.precoMedio}
              </span>
            )}
          </div>
        </div>

        {/* Valor estimado do ativo */}
        {parseFloat(ativo.quantidade) > 0 && parseFloat(ativo.precoMedio) > 0 && (
          <div style={{
            padding: '8px 12px',
            background: 'var(--primary-light)',
            borderRadius: 'var(--radius-sm)',
            fontSize: '12px',
            color: 'var(--primary)',
            fontWeight: 600,
          }}>
            Valor estimado:{' '}
            {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(
              parseFloat(ativo.quantidade) * parseFloat(ativo.precoMedio)
            )}
          </div>
        )}
      </div>
    </div>
  );
}
