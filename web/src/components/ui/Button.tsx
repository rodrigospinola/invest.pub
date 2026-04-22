import { type ButtonHTMLAttributes } from 'react';

type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger';
type ButtonSize = 'sm' | 'md' | 'lg';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  isLoading?: boolean;
}

const VARIANT_STYLES: Record<ButtonVariant, React.CSSProperties> = {
  primary: {
    background: '#00B2A9',
    color: '#fff',
    border: 'none',
  },
  secondary: {
    background: '#fff',
    color: '#00B2A9',
    border: '1.5px solid #00B2A9',
  },
  ghost: {
    background: 'transparent',
    color: '#A3AED0',
    border: 'none',
  },
  danger: {
    background: '#EE5D50',
    color: '#fff',
    border: 'none',
  },
};

const SIZE_STYLES: Record<ButtonSize, React.CSSProperties> = {
  sm: { padding: '7px 16px', fontSize: '13px' },
  md: { padding: '11px 24px', fontSize: '14px' },
  lg: { padding: '14px 32px', fontSize: '15px' },
};

const HOVER_BG: Record<ButtonVariant, string> = {
  primary: '#009E96',
  secondary: '#E6F8F7',
  ghost: 'rgba(163,174,208,0.1)',
  danger: '#dc4a3d',
};

const Spinner = () => (
  <svg
    width="16"
    height="16"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2.5"
    strokeLinecap="round"
    style={{ animation: 'spin 0.75s linear infinite', flexShrink: 0 }}
  >
    <path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83" />
    <style>{`@keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }`}</style>
  </svg>
);

export default function Button({
  variant = 'primary',
  size = 'md',
  isLoading,
  children,
  disabled,
  style,
  ...props
}: ButtonProps) {
  return (
    <button
      disabled={disabled || isLoading}
      style={{
        ...VARIANT_STYLES[variant],
        ...SIZE_STYLES[size],
        borderRadius: '12px',
        fontWeight: 600,
        cursor: disabled || isLoading ? 'not-allowed' : 'pointer',
        opacity: disabled || isLoading ? 0.6 : 1,
        width: '100%',
        transition: 'background 0.15s, opacity 0.15s',
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        gap: '8px',
        letterSpacing: '-0.01em',
        ...style,
      }}
      onMouseEnter={e => {
        if (!disabled && !isLoading) {
          (e.currentTarget as HTMLElement).style.background = HOVER_BG[variant];
        }
      }}
      onMouseLeave={e => {
        if (!disabled && !isLoading) {
          (e.currentTarget as HTMLElement).style.background = VARIANT_STYLES[variant].background as string;
        }
      }}
      {...props}
    >
      {isLoading && <Spinner />}
      {isLoading ? 'Carregando...' : children}
    </button>
  );
}
