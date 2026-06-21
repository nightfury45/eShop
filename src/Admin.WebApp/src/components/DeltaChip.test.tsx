import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { DeltaChip } from "./DeltaChip";

describe("DeltaChip", () => {
  it("formats a positive delta with a plus sign and percent", () => {
    render(<DeltaChip value={12.4} />);
    const chip = screen.getByText("+12.4%");
    expect(chip).toBeInTheDocument();
    expect(chip.className).toContain("text-positive");
  });

  it("formats a negative delta and uses the destructive tone", () => {
    render(<DeltaChip value={-2.3} />);
    const chip = screen.getByText("-2.3%");
    expect(chip).toBeInTheDocument();
    expect(chip.className).toContain("text-destructive");
  });

  it("supports a custom unit", () => {
    render(<DeltaChip value={0.4} unit="pt" />);
    expect(screen.getByText("+0.4pt")).toBeInTheDocument();
  });
});
