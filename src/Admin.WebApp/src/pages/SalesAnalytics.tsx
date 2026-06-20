import { useCallback, useEffect, useState } from "react";
import { useAuth } from "react-oidc-context";
import { Download } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { KpiCard } from "@/components/analytics/KpiCard";
import { RevenueChart } from "@/components/analytics/RevenueChart";
import { TopCategories } from "@/components/analytics/TopCategories";
import { TopProducts } from "@/components/analytics/TopProducts";
import { type AnalyticsPeriod, type AnalyticsSummary, getAnalyticsSummary } from "@/lib/api";
import { buildProductsCsv, downloadCsv } from "@/lib/csv";
import { formatCurrency, formatNumber } from "@/lib/format";

const periodLabels: Record<AnalyticsPeriod, string> = {
  "7d": "Last 7 days",
  "30d": "Last 30 days",
  "90d": "Last 90 days",
};

type State =
  | { kind: "loading" }
  | { kind: "ok"; summary: AnalyticsSummary }
  | { kind: "error"; message: string };

export function SalesAnalyticsPage() {
  const auth = useAuth();
  const accessToken = auth.user?.access_token ?? "";
  const [period, setPeriod] = useState<AnalyticsPeriod>("30d");
  const [state, setState] = useState<State>({ kind: "loading" });

  useEffect(() => {
    const controller = new AbortController();
    setState({ kind: "loading" });
    getAnalyticsSummary(accessToken, period, controller.signal)
      .then((summary) => setState({ kind: "ok", summary }))
      .catch((err: unknown) => {
        if (controller.signal.aborted) return;
        setState({ kind: "error", message: err instanceof Error ? err.message : "Unknown error" });
      });
    return () => controller.abort();
  }, [accessToken, period]);

  const handleExport = useCallback(() => {
    if (state.kind === "ok") {
      downloadCsv(`top-products-${period}.csv`, buildProductsCsv(state.summary.topProducts));
    }
  }, [state, period]);

  return (
    <div className="mx-auto max-w-6xl px-8 py-8">
      <header className="mb-8 flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-foreground">Sales Analytics</h1>
          <p className="mt-1 text-sm text-muted-foreground">Performance overview</p>
        </div>
        <div className="flex items-center gap-2">
          <Select value={period} onValueChange={(v) => setPeriod(v as AnalyticsPeriod)}>
            <SelectTrigger className="h-9 w-[150px]" aria-label="Period">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {(Object.keys(periodLabels) as AnalyticsPeriod[]).map((p) => (
                <SelectItem key={p} value={p}>
                  {periodLabels[p]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Button
            variant="secondary"
            size="sm"
            onClick={handleExport}
            disabled={state.kind !== "ok"}
          >
            <Download className="size-4" />
            Export
          </Button>
        </div>
      </header>

      {state.kind === "error" ? (
        <Card className="p-8">
          <p className="text-sm text-destructive">Could not load analytics — {state.message}</p>
        </Card>
      ) : state.kind === "loading" ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Card key={i} className="p-5">
              <Skeleton className="h-3 w-20" />
              <Skeleton className="mt-3 h-8 w-28" />
              <Skeleton className="mt-4 h-9 w-full" />
            </Card>
          ))}
        </div>
      ) : (
        <div className="space-y-6">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <KpiCard label="Revenue" value={formatCurrency(state.summary.revenue.value)} kpi={state.summary.revenue} sparkClassName="text-positive" />
            <KpiCard label="Orders" value={formatNumber(state.summary.orders.value)} kpi={state.summary.orders} />
            <KpiCard label="Avg order value" value={formatCurrency(state.summary.averageOrderValue.value)} kpi={state.summary.averageOrderValue} />
            <KpiCard label="Units sold" value={formatNumber(state.summary.units.value)} kpi={state.summary.units} />
          </div>

          <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
            <Card className="lg:col-span-2">
              <CardHeader>
                <CardTitle className="text-base">Revenue</CardTitle>
                <p className="text-sm text-muted-foreground">Gross sales · daily</p>
              </CardHeader>
              <CardContent>
                <RevenueChart data={state.summary.revenueSeries} />
              </CardContent>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Top categories</CardTitle>
              </CardHeader>
              <CardContent>
                <TopCategories items={state.summary.topCategories} />
              </CardContent>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Top selling products</CardTitle>
            </CardHeader>
            <CardContent className="px-0 pb-0">
              <TopProducts rows={state.summary.topProducts} />
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}
