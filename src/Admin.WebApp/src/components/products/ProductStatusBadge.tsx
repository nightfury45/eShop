import { StatusBadge, type Status } from "@/components/StatusBadge";
import type { ProductStatus } from "@/lib/api";

const statusMap: Record<ProductStatus, { status: Status; label: string }> = {
  Active: { status: "in-stock", label: "Active" },
  LowStock: { status: "low", label: "Low stock" },
  OutOfStock: { status: "out", label: "Out of stock" },
};

/** Renders a dashboard product status (derived from stock) as the Figma StatusBadge pill. */
export function ProductStatusBadge({ status }: { status: ProductStatus }) {
  const config = statusMap[status];
  return <StatusBadge status={config.status} label={config.label} />;
}
