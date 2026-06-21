import { useEffect, useState } from "react";
import { getPing } from "@/lib/api";
import { cn } from "@/lib/utils";

type Status =
  | { kind: "loading" }
  | { kind: "ok"; utc: string }
  | { kind: "error"; message: string };

/**
 * Epic 0 round-trip probe: calls the Admin BFF `/api/admin/ping` and surfaces connectivity.
 * Proves the SPA -> Vite proxy -> BFF pipe end-to-end.
 */
export function HealthCheck() {
  const [status, setStatus] = useState<Status>({ kind: "loading" });

  useEffect(() => {
    const controller = new AbortController();
    getPing(controller.signal)
      .then((res) => setStatus({ kind: "ok", utc: res.utc }))
      .catch((err: unknown) => {
        if (controller.signal.aborted) return;
        setStatus({
          kind: "error",
          message: err instanceof Error ? err.message : "Unknown error",
        });
      });
    return () => controller.abort();
  }, []);

  const dotColor =
    status.kind === "ok"
      ? "bg-positive"
      : status.kind === "error"
        ? "bg-destructive"
        : "bg-muted-foreground/40";

  return (
    <div className="flex items-center gap-2.5 text-sm">
      <span
        aria-hidden
        className={cn("size-2 rounded-full", dotColor, status.kind === "loading" && "animate-pulse")}
      />
      {status.kind === "loading" && (
        <span className="text-muted-foreground">Checking BFF…</span>
      )}
      {status.kind === "ok" && (
        <span className="text-muted-foreground">
          BFF connected · <time dateTime={status.utc}>{new Date(status.utc).toLocaleTimeString()}</time>
        </span>
      )}
      {status.kind === "error" && (
        <span className="text-destructive">Unable to reach BFF — {status.message}</span>
      )}
    </div>
  );
}
