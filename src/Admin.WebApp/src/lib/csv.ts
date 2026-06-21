import type { AdminProduct, ProductRow } from "@/lib/api";

function escapeCsv(value: string): string {
  return /[",\n]/.test(value) ? `"${value.replace(/"/g, '""')}"` : value;
}

const statusLabels: Record<AdminProduct["status"], string> = {
  Active: "Active",
  LowStock: "Low stock",
  OutOfStock: "Out of stock",
};

/** Builds a CSV string of the catalog product table (used by the Products Export action). */
export function buildCatalogCsv(rows: AdminProduct[]): string {
  const header = ["Product", "SKU", "Category", "Brand", "Price", "Stock", "Status"];
  const lines = rows.map((r) =>
    [r.name, r.sku, r.category, r.brand, r.price.toFixed(2), String(r.stock), statusLabels[r.status]]
      .map(escapeCsv)
      .join(","),
  );
  return [header.join(","), ...lines].join("\n");
}

/** Builds a CSV string of the top products table (used by the Export action). */
export function buildProductsCsv(rows: ProductRow[]): string {
  const header = ["Product", "Category", "Units", "Revenue", "Share %"];
  const lines = rows.map((r) =>
    [r.name, r.category, String(r.units), r.revenue.toFixed(2), r.sharePercent.toFixed(1)]
      .map(escapeCsv)
      .join(","),
  );
  return [header.join(","), ...lines].join("\n");
}

/** Triggers a client-side download of the given text content. */
export function downloadCsv(filename: string, content: string): void {
  const blob = new Blob([content], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}
