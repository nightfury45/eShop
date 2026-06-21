import type { ProductStatus } from "@/lib/api";

/**
 * Mirrors the BFF's status derivation (ProductCatalogService.DeriveStatus) so the edit drawer can
 * preview the resulting status as the administrator changes the stock value.
 */
export function deriveStatus(stock: number, restockThreshold: number): ProductStatus {
  if (stock <= 0) return "OutOfStock";
  if (stock <= restockThreshold) return "LowStock";
  return "Active";
}
