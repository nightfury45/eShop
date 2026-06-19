import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { StatusBadge } from "./StatusBadge";

describe("StatusBadge", () => {
  it("renders the default label for each status", () => {
    const { rerender } = render(<StatusBadge status="in-stock" />);
    expect(screen.getByText("In stock")).toBeInTheDocument();

    rerender(<StatusBadge status="low" />);
    expect(screen.getByText("Low stock")).toBeInTheDocument();

    rerender(<StatusBadge status="out" />);
    expect(screen.getByText("Out of stock")).toBeInTheDocument();
  });

  it("supports a custom label", () => {
    render(<StatusBadge status="in-stock" label="Available" />);
    expect(screen.getByText("Available")).toBeInTheDocument();
  });
});
