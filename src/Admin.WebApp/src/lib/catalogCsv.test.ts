import { describe, expect, it } from "vitest";
import { buildCatalogCsv } from "./csv";
import type { AdminProduct } from "@/lib/api";

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
    description: null,
    ...overrides,
  };
}

describe("buildCatalogCsv", () => {
  it("emits a header and one row per product with a friendly status", () => {
    const csv = buildCatalogCsv([product({})]);
    const lines = csv.split("\n");
    expect(lines[0]).toBe("Product,SKU,Category,Brand,Price,Stock,Status");
    expect(lines[1]).toBe("Aero Runner,SKU-0001,Footwear,Acme,129.00,412,Active");
  });

  it("maps OutOfStock to a readable label and quotes commas", () => {
    const csv = buildCatalogCsv([product({ name: "Knit, Crew", status: "OutOfStock", stock: 0 })]);
    expect(csv.split("\n")[1]).toBe('"Knit, Crew",SKU-0001,Footwear,Acme,129.00,0,Out of stock');
  });
});
