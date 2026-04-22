export interface DonutSlice {
  classe: string;
  percentual: number;
}

interface DonutChartProps {
  data: DonutSlice[];
  /** Diameter of the donut ring (not the total SVG size). Default 180. */
  size?: number;
  /** Small inline variant — just the ring, no labels. */
  compact?: boolean;
  /** Show BRL value in center and in labels. */
  valorTotal?: number;
  /** White text palette for dark backgrounds. */
  darkBg?: boolean;
  /** Donut only — no external labels (used inside hero banners). */
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

const BRL = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 });

function polar(cx: number, cy: number, r: number, deg: number) {
  const rad = (deg - 90) * (Math.PI / 180);
  return { x: cx + r * Math.cos(rad), y: cy + r * Math.sin(rad) };
}

function arcPath(cx: number, cy: number, r: number, startDeg: number, endDeg: number) {
  const GAP = 2.5;
  const s = startDeg + GAP / 2;
  const e = endDeg - GAP / 2;
  if (e - s < 0.5) return '';
  const a = polar(cx, cy, r, s);
  const b = polar(cx, cy, r, e);
  const large = e - s > 180 ? 1 : 0;
  return `M ${a.x.toFixed(2)},${a.y.toFixed(2)} A ${r},${r} 0 ${large} 1 ${b.x.toFixed(2)},${b.y.toFixed(2)}`;
}

export default function DonutChart({
  data,
  size = 180,
  compact = false,
  valorTotal,
  darkBg = false,
  noLegend = false,
}: DonutChartProps) {
  const showLabels = !compact && !noLegend;

  // Ring geometry
  const outerR = size * 0.44;
  const innerR = size * 0.27;
  const midR   = (outerR + innerR) / 2;
  const ringW  = outerR - innerR;

  // SVG canvas: extra padding around donut for labels
  const PAD = showLabels ? 96 : 0;
  const W = size + PAD * 2;
  const H = size + PAD * 2;
  const cx = W / 2;
  const cy = H / 2;

  const textPrimary = darkBg ? 'rgba(255,255,255,0.95)' : '#1B2559';
  const textMuted   = darkBg ? 'rgba(255,255,255,0.55)' : '#A3AED0';

  // Build slice angles
  let deg = 0;
  const slices = data.map(item => {
    const start = deg;
    const end   = deg + (item.percentual / 100) * 360;
    deg = end;
    return { ...item, start, end };
  });

  // Center text sizing
  const centerValueSize = compact ? Math.round(size * 0.09) : Math.round(size * 0.115);
  const centerSubSize   = compact ? Math.round(size * 0.06) : Math.round(size * 0.065);

  return (
    <svg
      viewBox={`0 0 ${W} ${H}`}
      width={W}
      height={H}
      style={{ display: 'block', overflow: 'visible' }}
    >
      {/* ── Slices (stroked arcs → rounded caps automatically) ── */}
      {slices.map(({ classe, start, end }) => (
        <path
          key={classe}
          d={arcPath(cx, cy, midR, start, end)}
          fill="none"
          stroke={CLASS_COLORS[classe] ?? '#9ca3af'}
          strokeWidth={ringW}
          strokeLinecap="round"
        />
      ))}

      {/* ── Center label ── */}
      {valorTotal ? (
        <>
          <text
            x={cx} y={cy + centerValueSize * 0.38}
            textAnchor="middle"
            fontSize={centerValueSize}
            fontWeight="800"
            fill={textPrimary}
            fontFamily="inherit"
          >
            {BRL.format(valorTotal)}
          </text>
          <text
            x={cx} y={cy + centerValueSize * 0.38 + centerSubSize + 3}
            textAnchor="middle"
            fontSize={centerSubSize}
            fontWeight="500"
            fill={textMuted}
            fontFamily="inherit"
          >
            Total
          </text>
        </>
      ) : (
        <>
          <text x={cx} y={cy + 4}  textAnchor="middle" fontSize={centerSubSize} fontWeight="500" fill={textMuted} fontFamily="inherit">Carteira</text>
          <text x={cx} y={cy + 4 + centerSubSize + 2} textAnchor="middle" fontSize={centerSubSize} fontWeight="500" fill={textMuted} fontFamily="inherit">ideal</text>
        </>
      )}

      {/* ── Leader-line labels (Figma style) ── */}
      {showLabels && slices.map(({ classe, percentual, start, end }) => {
        // Skip labels for very small slices to avoid clutter
        if (end - start < 12) return null;

        const midDeg = (start + end) / 2;
        const midRad = (midDeg - 90) * Math.PI / 180;
        const isRight = Math.cos(midRad) >= 0;
        const color = CLASS_COLORS[classe] ?? '#9ca3af';

        // Connector: dot on outer edge → elbow out → horizontal tick → text
        const dot    = polar(cx, cy, outerR + 2, midDeg);
        const elbow  = polar(cx, cy, outerR + 22, midDeg);
        const tickLen = 18;
        const tick   = { x: elbow.x + (isRight ? tickLen : -tickLen), y: elbow.y };
        const textX  = isRight ? tick.x + 5 : tick.x - 5;
        const anchor = isRight ? 'start' : 'end';

        const valStr = valorTotal
          ? BRL.format((percentual / 100) * valorTotal)
          : `${percentual}%`;

        const labelColor     = darkBg ? 'rgba(255,255,255,0.9)'  : '#2B3674';
        const labelSubColor  = darkBg ? 'rgba(255,255,255,0.55)' : '#A3AED0';

        return (
          <g key={`lbl-${classe}`}>
            {/* Dot at outer edge */}
            <circle cx={dot.x} cy={dot.y} r="2.2" fill={color} />

            {/* Polyline connector */}
            <polyline
              points={[dot, elbow, tick].map(p => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ')}
              fill="none"
              stroke={color}
              strokeWidth="1.3"
              strokeLinecap="round"
              strokeLinejoin="round"
            />

            {/* Class name */}
            <text
              x={textX}
              y={elbow.y - 5}
              textAnchor={anchor}
              fontSize="11"
              fontWeight="600"
              fill={labelColor}
              fontFamily="inherit"
            >
              {classe}
            </text>

            {/* Value / percentage */}
            <text
              x={textX}
              y={elbow.y + 8}
              textAnchor={anchor}
              fontSize="10"
              fontWeight="500"
              fill={labelSubColor}
              fontFamily="inherit"
            >
              {valStr}
            </text>
          </g>
        );
      })}
    </svg>
  );
}
