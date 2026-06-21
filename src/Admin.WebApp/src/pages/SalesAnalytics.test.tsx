import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("react-oidc-context", () => ({
  useAuth: () => ({ user: { access_token: "test-token" } }),
}));

import { SalesAnalyticsPage } from "./SalesAnalytics";
import type { AnalyticsSummary } from "@/lib/api";

const summary: AnalyticsSummary = {
  periodDays: 30,
  revenue: { value: 48250, deltaPercent: 12.4, spark: [1, 2, 3, 4] },
  orders: { value: 1284, deltaPercent: 6.1, spark: [1, 2, 3, 4] },
  averageOrderValue: { value: 37.58, deltaPercent: -2.3, spark: [4, 3, 2, 1] },
  units: { value: 5000, deltaPercent: 4, spark: [1, 2, 3, 4] },
  revenueSeries: [{ date: "2026-06-01", current: 100, previous: 90 }],
  topCategories: [{ category: "Apparel", revenue: 18420 }],
  topProducts: [
    { productId: 1, name: "Aero Runner", category: "Footwear", units: 412, revenue: 24720, sharePercent: 14.2 },
  ],
};

afterEach(() => {
  vi.restoreAllMocks();
});

describe("SalesAnalyticsPage", () => {
  it("renders KPIs, top categories and top products from the API", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response(JSON.stringify(summary), { status: 200, headers: { "Content-Type": "application/json" } }),
    );

    render(<SalesAnalyticsPage />);

    await waitFor(() => expect(screen.getByText("$48,250.00")).toBeInTheDocument());
    expect(screen.getByText("1,284")).toBeInTheDocument();
    expect(screen.getByText("+12.4%")).toBeInTheDocument();
    expect(screen.getByText("-2.3%")).toBeInTheDocument();
    expect(screen.getByText("Apparel")).toBeInTheDocument();
    expect(screen.getByText("Aero Runner")).toBeInTheDocument();
  });

  it("shows an error state when the API fails", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response("nope", { status: 500 }));

    render(<SalesAnalyticsPage />);

    await waitFor(() => expect(screen.getByText(/Could not load analytics/i)).toBeInTheDocument());
  });
});
