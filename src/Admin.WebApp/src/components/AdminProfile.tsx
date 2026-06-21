import { useEffect, useState } from "react";
import { getMe, type AdminUser } from "@/lib/api";

type State =
  | { kind: "loading" }
  | { kind: "ok"; user: AdminUser }
  | { kind: "error"; message: string };

/** Fetches the current administrator from the secured BFF endpoint, proving JWT propagation works. */
export function AdminProfile({ accessToken }: { accessToken: string }) {
  const [state, setState] = useState<State>({ kind: "loading" });

  useEffect(() => {
    const controller = new AbortController();
    getMe(accessToken, controller.signal)
      .then((user) => setState({ kind: "ok", user }))
      .catch((err: unknown) => {
        if (controller.signal.aborted) return;
        setState({ kind: "error", message: err instanceof Error ? err.message : "Unknown error" });
      });
    return () => controller.abort();
  }, [accessToken]);

  if (state.kind === "loading") {
    return <p className="text-sm text-muted-foreground">Loading profile…</p>;
  }

  if (state.kind === "error") {
    return <p className="text-sm text-destructive">Could not load profile — {state.message}</p>;
  }

  return (
    <div className="space-y-1">
      <p className="text-sm font-medium text-foreground">{state.user.name}</p>
      <p className="text-xs text-muted-foreground">
        {state.user.roles.length > 0 ? state.user.roles.join(", ") : "No roles"}
      </p>
    </div>
  );
}
