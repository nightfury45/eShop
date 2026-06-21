/** Response of the Admin BFF liveness probe (`GET /api/admin/ping`). */
export interface PingResponse {
  status: string;
  utc: string;
}

/** Calls the Admin BFF ping endpoint through the Vite dev proxy / Aspire service discovery. */
export async function getPing(signal?: AbortSignal): Promise<PingResponse> {
  const res = await fetch("/api/admin/ping", { signal });
  if (!res.ok) {
    throw new Error(`Admin BFF ping failed with status ${res.status}`);
  }
  return (await res.json()) as PingResponse;
}

/** OIDC client configuration the SPA needs to start a login, served by the BFF. */
export interface AdminClientConfig {
  authority: string;
  clientId: string;
  scope: string;
}

/** The authenticated administrator, as projected by the BFF from the validated JWT. */
export interface AdminUser {
  subject: string;
  name: string;
  roles: string[];
}

/** Fetches the OIDC client configuration (anonymous endpoint). */
export async function getAdminConfig(signal?: AbortSignal): Promise<AdminClientConfig> {
  const res = await fetch("/api/admin/config", { signal });
  if (!res.ok) {
    throw new Error(`Admin config failed with status ${res.status}`);
  }
  return (await res.json()) as AdminClientConfig;
}

/** Fetches the current administrator profile from the secured BFF endpoint. */
export async function getMe(accessToken: string, signal?: AbortSignal): Promise<AdminUser> {
  const res = await fetch("/api/admin/me", {
    headers: { Authorization: `Bearer ${accessToken}` },
    signal,
  });
  if (!res.ok) {
    throw new Error(`Admin profile failed with status ${res.status}`);
  }
  return (await res.json()) as AdminUser;
}

// ---- Analytics (Sales) ----

export interface KpiValue {
  value: number;
  deltaPercent: number;
  spark: number[];
}

export interface RevenuePoint {
  date: string;
  current: number;
  previous: number;
}

export interface CategorySlice {
  category: string;
  revenue: number;
}

export interface ProductRow {
  productId: number;
  name: string;
  category: string;
  units: number;
  revenue: number;
  sharePercent: number;
}

export interface AnalyticsSummary {
  periodDays: number;
  revenue: KpiValue;
  orders: KpiValue;
  averageOrderValue: KpiValue;
  units: KpiValue;
  revenueSeries: RevenuePoint[];
  topCategories: CategorySlice[];
  topProducts: ProductRow[];
}

export type AnalyticsPeriod = "7d" | "30d" | "90d";

/** Fetches the Sales Analytics summary for a period from the secured BFF endpoint. */
export async function getAnalyticsSummary(
  accessToken: string,
  period: AnalyticsPeriod,
  signal?: AbortSignal,
): Promise<AnalyticsSummary> {
  const res = await fetch(`/api/admin/analytics/summary?period=${encodeURIComponent(period)}`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    signal,
  });
  if (!res.ok) {
    throw new Error(`Analytics summary failed with status ${res.status}`);
  }
  return (await res.json()) as AnalyticsSummary;
}

// ---- Products ----

/** Dashboard-derived product status (Catalog has no status field — see BFF ProductCatalogService). */
export type ProductStatus = "Active" | "LowStock" | "OutOfStock";

export interface AdminProduct {
  id: number;
  name: string;
  sku: string;
  categoryId: number;
  category: string;
  brandId: number;
  brand: string;
  price: number;
  stock: number;
  restockThreshold: number;
  status: ProductStatus;
  description: string | null;
}

export interface CatalogRef {
  id: number;
  name: string;
}

export interface AdminProductsResult {
  items: AdminProduct[];
  pageIndex: number;
  pageSize: number;
  totalItems: number;
  categories: CatalogRef[];
  brands: CatalogRef[];
}

export interface ProductsQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  category?: number;
  brand?: number;
}

export interface ProductUpdateRequest {
  name: string;
  price: number;
  stock: number;
  categoryId: number;
  brandId: number;
  description: string | null;
}

/** Fetches a page of products (plus category/brand filter lists) from the secured BFF endpoint. */
export async function getProducts(
  accessToken: string,
  query: ProductsQuery,
  signal?: AbortSignal,
): Promise<AdminProductsResult> {
  const params = new URLSearchParams();
  if (query.page !== undefined) params.set("page", String(query.page));
  if (query.pageSize !== undefined) params.set("pageSize", String(query.pageSize));
  if (query.search) params.set("search", query.search);
  if (query.category !== undefined) params.set("category", String(query.category));
  if (query.brand !== undefined) params.set("brand", String(query.brand));

  const res = await fetch(`/api/admin/products?${params.toString()}`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    signal,
  });
  if (!res.ok) {
    throw new Error(`Products request failed with status ${res.status}`);
  }
  return (await res.json()) as AdminProductsResult;
}

/** Saves edits to a product through the secured BFF endpoint, returning the updated product. */
export async function updateProduct(
  accessToken: string,
  id: number,
  body: ProductUpdateRequest,
  signal?: AbortSignal,
): Promise<AdminProduct> {
  const res = await fetch(`/api/admin/products/${id}`, {
    method: "PUT",
    headers: { Authorization: `Bearer ${accessToken}`, "Content-Type": "application/json" },
    body: JSON.stringify(body),
    signal,
  });
  if (!res.ok) {
    throw new Error(`Product update failed with status ${res.status}`);
  }
  return (await res.json()) as AdminProduct;
}

// ---- Inventory ----

export interface InventoryItem {
  id: number;
  name: string;
  sku: string;
  onHand: number;
  reorderThreshold: number;
  status: ProductStatus;
  price: number;
}

export interface InventorySummary {
  totalSkus: number;
  lowStockCount: number;
  outOfStockCount: number;
  inventoryValue: number;
}

export interface InventoryResult {
  items: InventoryItem[];
  pageIndex: number;
  pageSize: number;
  totalItems: number;
  summary: InventorySummary;
}

export interface InventoryQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  lowStockOnly?: boolean;
}

export interface StockAdjustmentRequest {
  newOnHand: number;
  reason: string;
}

/** Fetches a page of inventory rows plus store-wide KPIs from the secured BFF endpoint. */
export async function getInventory(
  accessToken: string,
  query: InventoryQuery,
  signal?: AbortSignal,
): Promise<InventoryResult> {
  const params = new URLSearchParams();
  if (query.page !== undefined) params.set("page", String(query.page));
  if (query.pageSize !== undefined) params.set("pageSize", String(query.pageSize));
  if (query.search) params.set("search", query.search);
  if (query.lowStockOnly) params.set("lowStockOnly", "true");

  const res = await fetch(`/api/admin/inventory?${params.toString()}`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    signal,
  });
  if (!res.ok) {
    throw new Error(`Inventory request failed with status ${res.status}`);
  }
  return (await res.json()) as InventoryResult;
}

/** Adjusts a product's on-hand stock through the secured BFF endpoint, returning the updated row. */
export async function adjustStock(
  accessToken: string,
  id: number,
  body: StockAdjustmentRequest,
  signal?: AbortSignal,
): Promise<InventoryItem> {
  const res = await fetch(`/api/admin/inventory/${id}/adjust`, {
    method: "POST",
    headers: { Authorization: `Bearer ${accessToken}`, "Content-Type": "application/json" },
    body: JSON.stringify(body),
    signal,
  });
  if (!res.ok) {
    throw new Error(`Stock adjustment failed with status ${res.status}`);
  }
  return (await res.json()) as InventoryItem;
}
