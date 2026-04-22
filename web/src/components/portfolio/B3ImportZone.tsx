import { useState, useRef } from 'react';
import type { ChangeEvent, DragEvent } from 'react';
import { portfolioService, type ParsedAsset } from '../../services/portfolioService';

interface B3ImportZoneProps {
  onImportSuccess: (assets: ParsedAsset[]) => void;
}

export default function B3ImportZone({ onImportSuccess }: B3ImportZoneProps) {
  const [isHovering, setIsHovering] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleDragOver = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsHovering(true);
  };

  const handleDragLeave = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsHovering(false);
  };

  const handleDrop = async (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsHovering(false);
    const files = e.dataTransfer.files;
    if (files.length > 0) await processFile(files[0]);
  };

  const handleFileSelect = async (e: ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      await processFile(e.target.files[0]);
    }
  };

  const processFile = async (file: File) => {
    setError(null);
    if (!file.name.endsWith('.xlsx')) {
      setError('Envie um arquivo .xlsx da B3 (Excel de posição consolidada).');
      return;
    }
    setIsLoading(true);
    try {
      const result = await portfolioService.importB3(file);
      if (result.parsedAssets.length === 0) {
        setError('Nenhum ativo encontrado no arquivo. Verifique se é o arquivo correto da B3.');
        return;
      }
      onImportSuccess(result.parsedAssets);
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { error?: { message?: string } } } };
      setError(
        axiosErr.response?.data?.error?.message ||
        'Falha ao processar a planilha. Certifique-se de que é o arquivo de Posição Consolidada da B3.'
      );
    } finally {
      setIsLoading(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  return (
    <div style={{ marginBottom: '8px' }}>
      <div
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        onClick={() => !isLoading && fileInputRef.current?.click()}
        style={{
          border: `2px dashed ${isHovering ? 'var(--primary)' : 'var(--border)'}`,
          borderRadius: 'var(--radius-lg)',
          padding: '32px 24px',
          textAlign: 'center',
          background: isHovering ? 'var(--primary-light)' : 'var(--bg-card)',
          transition: 'all 0.2s ease',
          cursor: isLoading ? 'wait' : 'pointer',
          boxShadow: 'var(--shadow-sm)',
        }}
      >
        {/* Ícone */}
        <div style={{
          width: '56px', height: '56px',
          borderRadius: '50%',
          background: 'var(--primary-light)',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          margin: '0 auto 16px',
          fontSize: '24px',
        }}>
          {isLoading ? '⏳' : '📥'}
        </div>

        <h3 style={{ fontSize: '15px', fontWeight: 700, color: 'var(--text-heading)', margin: '0 0 6px' }}>
          {isLoading ? 'Processando arquivo...' : 'Importe seu extrato da B3'}
        </h3>
        <p style={{ fontSize: '13px', color: 'var(--text-muted)', margin: '0 0 20px', lineHeight: 1.5 }}>
          Arraste e solte o arquivo Excel aqui, ou clique para selecionar.{' '}
          <span style={{ color: 'var(--primary)', fontWeight: 500 }}>Posição Consolidada → Exportar Excel</span>
        </p>

        <button
          type="button"
          disabled={isLoading}
          onClick={(e) => { e.stopPropagation(); fileInputRef.current?.click(); }}
          style={{
            background: isLoading ? 'var(--border)' : 'var(--primary)',
            color: isLoading ? 'var(--text-muted)' : '#fff',
            border: 'none',
            borderRadius: 'var(--radius-md)',
            padding: '10px 24px',
            fontSize: '14px',
            fontWeight: 600,
            cursor: isLoading ? 'wait' : 'pointer',
            transition: 'background 0.15s',
          }}
        >
          {isLoading ? 'Aguarde...' : 'Selecionar arquivo .xlsx'}
        </button>

        <input
          type="file"
          ref={fileInputRef}
          onChange={handleFileSelect}
          style={{ display: 'none' }}
          accept=".xlsx"
        />
      </div>

      {error && (
        <div style={{
          padding: '12px 16px',
          background: 'var(--error-bg, #FEF2F2)',
          border: '1px solid var(--error)',
          borderRadius: 'var(--radius-md)',
          color: 'var(--error)',
          fontSize: '13px',
          marginTop: '10px',
          display: 'flex',
          gap: '8px',
          alignItems: 'flex-start',
        }}>
          <span>⚠️</span>
          <span>{error}</span>
        </div>
      )}
    </div>
  );
}
