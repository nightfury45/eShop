import { cn } from "@/lib/utils";

/** Minimal dependency-free SVG sparkline used inside KPI cards. */
export function Sparkline({ data, className }: { data: number[]; className?: string }) {
  const width = 120;
  const height = 32;

  if (data.length === 0) {
    return <svg viewBox={`0 0 ${width} ${height}`} className={className} aria-hidden />;
  }

  const max = Math.max(...data, 1);
  const min = Math.min(...data, 0);
  const range = max - min || 1;
  const step = data.length > 1 ? width / (data.length - 1) : width;

  const points = data
    .map((value, index) => {
      const x = index * step;
      const y = height - ((value - min) / range) * height;
      return `${x.toFixed(1)},${y.toFixed(1)}`;
    })
    .join(" ");

  return (
    <svg viewBox={`0 0 ${width} ${height}`} className={cn(className)} preserveAspectRatio="none" aria-hidden>
      <polyline points={points} fill="none" stroke="currentColor" strokeWidth="1.5" vectorEffect="non-scaling-stroke" />
    </svg>
  );
}
