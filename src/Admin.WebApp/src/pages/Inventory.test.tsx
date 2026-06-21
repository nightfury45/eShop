import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("react-oidc-context", () => ({
  useAuth: () => ({ user: { access_token: "test-token" } }),
}));

import { InventoryPage } from "./Inventory";
import type { InventoryItem, InventoryResult } from "@/lib/api";

function item(overrides: Partial<InventoryItem>): InventoryItem {
  return {
    id: 1,
    name: "Aero Runner",
    sku: "SKU-0001",
    onHand: 412,
    reorderThreshold: 80,
    status: "Active",
    price: 129,
    ...overrides,
  };
}

const result: InventoryResult = {
  items: [
    item({}),
    item({ id: 2, name: "Merino Knit", sku: "SKU-0002", onHand: 0, status: "OutOfStock" }),
  ],
  pageIndex: 0,
  pageSize: 10,
  totalItems: 2,
  summary: { totalSkus: 2, lowStockCount: 0, outOfStockCount: 1, inventoryValue: 53196 },
};

function mockFetch(onPost?: (body: unknown) => void) {
  return vi.spyOn(globalThis, "fetch").mockImplementation((_url, init) => {
    if (init?.method === "POST") {
      onPost?.(JSON.parse(String(init.body)));
      return Promise.resolve(
        new Response(JSON.stringify(item({ onHand: 200 })), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      );
    }
    return Promise.resolve(
      new Response(JSON.stringify(result), { status: 200, headers: { "Content-Type": "application/json" } }),
    );
  });
}

afterEach(() => {
  vi.restoreAllMocks();
});

describe("InventoryPage", () => {
  it("renders inventory rows, status and KPIs from the API", async () => {
    mockFetch();
    render(<InventoryPage />);

    await waitFor(() => expect(screen.getByText("Aero Runner")).toBeInTheDocument());
    expect(screen.getByText("Merino Knit")).toBeInTheDocument();
    expect(screen.getByText("SKU-0001")).toBeInTheDocument();
    // "Out of stock" is also a KPI label, so scope the badge assertion to the table.
    const table = screen.getByRole("table");
    expect(within(table).getByText("Out of stock")).toBeInTheDocument();
    expect(within(table).getByText("In stock")).toBeInTheDocument();
    expect(screen.getByText("$53,196.00")).toBeInTheDocument(); // inventory value KPI
  });

  it("opens the adjust drawer and submits an adjustment to the API", async () => {
    let postBody: unknown = null;
    mockFetch((body) => (postBody = body));
    render(<InventoryPage />);

    await waitFor(() => expect(screen.getByText("Aero Runner")).toBeInTheDocument());

    fireEvent.click(screen.getByRole("button", { name: "Adjust Aero Runner" }));

    const drawer = await screen.findByRole("dialog");
    expect(within(drawer).getByText("Adjust stock")).toBeInTheDocument();

    fireEvent.change(within(drawer).getByLabelText("New on-hand quantity"), { target: { value: "200" } });
    fireEvent.change(within(drawer).getByLabelText("Reason"), { target: { value: "Cycle count" } });
    fireEvent.click(within(drawer).getByRole("button", { name: "Save adjustment" }));

    await waitFor(() => expect(postBody).not.toBeNull());
    expect(postBody).toMatchObject({ newOnHand: 200, reason: "Cycle count" });
  });

  it("requires a reason and does not call the API when it is blank", async () => {
    const postCalls: unknown[] = [];
    mockFetch((body) => postCalls.push(body));
    render(<InventoryPage />);

    await waitFor(() => expect(screen.getByText("Aero Runner")).toBeInTheDocument());
    fireEvent.click(screen.getByRole("button", { name: "Adjust Aero Runner" }));

    const drawer = await screen.findByRole("dialog");
    fireEvent.change(within(drawer).getByLabelText("New on-hand quantity"), { target: { value: "200" } });
    fireEvent.click(within(drawer).getByRole("button", { name: "Save adjustment" }));

    expect(await within(drawer).findByText("A reason for the adjustment is required.")).toBeInTheDocument();
    expect(postCalls).toHaveLength(0);
  });
});
