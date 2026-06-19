import { useAuth } from "react-oidc-context";
import { AdminProfile } from "@/components/AdminProfile";
import { HealthCheck } from "@/components/HealthCheck";

function CenteredCard({ children }: { children: React.ReactNode }) {
  return (
    <main className="grid min-h-dvh place-items-center p-6">
      <section className="w-full max-w-md rounded-lg border bg-card p-8 shadow-sm">{children}</section>
    </main>
  );
}

function Brand() {
  return (
    <div className="flex items-baseline gap-2">
      <h1 className="text-lg font-semibold tracking-tight text-foreground">eShop</h1>
      <span className="text-xs font-medium uppercase tracking-widest text-muted-foreground">Admin</span>
    </div>
  );
}

export default function App() {
  const auth = useAuth();

  if (auth.isLoading) {
    return (
      <CenteredCard>
        <p className="text-sm text-muted-foreground">Signing in…</p>
      </CenteredCard>
    );
  }

  if (auth.error) {
    return (
      <CenteredCard>
        <p className="text-sm text-destructive">Authentication error — {auth.error.message}</p>
      </CenteredCard>
    );
  }

  if (!auth.isAuthenticated) {
    return (
      <CenteredCard>
        <Brand />
        <p className="mt-2 text-sm text-muted-foreground">Sign in to manage your store.</p>
        <button
          type="button"
          onClick={() => void auth.signinRedirect()}
          className="mt-6 inline-flex h-10 w-full items-center justify-center rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
        >
          Sign in
        </button>
      </CenteredCard>
    );
  }

  return (
    <main className="min-h-dvh p-6">
      <header className="mx-auto flex max-w-3xl items-center justify-between">
        <Brand />
        <button
          type="button"
          onClick={() => void auth.signoutRedirect()}
          className="inline-flex h-9 items-center justify-center rounded-md border bg-card px-3 text-sm font-medium text-foreground transition-colors hover:bg-secondary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
        >
          Sign out
        </button>
      </header>

      <section className="mx-auto mt-8 max-w-3xl space-y-6">
        <div className="rounded-lg border bg-card p-6 shadow-sm">
          <h2 className="text-sm font-medium text-muted-foreground">Signed in as</h2>
          <div className="mt-3">
            <AdminProfile accessToken={auth.user?.access_token ?? ""} />
          </div>
        </div>

        <div className="rounded-lg border bg-card p-6 shadow-sm">
          <HealthCheck />
        </div>
      </section>
    </main>
  );
}
