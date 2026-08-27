interface IconeProps {
  className?: string;
}

/** Icones inspirados nos cards da referencia visual (traco fino, 2px). */
const base = {
  width: 44,
  height: 44,
  viewBox: '0 0 44 44',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 2,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
};

export function IconeSaldo({ className }: IconeProps) {
  return (
    <svg {...base} className={className} aria-hidden="true">
      <rect x="9" y="9" width="26" height="26" rx="3" />
      <rect
        x="3"
        y="3"
        width="38"
        height="38"
        rx="4"
        strokeDasharray="3 5"
        opacity="0.7"
      />
    </svg>
  );
}

export function IconeMovimentacao({ className }: IconeProps) {
  return (
    <svg {...base} className={className} aria-hidden="true">
      <path d="M12 14h20l-5-5" />
      <path d="M32 30H12l5 5" />
    </svg>
  );
}

export function IconeResumo({ className }: IconeProps) {
  return (
    <svg {...base} className={className} aria-hidden="true">
      <path d="M6 34V20" />
      <path d="M16 34V10" />
      <path d="M26 34V25" />
      <path d="M36 34V15" />
      <path d="M4 40h36" opacity="0.6" />
    </svg>
  );
}

export function IconeConta({ className }: IconeProps) {
  return (
    <svg {...base} className={className} aria-hidden="true">
      <path d="M5 16l17-9 17 9" />
      <path d="M9 16v16" />
      <path d="M18 16v16" />
      <path d="M26 16v16" />
      <path d="M35 16v16" />
      <path d="M5 36h34" />
    </svg>
  );
}

export function IconeHistorico({ className }: IconeProps) {
  return (
    <svg {...base} className={className} aria-hidden="true">
      <rect x="5" y="8" width="34" height="28" rx="3" />
      <path d="M12 18l5 4-5 4" />
      <path d="M22 26h10" />
    </svg>
  );
}
