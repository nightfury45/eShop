import { useCallback, useEffect, useMemo, useState } from "react";
import { useAuth } from "react-oidc-context";
import { Download, Search } from "lucide-react";
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
import { Card } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { InventoryStatusBadge } from "@/components/inventory/InventoryStatusBadge";
import { AdjustStockDrawer } from "@/components/inventory/AdjustStockDrawer";
import {
  adjustStock,
  getInventory,
  type InventoryItem,
  type InventoryResult,
  type ProductStatus,
  type StockAdjustmentRequest,
} from "@/lib/api";
import { buildInventoryCsv, downloadCsv } from "@/lib/csv";
import { cn } from "@/lib/utils";
import { formatCurrency, formatNumber } from "@/lib/format";

const PAGE_SIZE = 10;

type StockFilter = "all" | "low";

function onHandClass(status: ProductStatus): string {
  if (status === "OutOfStock") return "text-destructive";
  if (status === "LowStock") return "text-warning";
  return "text-foreground";
}

function rowAccent(status: ProductStatus): string {
  if (status === "OutOfStock") return "border-l-2 border-l-destructive";
  if (status === "LowStock") return "border-l-2 border-l-warning";
  return "border-l-2 border-l-transparent";
}

function KpiCard({ label, value, sub, tone }: { label: string; value: string; sub: string; tone?: string }) {
  return (
    <Card className="p-5">
      <p className="text-[11px] font-medium uppercase tracking-widest text-muted-foreground">{label}</p>
      <p className={cn("mt-2 text-3xl font-semibold tracking-tight tabular-nums", tone ?? "text-foreground")}>{value}</p>
      <p className="mt-2 text-xs text-muted-foreground">{sub}</p>
    </Card>
  );
}

export function InventoryPage() {
  const auth = useAuth();
  const accessToken = auth.user?.access_token ?? "";

  const [search, setSearch] = useState("");
  const [stockFilter, setStockFilter] = useState<StockFilter>("all");
  const [page, setPage] = useState(0);

  const [data, setData] = useState<InventoryResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  const [adjusting, setAdjusting] = useState<InventoryItem | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError(null);
    getInventory(
      accessToken,
      {
        page,
        pageSize: PAGE_SIZE,
        search: search.trim() || undefined,
        lowStockOnly: stockFilter === "low",
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
  }, [accessToken, page, search, stockFilter, reloadKey]);

  const items = useMemo(() => data?.items ?? [], [data]);

  const openAdjust = useCallback((item: InventoryItem) => {
    setAdjusting(item);
    setSaveError(null);
    setDrawerOpen(true);
  }, []);

  // Header action jumps to the most urgent row on the page (out, then low, else first).
  const openMostUrgent = useCallback(() => {
    const target =
      items.find((i) => i.status === "OutOfStock") ??
      items.find((i) => i.status === "LowStock") ??
      items[0];
    if (target) openAdjust(target);
  }, [items, openAdjust]);

  const handleSave = useCallback(
    (id: number, body: StockAdjustmentRequest) => {
      setSaving(true);
      setSaveError(null);
      adjustStock(accessToken, id, body)
        .then(() => {
          setDrawerOpen(false);
          setReloadKey((k) => k + 1); // refresh with persisted values + recomputed KPIs
        })
        .catch((err: unknown) => {
          setSaveError(err instanceof Error ? err.message : "Could not save adjustment");
        })
        .finally(() => setSaving(false));
    },
    [accessToken],
  );

  const handleExport = useCallback(() => {
    if (data) downloadCsv("inventory.csv", buildInventoryCsv(items));
  }, [data, items]);

  const summary = data?.summary;
  const total = data?.totalItems ?? 0;
  const rangeStart = total === 0 ? 0 : page * PAGE_SIZE + 1;
  const rangeEnd = Math.min((page + 1) * PAGE_SIZE, total);
  const hasNext = (page + 1) * PAGE_SIZE < total;

  return (
    <div className="mx-auto max-w-6xl px-8 py-8">
      <header className="mb-6 flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-foreground">Inventory</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {formatNumber(summary?.totalSkus ?? 0)} SKUs · {formatNumber(summary?.lowStockCount ?? 0)} below reorder point
          </p>
        </div>
        <div className="flex items-center gap-2.5">
          <Button variant="secondary" size="sm" onClick={handleExport} disabled={!data}>
            <Download className="size-4" />
            Export
          </Button>
          <Button size="sm" onClick={openMostUrgent} disabled={items.length === 0}>
            Adjust stock
          </Button>
        </div>
      </header>

      <div className="mb-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <KpiCard label="Total SKUs" value={formatNumber(summary?.totalSkus ?? 0)} sub="tracked products" />
        <KpiCard
          label="Low stock"
          value={formatNumber(summary?.lowStockCount ?? 0)}
          sub="below reorder point"
          tone="text-warning"
        />
        <KpiCard
          label="Out of stock"
          value={formatNumber(summary?.outOfStockCount ?? 0)}
          sub="needs restock now"
          tone="text-destructive"
        />
        <KpiCard
          label="Inventory value"
          value={formatCurrency(summary?.inventoryValue ?? 0)}
          sub="retail · on-hand × price"
        />
      </div>

      <div className="mb-4 flex flex-wrap items-center gap-2.5">
        <div className="relative w-[300px]">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={search}
            onChange={(e) => {
              setPage(0);
              setSearch(e.target.value);
            }}
            placeholder="Search products or SKUs…"
            className="h-9 pl-9"
            aria-label="Search inventory"
          />
        </div>
        <Select
          value={stockFilter}
          onValueChange={(v) => {
            setPage(0);
            setStockFilter(v as StockFilter);
          }}
        >
          <SelectTrigger className="h-9 w-[180px]" aria-label="Stock filter">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All stock</SelectItem>
            <SelectItem value="low">Below reorder point</SelectItem>
          </SelectContent>
        </Select>
        <p className="ml-auto text-sm text-muted-foreground">
          Showing {formatNumber(rangeStart)}–{formatNumber(rangeEnd)} of {formatNumber(total)}
        </p>
      </div>

      <div className="rounded-lg border border-border bg-card">
        {error ? (
          <p className="p-8 text-sm text-destructive">Could not load inventory — {error}</p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow className="hover:bg-transparent">
                <TableHead className="text-[11px] uppercase tracking-widest text-muted-foreground">Product</TableHead>
                <TableHead className="w-[120px] text-right text-[11px] uppercase tracking-widest text-muted-foreground">On hand</TableHead>
                <TableHead className="w-[120px] text-right text-[11px] uppercase tracking-widest text-muted-foreground">Reorder pt</TableHead>
                <TableHead className="w-[140px] text-[11px] uppercase tracking-widest text-muted-foreground">Status</TableHead>
                <TableHead className="w-[96px]" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading ? (
                Array.from({ length: 6 }).map((_, i) => (
                  <TableRow key={i} className="hover:bg-transparent">
                    <TableCell colSpan={5}>
                      <Skeleton className="h-9 w-full" />
                    </TableCell>
                  </TableRow>
                ))
              ) : items.length === 0 ? (
                <TableRow className="hover:bg-transparent">
                  <TableCell colSpan={5} className="py-10 text-center text-sm text-muted-foreground">
                    No products match your filters.
                  </TableCell>
                </TableRow>
              ) : (
                items.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell className={rowAccent(item.status)}>
                      <div className="flex items-center gap-3">
                        <div className="flex size-9 items-center justify-center rounded-lg bg-accent/10 font-mono text-sm text-accent">
                          {item.name.charAt(0).toUpperCase()}
                        </div>
                        <div className="flex flex-col">
                          <span className="font-medium text-foreground">{item.name}</span>
                          <span className="font-mono text-xs text-muted-foreground">{item.sku}</span>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell className={cn("text-right font-mono", onHandClass(item.status))}>
                      {formatNumber(item.onHand)}
                    </TableCell>
                    <TableCell className="text-right font-mono text-muted-foreground">
                      {formatNumber(item.reorderThreshold)}
                    </TableCell>
                    <TableCell>
                      <InventoryStatusBadge status={item.status} />
                    </TableCell>
                    <TableCell className="text-right">
                      <Button
                        variant={item.status === "Active" ? "ghost" : "secondary"}
                        size="sm"
                        onClick={() => openAdjust(item)}
                        aria-label={`Adjust ${item.name}`}
                      >
                        {item.status === "Active" ? "Adjust" : "Restock"}
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

      <AdjustStockDrawer
        item={adjusting}
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
