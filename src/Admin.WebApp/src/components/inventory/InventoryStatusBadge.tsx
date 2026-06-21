import { StatusBadge, type Status } from "@/components/StatusBadge";
import type { ProductStatus } from "@/lib/api";

const statusMap: Record<ProductStatus, { status: Status; label: string }> = {
  Active: { status: "in-stock", label: "In stock" },
  LowStock: { status: "low", label: "Low stock" },
  OutOfStock: { status: "out", label: "Out of stock" },
};

/** Renders a stock status (derived from on-hand vs reorder point) as the Figma StatusBadge pill. */
export function InventoryStatusBadge({ status }: { status: ProductStatus }) {
  const config = statusMap[status];
  return <StatusBadge status={config.status} label={config.label} />;
}
