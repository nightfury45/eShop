import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { HealthCheck } from "./HealthCheck";

afterEach(() => {
  vi.restoreAllMocks();
});

function mockFetch(response: Response) {
  vi.spyOn(globalThis, "fetch").mockResolvedValue(response);
}

describe("HealthCheck", () => {
  it("reports a connected BFF when ping succeeds", async () => {
    mockFetch(
      new Response(JSON.stringify({ status: "ok", utc: "2026-06-19T12:00:00Z" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );

    render(<HealthCheck />);

    await waitFor(() => expect(screen.getByText(/BFF connected/i)).toBeInTheDocument());
  });

  it("reports an error when the BFF returns a failure status", async () => {
    mockFetch(new Response("nope", { status: 500 }));

    render(<HealthCheck />);

    await waitFor(() => expect(screen.getByText(/Unable to reach BFF/i)).toBeInTheDocument());
  });
});
