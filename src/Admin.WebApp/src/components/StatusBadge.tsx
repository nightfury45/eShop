import { Badge, type BadgeProps } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

/** Figma component: StatusBadge. Stock/health states rendered as a tinted pill with a status dot. */
export type Status = "in-stock" | "low" | "out" | "neutral";

const statusConfig: Record<Status, { label: string; variant: BadgeProps["variant"]; dot: string }> = {
  "in-stock": { label: "In stock", variant: "positive", dot: "bg-positive" },
  low: { label: "Low stock", variant: "warning", dot: "bg-warning" },
  out: { label: "Out of stock", variant: "destructive", dot: "bg-destructive" },
  neutral: { label: "Unknown", variant: "neutral", dot: "bg-muted-foreground" },
};

export interface StatusBadgeProps {
  status: Status;
  label?: string;
  className?: string;
}

export function StatusBadge({ status, label, className }: StatusBadgeProps) {
  const config = statusConfig[status];
  return (
    <Badge variant={config.variant} className={cn("gap-1.5", className)}>
      <span aria-hidden className={cn("size-1.5 rounded-full", config.dot)} />
      {label ?? config.label}
    </Badge>
  );
}
