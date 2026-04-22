export interface DonutSlice {
  classe: string;
  percentual: number;
}

interface DonutChartProps {
  data: DonutSlice[];
  size?: number;
  compact?: boolean;
  valorTotal?: number;
  /** Render on a dark background — makes SVG center text and legend text white */
  darkBg?: boolean;
  /** Only render the SVG donut — no side legend */
  noLegend?: boolean;
}

// TapTap Design System — chart palette
export const CLASS_COLORS: Record<string, string> = {
  'RF Dinâmica':          '#3965FF',
  'RF Pós':               '#00B2A9',
  'Fundos imobiliários':  '#8B5CF6',
  'Ações':                '#F7A35C',
  'Internacional':        '#FFCE20',
  'Fundos multimercados': '#EE5D50',
  'Alternativos':         '#A3AED0',
};

// Suggested asset count range per class
const CLASS_ASSET_HINT: Record<string, string> = {
  'Ações':               '5–15 ações',
  'Fundos imobiliários': '5–8 FIIs',
};

const BRL = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 });

function polarToCartesian(cx: number, cy: number, r: number, deg: number) {
  const rad = (deg - 90) * (Math.PI / 180);
  return { x: cx + r * Math.cos(rad), y: cy + r * Math.sin(rad) };
}

function arcPath(cx: number, cy: number, outerR: number, innerR: number, startDeg: number, endDeg: number) {
  const GAP = 1.5;
  const s = startDeg + GAP;
  const e = endDeg - GAP;
  if (e - s < 1) return '';

  const p1 = polarToCartesian(cx, cy, outerR, s);
  const p2 = polarToCartesian(cx, cy, outerR, e);
  const p3 = polarToCartesian(cx, cy, innerR, e);
  const p4 = polarToCartesian(cx, cy, innerR, s);
  const large = e - s > 180 ? 1 : 0;

  return [
    `M ${p1.x.toFixed(2)} ${p1.y.toFixed(2)}`,
    `A ${outerR} ${outerR} 0 ${large} 1 ${p2.x.toFixed(2)} ${p2.y.toFixed(2)}`,
    `L ${p3.x.toFixed(2)} ${p3.y.toFixed(2)}`,
    `A ${innerR} ${innerR} 0 ${large} 0 ${p4.x.toFixed(2)} ${p4.y.toFixed(2)}`,
    'Z',
  ].join(' ');
}

export default function DonutChart({ data, size = 180, compact = false, valorTotal, darkBg = false, noLegend = false }: DonutChartProps) {
  const cx = size / 2;
  const cy = size / 2;
  const outerR = size * 0.42;
  const innerR = size * 0.26;

  // Color tokens that adapt to background
  const textPrimary   = darkBg ? 'rgba(255,255,255,0.95)' : '#2B3674';
  const textSecondary = darkBg ? 'rgba(255,255,255,0.6)'  : '#A3AED0';
  const legendName    = darkBg ? 'rgba(255,255,255,0.9)'  : 'var(--text-body)';
  const legendMuted   = darkBg ? 'rgba(255,255,255,0.6)'  : 'var(--text-muted)';

  let currentDeg = 0;
  const slices = data.map((item) => {
    const startDeg = currentDeg;
    const endDeg = currentDeg + (item.percentual / 100) * 360;
    currentDeg = endDeg;
    return { ...item, startDeg, endDeg };
  });

  // Total for center label when valorTotal provided
  const totalFormatted = valorTotal ? BRL.format(valorTotal) : null;

  return (
    <div style={{
      display: 'flex',
      alignItems: compact ? 'center' : 'flex-start',
      gap: compact ? '16px' : '24px',
      flexWrap: 'wrap',
      justifyContent: noLegend ? 'flex-start' : 'center',
    }}>
      {/* Donut SVG */}
      <svg
        viewBox={`0 0 ${size} ${size}`}
        width={size}
        height={size}
        style={{ flexShrink: 0 }}
      >
        {slices.map(({ classe, startDeg, endDeg }) => (
          <path
            key={classe}
            d={arcPath(cx, cy, outerR, innerR, startDeg, endDeg)}
            fill={CLASS_COLORS[classe] ?? '#9ca3af'}
          />
        ))}
        {totalFormatted ? (
          <>
            <text x={cx} y={cy - 8} textAnchor="middle" fontSize={compact ? 8 : 9} fill={textSecondary} fontWeight="500">
              Total
            </text>
            <text x={cx} y={cy + 6} textAnchor="middle" fontSize={compact ? 8 : 10} fill={textPrimary} fontWeight="700">
              {totalFormatted}
            </text>
          </>
        ) : (
          <>
            <text x={cx} y={cy - 6} textAnchor="middle" fontSize={compact ? 9 : 11} fill={textSecondary} fontWeight="500">
              Carteira
            </text>
            <text x={cx} y={cy + 9} textAnchor="middle" fontSize={compact ? 9 : 11} fill={textSecondary} fontWeight="500">
              ideal
            </text>
          </>
        )}
      </svg>

      {/* Legend — hidden when noLegend=true */}
      {!noLegend && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: compact ? '5px' : '9px' }}>
          {data.map(({ classe, percentual }) => {
            const valor = valorTotal ? (percentual / 100) * valorTotal : null;
            const hint  = CLASS_ASSET_HINT[classe];
            return (
              <div key={classe}>
                {/* Row: color swatch + name + % */}
                <div style={{ display: 'flex', alignItems: 'center', gap: '7px', fontSize: compact ? 12 : 13 }}>
                  <div style={{
                    width: compact ? 10 : 12,
                    height: compact ? 10 : 12,
                    borderRadius: '3px',
                    flexShrink: 0,
                    background: CLASS_COLORS[classe] ?? '#9ca3af',
                  }} />
                  <span style={{ color: legendName }}>{classe}</span>
                  <span style={{ color: legendMuted, marginLeft: 'auto', fontWeight: 600, paddingLeft: '8px' }}>
                    {percentual}%
                  </span>
                </div>

                {/* Sub-row: R$ value + asset hint (only in full mode with valorTotal) */}
                {!compact && (valor !== null || hint) && (
                  <div style={{ paddingLeft: '19px', marginTop: '2px', display: 'flex', gap: '6px', flexWrap: 'wrap', alignItems: 'center' }}>
                    {valor !== null && (
                      <span style={{ fontSize: '11px', fontWeight: 600, color: darkBg ? 'rgba(0,226,220,0.9)' : 'var(--primary)' }}>
                        {BRL.format(valor)}
                      </span>
                    )}
                    {hint && (
                      <span style={{
                        fontSize: '10px',
                        background: darkBg ? 'rgba(0,178,169,0.25)' : 'rgba(0,178,169,0.1)',
                        color: darkBg ? '#00E5DB' : 'var(--primary)',
                        padding: '1px 6px',
                        borderRadius: '4px',
                        fontWeight: 600,
                      }}>
                        {hint}
                      </span>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
