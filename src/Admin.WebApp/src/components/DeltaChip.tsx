import { ArrowDown, ArrowUp } from "lucide-react";
import { cn } from "@/lib/utils";

/**
 * Figma component: DeltaChip. A signed trend chip — green tint/up for non-negative deltas,
 * red tint/down for negative. Used by KPI cards.
 */
export interface DeltaChipProps {
  /** The delta value, e.g. 12.4 renders "+12.4%". */
  value: number;
  /** Unit suffix, defaults to "%". */
  unit?: string;
  className?: string;
}

export function DeltaChip({ value, unit = "%", className }: DeltaChipProps) {
  const positive = value >= 0;
  const Icon = positive ? ArrowUp : ArrowDown;
  const formatted = `${positive ? "+" : ""}${value.toFixed(1)}${unit}`;

  return (
    <span
      className={cn(
        "inline-flex items-center gap-0.5 rounded-md px-1.5 py-0.5 text-xs font-medium tabular-nums",
        positive ? "bg-positive-tint text-positive" : "bg-destructive/10 text-destructive",
        className,
      )}
    >
      <Icon className="size-3" aria-hidden />
      {formatted}
    </span>
  );
}
