import { useCallback, useEffect, useMemo, useState } from "react";
import { useAuth } from "react-oidc-context";
import { Download, Pencil, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Skeleton } from "@/components/ui/skeleton";
import { ProductStatusBadge } from "@/components/products/ProductStatusBadge";
import { ProductEditDrawer } from "@/components/products/ProductEditDrawer";
import {
  type AdminProduct,
  type AdminProductsResult,
  getProducts,
  type ProductStatus,
  type ProductUpdateRequest,
  updateProduct,
} from "@/lib/api";
import { buildCatalogCsv, downloadCsv } from "@/lib/csv";
import { cn } from "@/lib/utils";
import { formatCurrency, formatNumber } from "@/lib/format";

const PAGE_SIZE = 10;

type StatusFilter = "all" | ProductStatus;

function stockClass(status: ProductStatus): string {
  if (status === "OutOfStock") return "text-destructive";
  if (status === "LowStock") return "text-warning";
  return "text-foreground";
}

export function ProductsPage() {
  const auth = useAuth();
  const accessToken = auth.user?.access_token ?? "";

  const [search, setSearch] = useState("");
  const [category, setCategory] = useState<string>("all");
  const [status, setStatus] = useState<StatusFilter>("all");
  const [page, setPage] = useState(0);

  const [data, setData] = useState<AdminProductsResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  const [editing, setEditing] = useState<AdminProduct | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError(null);
    getProducts(
      accessToken,
      {
        page,
        pageSize: PAGE_SIZE,
        search: search.trim() || undefined,
        category: category === "all" ? undefined : Number(category),
      },
      controller.signal,
    )
      .then((result) => setData(result))
      .catch((err: unknown) => {
        if (controller.signal.aborted) return;
        setError(err instanceof Error ? err.message : "Unknown error");
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, [accessToken, page, search, category, reloadKey]);

  // Status is derived on the BFF, so filter it client-side over the current page.
  const visibleItems = useMemo(() => {
    if (!data) return [];
    return status === "all" ? data.items : data.items.filter((p) => p.status === status);
  }, [data, status]);

  const openEditor = useCallback((product: AdminProduct) => {
    setEditing(product);
    setSaveError(null);
    setDrawerOpen(true);
  }, []);

  const handleSave = useCallback(
    (id: number, body: ProductUpdateRequest) => {
      setSaving(true);
      setSaveError(null);
      updateProduct(accessToken, id, body)
        .then(() => {
          setDrawerOpen(false);
          setReloadKey((k) => k + 1); // refresh the list with the persisted values
        })
        .catch((err: unknown) => {
          setSaveError(err instanceof Error ? err.message : "Could not save changes");
        })
        .finally(() => setSaving(false));
    },
    [accessToken],
  );

  const handleExport = useCallback(() => {
    if (data) {
      downloadCsv("products.csv", buildCatalogCsv(visibleItems));
    }
  }, [data, visibleItems]);

  const total = data?.totalItems ?? 0;
  const rangeStart = total === 0 ? 0 : page * PAGE_SIZE + 1;
  const rangeEnd = Math.min((page + 1) * PAGE_SIZE, total);
  const hasNext = (page + 1) * PAGE_SIZE < total;

  return (
    <div className="mx-auto max-w-6xl px-8 py-8">
      <header className="mb-6 flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-foreground">Products</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {formatNumber(total)} products · {data?.categories.length ?? 0} categories
          </p>
        </div>
        <div className="flex items-center gap-2.5">
          <Button variant="secondary" size="sm" onClick={handleExport} disabled={!data}>
            <Download className="size-4" />
            Export
          </Button>
        </div>
      </header>

      <div className="mb-4 flex flex-wrap items-center gap-2.5">
        <div className="relative w-[300px]">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={search}
            onChange={(e) => {
              setPage(0);
              setSearch(e.target.value);
            }}
            placeholder="Search products…"
            className="h-9 pl-9"
            aria-label="Search products"
          />
        </div>
        <Select
          value={category}
          onValueChange={(v) => {
            setPage(0);
            setCategory(v);
          }}
        >
          <SelectTrigger className="h-9 w-[180px]" aria-label="Category">
            <SelectValue placeholder="All categories" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All categories</SelectItem>
            {data?.categories.map((c) => (
              <SelectItem key={c.id} value={String(c.id)}>{c.name}</SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={status} onValueChange={(v) => setStatus(v as StatusFilter)}>
          <SelectTrigger className="h-9 w-[150px]" aria-label="Status">
            <SelectValue placeholder="All status" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All status</SelectItem>
            <SelectItem value="Active">Active</SelectItem>
            <SelectItem value="LowStock">Low stock</SelectItem>
            <SelectItem value="OutOfStock">Out of stock</SelectItem>
          </SelectContent>
        </Select>
        <p className="ml-auto text-sm text-muted-foreground">
          Showing {formatNumber(rangeStart)}–{formatNumber(rangeEnd)} of {formatNumber(total)}
        </p>
      </div>

      <div className="rounded-lg border border-border bg-card">
        {error ? (
          <p className="p-8 text-sm text-destructive">Could not load products — {error}</p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow className="hover:bg-transparent">
                <TableHead className="text-[11px] uppercase tracking-widest text-muted-foreground">Product</TableHead>
                <TableHead className="w-[150px] text-[11px] uppercase tracking-widest text-muted-foreground">Category</TableHead>
                <TableHead className="w-[110px] text-right text-[11px] uppercase tracking-widest text-muted-foreground">Price</TableHead>
                <TableHead className="w-[110px] text-right text-[11px] uppercase tracking-widest text-muted-foreground">Stock</TableHead>
                <TableHead className="w-[140px] text-[11px] uppercase tracking-widest text-muted-foreground">Status</TableHead>
                <TableHead className="w-[64px]" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading ? (
                Array.from({ length: 6 }).map((_, i) => (
                  <TableRow key={i} className="hover:bg-transparent">
                    <TableCell colSpan={6}>
                      <Skeleton className="h-9 w-full" />
                    </TableCell>
                  </TableRow>
                ))
              ) : visibleItems.length === 0 ? (
                <TableRow className="hover:bg-transparent">
                  <TableCell colSpan={6} className="py-10 text-center text-sm text-muted-foreground">
                    No products match your filters.
                  </TableCell>
                </TableRow>
              ) : (
                visibleItems.map((product) => (
                  <TableRow key={product.id}>
                    <TableCell>
                      <div className="flex items-center gap-3">
                        <div className="flex size-9 items-center justify-center rounded-lg bg-accent/10 font-mono text-sm text-accent">
                          {product.name.charAt(0).toUpperCase()}
                        </div>
                        <div className="flex flex-col">
                          <span className="font-medium text-foreground">{product.name}</span>
                          <span className="font-mono text-xs text-muted-foreground">{product.sku}</span>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell className="text-muted-foreground">{product.category}</TableCell>
                    <TableCell className="text-right font-mono text-foreground">{formatCurrency(product.price)}</TableCell>
                    <TableCell className={cn("text-right font-mono", stockClass(product.status))}>
                      {formatNumber(product.stock)}
                    </TableCell>
                    <TableCell>
                      <ProductStatusBadge status={product.status} />
                    </TableCell>
                    <TableCell className="text-right">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => openEditor(product)}
                        aria-label={`Edit ${product.name}`}
                      >
                        <Pencil className="size-4" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        )}

        <div className="flex items-center justify-between border-t border-border px-4 py-3">
          <p className="text-sm text-muted-foreground">
            Showing {formatNumber(rangeStart)}–{formatNumber(rangeEnd)} of {formatNumber(total)} products
          </p>
          <div className="flex items-center gap-2">
            <Button
              variant="secondary"
              size="sm"
              onClick={() => setPage((p) => Math.max(p - 1, 0))}
              disabled={page === 0}
            >
              Previous
            </Button>
            <Button variant="secondary" size="sm" onClick={() => setPage((p) => p + 1)} disabled={!hasNext}>
              Next
            </Button>
          </div>
        </div>
      </div>

      <ProductEditDrawer
        product={editing}
        categories={data?.categories ?? []}
        brands={data?.brands ?? []}
        open={drawerOpen}
        saving={saving}
        error={saveError}
        onOpenChange={(open) => {
          setDrawerOpen(open);
          if (!open) setSaveError(null);
        }}
        onSave={handleSave}
      />
    </div>
  );
}
