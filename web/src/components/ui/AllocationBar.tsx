import { useEffect, useState } from 'react';

interface AllocationBarProps {
  classe: string;
  percentual: number;
  color?: string;
  delay?: number;
}

const CLASS_COLORS: Record<string, string> = {
  'RF Dinâmica': '#3965FF',
  'RF Pós': '#00B2A9',
  'Fundos imobiliários': '#F7A35C',
  'Ações': '#00B2A9',
  'Internacional': '#FFCE20',
  'Fundos multimercados': '#EE5D50',
  'Alternativos': '#A3AED0',
};

export default function AllocationBar({ classe, percentual, color, delay = 0 }: AllocationBarProps) {
  const barColor = color ?? CLASS_COLORS[classe] ?? '#A3AED0';
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    const timer = setTimeout(() => setVisible(true), delay);
    return () => clearTimeout(timer);
  }, [delay]);

  return (
    <div style={{ marginBottom: '14px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '6px' }}>
        <span style={{ fontSize: '13px', color: 'var(--text-body)', fontWeight: 500 }}>{classe}</span>
        <span style={{ fontSize: '13px', color: 'var(--text-heading)', fontWeight: 700 }}>{percentual}%</span>
      </div>
      <div style={{ height: '8px', background: 'var(--border)', borderRadius: '4px', overflow: 'hidden' }}>
        <div
          style={{
            height: '100%',
            width: visible ? `${percentual}%` : '0%',
            background: barColor,
            borderRadius: '4px',
            transition: 'width 0.6s ease',
          }}
        />
      </div>
    </div>
  );
}
