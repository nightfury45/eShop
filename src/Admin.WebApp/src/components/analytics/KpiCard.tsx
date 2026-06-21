import { Card } from "@/components/ui/card";
import { DeltaChip } from "@/components/DeltaChip";
import { Sparkline } from "@/components/Sparkline";
import type { KpiValue } from "@/lib/api";
import { cn } from "@/lib/utils";

export interface KpiCardProps {
  label: string;
  value: string;
  kpi: KpiValue;
  /** Tailwind text-color class for the sparkline (e.g. text-positive). */
  sparkClassName?: string;
}

export function KpiCard({ label, value, kpi, sparkClassName }: KpiCardProps) {
  return (
    <Card className="p-5">
      <p className="text-[11px] font-medium uppercase tracking-widest text-muted-foreground">{label}</p>
      <p className="mt-2 text-3xl font-semibold tracking-tight tabular-nums text-foreground">{value}</p>
      <div className="mt-2 flex items-center gap-2">
        <DeltaChip value={kpi.deltaPercent} />
        <span className="text-xs text-muted-foreground">vs prev. period</span>
      </div>
      <Sparkline data={kpi.spark} className={cn("mt-4 h-9 w-full", sparkClassName ?? "text-muted-foreground/50")} />
    </Card>
  );
}
