import { describe, expect, it } from "vitest";
import { buildInventoryCsv } from "./csv";
import type { InventoryItem } from "@/lib/api";

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

describe("buildInventoryCsv", () => {
  it("emits a header and one row per item with a friendly status and stock value", () => {
    const csv = buildInventoryCsv([item({})]);
    const lines = csv.split("\n");
    expect(lines[0]).toBe("Product,SKU,On hand,Reorder point,Status,Stock value");
    expect(lines[1]).toBe("Aero Runner,SKU-0001,412,80,In stock,53148.00"); // 129 * 412
  });

  it("maps OutOfStock to a readable label and quotes commas", () => {
    const csv = buildInventoryCsv([item({ name: "Knit, Crew", status: "OutOfStock", onHand: 0 })]);
    expect(csv.split("\n")[1]).toBe('"Knit, Crew",SKU-0001,0,80,Out of stock,0.00');
  });
});
