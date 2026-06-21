import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const useAuthMock = vi.fn();
vi.mock("react-oidc-context", () => ({
  useAuth: () => useAuthMock(),
}));

import App from "./App";

afterEach(() => {
  vi.restoreAllMocks();
});

beforeEach(() => {
  useAuthMock.mockReset();
});

describe("App", () => {
  it("renders a sign-in button when unauthenticated", () => {
    useAuthMock.mockReturnValue({
      isLoading: false,
      isAuthenticated: false,
      signinRedirect: vi.fn(),
    });

    render(<App />);

    expect(screen.getByRole("button", { name: /sign in/i })).toBeInTheDocument();
  });

  it("renders the dashboard shell and admin profile when authenticated", async () => {
    vi.spyOn(globalThis, "fetch").mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/api/admin/me")) {
        return Promise.resolve(
          new Response(JSON.stringify({ subject: "u1", name: "Priya Admin", roles: ["Administrator"] }), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      return Promise.resolve(
        new Response(JSON.stringify({ status: "ok", utc: new Date().toISOString() }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      );
    });

    useAuthMock.mockReturnValue({
      isLoading: false,
      isAuthenticated: true,
      user: { access_token: "test-token" },
      signoutRedirect: vi.fn(),
    });

    render(<App />);

    expect(screen.getByRole("button", { name: /sign out/i })).toBeInTheDocument();
    await waitFor(() => expect(screen.getByText("Priya Admin")).toBeInTheDocument());
    await waitFor(() => expect(screen.getByText("Administrator")).toBeInTheDocument());
  });
});
