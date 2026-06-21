import type { CategorySlice } from "@/lib/api";
import { formatCurrency } from "@/lib/format";

export function TopCategories({ items }: { items: CategorySlice[] }) {
  const max = Math.max(...items.map((i) => i.revenue), 1);

  if (items.length === 0) {
    return <p className="text-sm text-muted-foreground">No category sales in this period.</p>;
  }

  return (
    <ul className="space-y-4">
      {items.map((item) => (
        <li key={item.category}>
          <div className="flex items-baseline justify-between">
            <span className="text-sm font-medium text-foreground">{item.category}</span>
            <span className="text-sm tabular-nums text-muted-foreground">{formatCurrency(item.revenue)}</span>
          </div>
          <div className="mt-1.5 h-1.5 w-full overflow-hidden rounded-full bg-secondary">
            <div
              className="h-full rounded-full bg-primary"
              style={{ width: `${Math.max((item.revenue / max) * 100, 2)}%` }}
            />
          </div>
        </li>
      ))}
    </ul>
  );
}
