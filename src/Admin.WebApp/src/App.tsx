import { useAuth } from "react-oidc-context";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AppLayout } from "@/components/layout/AppLayout";
import { DashboardPage } from "@/pages/Dashboard";
import { SalesAnalyticsPage } from "@/pages/SalesAnalytics";
import { ProductsPage } from "@/pages/Products";
import { InventoryPage } from "@/pages/Inventory";
import { SettingsPage } from "@/pages/Settings";

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
          className="mt-6 inline-flex h-10 w-full items-center justify-center rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background"
        >
          Sign in
        </button>
      </CenteredCard>
    );
  }

  const accessToken = auth.user?.access_token ?? "";

  return (
    <BrowserRouter>
      <Routes>
        <Route element={<AppLayout accessToken={accessToken} onSignOut={() => void auth.signoutRedirect()} />}>
          <Route index element={<Navigate to="/dashboard" replace />} />
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="sales" element={<SalesAnalyticsPage />} />
          <Route path="products" element={<ProductsPage />} />
          <Route path="inventory" element={<InventoryPage />} />
          <Route path="settings" element={<SettingsPage />} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
