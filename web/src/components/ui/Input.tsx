import { type InputHTMLAttributes, forwardRef, useState } from 'react';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
}

const Input = forwardRef<HTMLInputElement, InputProps>(({ label, error, style, ...props }, ref) => {
  const [focused, setFocused] = useState(false);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
      {label && (
        <label style={{
          fontSize: '14px',
          fontWeight: 500,
          color: '#2B3674',
        }}>
          {label}
        </label>
      )}
      <input
        ref={ref}
        style={{
          padding: '12px 16px',
          border: `1.5px solid ${error ? '#EE5D50' : focused ? '#00B2A9' : '#E9EDF7'}`,
          borderRadius: '12px',
          fontSize: '14px',
          outline: 'none',
          width: '100%',
          boxSizing: 'border-box',
          color: '#1B2559',
          background: '#fff',
          boxShadow: focused ? '0 0 0 3px rgba(0,178,169,0.15)' : 'none',
          transition: 'border-color 0.15s, box-shadow 0.15s',
          ...style,
        }}
        onFocus={e => { setFocused(true); props.onFocus?.(e); }}
        onBlur={e => { setFocused(false); props.onBlur?.(e); }}
        {...props}
      />
      {error && (
        <span style={{ fontSize: '12px', color: '#EE5D50', fontWeight: 500 }}>{error}</span>
      )}
    </div>
  );
});

Input.displayName = 'Input';
export default Input;
