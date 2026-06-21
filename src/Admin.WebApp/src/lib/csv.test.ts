import { describe, expect, it } from "vitest";
import { buildProductsCsv } from "./csv";
import type { ProductRow } from "@/lib/api";

describe("buildProductsCsv", () => {
  it("emits a header and one row per product", () => {
    const rows: ProductRow[] = [
      { productId: 1, name: "Aero Runner", category: "Footwear", units: 412, revenue: 24720, sharePercent: 14.2 },
    ];

    const csv = buildProductsCsv(rows);

    const lines = csv.split("\n");
    expect(lines[0]).toBe("Product,Category,Units,Revenue,Share %");
    expect(lines[1]).toBe("Aero Runner,Footwear,412,24720.00,14.2");
  });

  it("quotes values containing commas", () => {
    const rows: ProductRow[] = [
      { productId: 2, name: "Knit, Crew", category: "Apparel", units: 10, revenue: 100, sharePercent: 1 },
    ];

    const csv = buildProductsCsv(rows);

    expect(csv.split("\n")[1]).toBe('"Knit, Crew",Apparel,10,100.00,1.0');
  });
});
