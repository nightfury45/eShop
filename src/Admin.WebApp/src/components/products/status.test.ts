import { describe, expect, it } from "vitest";
import { deriveStatus } from "./status";

describe("deriveStatus", () => {
  it("is OutOfStock at zero stock", () => {
    expect(deriveStatus(0, 10)).toBe("OutOfStock");
  });

  it("is LowStock at or below the restock threshold", () => {
    expect(deriveStatus(10, 10)).toBe("LowStock");
    expect(deriveStatus(3, 10)).toBe("LowStock");
  });

  it("is Active above the restock threshold", () => {
    expect(deriveStatus(50, 10)).toBe("Active");
  });
});
