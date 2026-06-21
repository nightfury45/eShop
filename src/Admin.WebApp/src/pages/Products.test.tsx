import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("react-oidc-context", () => ({
  useAuth: () => ({ user: { access_token: "test-token" } }),
}));

import { ProductsPage } from "./Products";
import type { AdminProduct, AdminProductsResult } from "@/lib/api";

function product(overrides: Partial<AdminProduct>): AdminProduct {
  return {
    id: 1,
    name: "Aero Runner",
    sku: "SKU-0001",
    categoryId: 1,
    category: "Footwear",
    brandId: 1,
    brand: "Acme",
    price: 129,
    stock: 412,
    restockThreshold: 10,
    status: "Active",
    description: "Fast shoe",
    ...overrides,
  };
}

const result: AdminProductsResult = {
  items: [
    product({}),
    product({ id: 2, name: "Merino Knit", sku: "SKU-0002", category: "Apparel", status: "OutOfStock", stock: 0 }),
  ],
  pageIndex: 0,
  pageSize: 10,
  totalItems: 2,
  categories: [
    { id: 1, name: "Footwear" },
    { id: 2, name: "Apparel" },
  ],
  brands: [{ id: 1, name: "Acme" }],
};

function mockFetch(onPut?: (body: unknown) => void) {
  return vi.spyOn(globalThis, "fetch").mockImplementation((_url, init) => {
    if (init?.method === "PUT") {
      onPut?.(JSON.parse(String(init.body)));
      return Promise.resolve(
        new Response(JSON.stringify(product({ name: "Aero Runner 2" })), {
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

describe("ProductsPage", () => {
  it("renders products with derived status from the API", async () => {
    mockFetch();
    render(<ProductsPage />);

    await waitFor(() => expect(screen.getByText("Aero Runner")).toBeInTheDocument());
    expect(screen.getByText("Merino Knit")).toBeInTheDocument();
    expect(screen.getByText("SKU-0001")).toBeInTheDocument();
    expect(screen.getByText("Out of stock")).toBeInTheDocument();
  });

  it("opens the edit drawer and submits changes to the API", async () => {
    let putBody: unknown = null;
    mockFetch((body) => (putBody = body));
    render(<ProductsPage />);

    await waitFor(() => expect(screen.getByText("Aero Runner")).toBeInTheDocument());

    fireEvent.click(screen.getByRole("button", { name: "Edit Aero Runner" }));

    const drawer = await screen.findByRole("dialog");
    expect(within(drawer).getByText("Edit product")).toBeInTheDocument();

    fireEvent.change(within(drawer).getByLabelText("Price (USD)"), { target: { value: "149" } });
    fireEvent.click(within(drawer).getByRole("button", { name: "Save changes" }));

    await waitFor(() => expect(putBody).not.toBeNull());
    expect(putBody).toMatchObject({ name: "Aero Runner", price: 149 });
  });

  it("shows a validation error and does not call the API when the name is blank", async () => {
    const putCalls: unknown[] = [];
    mockFetch((body) => putCalls.push(body));
    render(<ProductsPage />);

    await waitFor(() => expect(screen.getByText("Aero Runner")).toBeInTheDocument());
    fireEvent.click(screen.getByRole("button", { name: "Edit Aero Runner" }));

    const drawer = await screen.findByRole("dialog");
    fireEvent.change(within(drawer).getByLabelText("Product name"), { target: { value: "" } });
    fireEvent.click(within(drawer).getByRole("button", { name: "Save changes" }));

    expect(await within(drawer).findByText("Product name is required.")).toBeInTheDocument();
    expect(putCalls).toHaveLength(0);
  });
});
