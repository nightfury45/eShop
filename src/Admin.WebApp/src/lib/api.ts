/** Response of the Admin BFF liveness probe (`GET /api/admin/ping`). */
export interface PingResponse {
  status: string;
  utc: string;
}

/** Calls the Admin BFF ping endpoint through the Vite dev proxy / Aspire service discovery. */
export async function getPing(signal?: AbortSignal): Promise<PingResponse> {
  const res = await fetch("/api/admin/ping", { signal });
  if (!res.ok) {
    throw new Error(`Admin BFF ping failed with status ${res.status}`);
  }
  return (await res.json()) as PingResponse;
}
