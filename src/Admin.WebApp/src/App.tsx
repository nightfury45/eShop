import { HealthCheck } from "@/components/HealthCheck";

/**
 * Epic 0 placeholder shell. The full Sidebar/Topbar/Content app shell and screens arrive in later
 * epics; for now this proves the theme (tokens) and the SPA -> BFF round-trip render correctly.
 */
export default function App() {
  return (
    <main className="grid min-h-dvh place-items-center p-6">
      <section className="w-full max-w-md rounded-lg border bg-card p-8 shadow-sm">
        <div className="flex items-baseline gap-2">
          <h1 className="text-lg font-semibold tracking-tight text-foreground">eShop</h1>
          <span className="text-xs font-medium uppercase tracking-widest text-muted-foreground">
            Admin
          </span>
        </div>
        <p className="mt-2 text-sm text-muted-foreground">
          Dashboard foundations are in place. Screens land in upcoming epics.
        </p>
        <div className="mt-6 border-t pt-4">
          <HealthCheck />
        </div>
      </section>
    </main>
  );
}
