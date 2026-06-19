import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { Sidebar } from "./Sidebar";

beforeEach(() => {
  // AdminProfile (sidebar footer) calls the BFF /me endpoint.
  vi.spyOn(globalThis, "fetch").mockResolvedValue(
    new Response(JSON.stringify({ subject: "u1", name: "Priya Admin", roles: ["Administrator"] }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    }),
  );
});

afterEach(() => {
  vi.restoreAllMocks();
});

describe("Sidebar", () => {
  it("marks the active route with aria-current", () => {
    render(
      <MemoryRouter initialEntries={["/sales"]}>
        <Sidebar accessToken="t" />
      </MemoryRouter>,
    );

    expect(screen.getByRole("link", { name: /sales analytics/i })).toHaveAttribute("aria-current", "page");
    expect(screen.getByRole("link", { name: /dashboard/i })).not.toHaveAttribute("aria-current", "page");
  });

  it("renders every primary nav item", () => {
    render(
      <MemoryRouter initialEntries={["/dashboard"]}>
        <Sidebar accessToken="t" />
      </MemoryRouter>,
    );

    for (const label of ["Dashboard", "Sales Analytics", "Products", "Inventory", "Settings"]) {
      expect(screen.getByRole("link", { name: new RegExp(label, "i") })).toBeInTheDocument();
    }
  });
});
