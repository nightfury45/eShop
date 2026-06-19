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

/** OIDC client configuration the SPA needs to start a login, served by the BFF. */
export interface AdminClientConfig {
  authority: string;
  clientId: string;
  scope: string;
}

/** The authenticated administrator, as projected by the BFF from the validated JWT. */
export interface AdminUser {
  subject: string;
  name: string;
  roles: string[];
}

/** Fetches the OIDC client configuration (anonymous endpoint). */
export async function getAdminConfig(signal?: AbortSignal): Promise<AdminClientConfig> {
  const res = await fetch("/api/admin/config", { signal });
  if (!res.ok) {
    throw new Error(`Admin config failed with status ${res.status}`);
  }
  return (await res.json()) as AdminClientConfig;
}

/** Fetches the current administrator profile from the secured BFF endpoint. */
export async function getMe(accessToken: string, signal?: AbortSignal): Promise<AdminUser> {
  const res = await fetch("/api/admin/me", {
    headers: { Authorization: `Bearer ${accessToken}` },
    signal,
  });
  if (!res.ok) {
    throw new Error(`Admin profile failed with status ${res.status}`);
  }
  return (await res.json()) as AdminUser;
}
