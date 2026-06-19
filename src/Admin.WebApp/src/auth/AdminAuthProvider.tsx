import { useEffect, useState, type ReactNode } from "react";
import { AuthProvider } from "react-oidc-context";
import { WebStorageStateStore } from "oidc-client-ts";
import { getAdminConfig, type AdminClientConfig } from "@/lib/api";

// After the OIDC redirect completes, strip the code/state query params from the URL.
function onSigninCallback() {
  window.history.replaceState({}, document.title, window.location.pathname);
}

/**
 * Loads the OIDC client configuration from the BFF at runtime (so the SPA needs no build-time secrets),
 * then mounts react-oidc-context's AuthProvider for an Authorization Code + PKCE flow against Identity.API.
 */
export function AdminAuthProvider({ children }: { children: ReactNode }) {
  const [config, setConfig] = useState<AdminClientConfig | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    getAdminConfig(controller.signal)
      .then(setConfig)
      .catch((err: unknown) => {
        if (controller.signal.aborted) return;
        setError(err instanceof Error ? err.message : "Unknown error");
      });
    return () => controller.abort();
  }, []);

  if (error) {
    return (
      <div className="grid min-h-dvh place-items-center p-6 text-sm text-destructive">
        Failed to load authentication configuration — {error}
      </div>
    );
  }

  if (!config) {
    return (
      <div className="grid min-h-dvh place-items-center p-6 text-sm text-muted-foreground">
        Loading…
      </div>
    );
  }

  return (
    <AuthProvider
      authority={config.authority}
      client_id={config.clientId}
      redirect_uri={`${window.location.origin}/callback`}
      post_logout_redirect_uri={window.location.origin}
      scope={config.scope}
      response_type="code"
      userStore={new WebStorageStateStore({ store: window.localStorage })}
      onSigninCallback={onSigninCallback}
    >
      {children}
    </AuthProvider>
  );
}
