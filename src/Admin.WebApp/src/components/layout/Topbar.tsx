import { CircleHelp, LogOut, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { HealthCheck } from "@/components/HealthCheck";

/** Fixed 64px top bar: global search, BFF health, help, and sign-out. */
export function Topbar({ onSignOut }: { onSignOut: () => void }) {
  return (
    <header className="flex h-16 shrink-0 items-center gap-4 border-b bg-card px-6">
      <div className="relative w-full max-w-sm">
        <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          placeholder="Search products, orders, SKUs…"
          aria-label="Search"
          className="h-9 pl-9"
        />
      </div>

      <div className="ml-auto flex items-center gap-4">
        <HealthCheck />
        <Button variant="ghost" size="icon" className="size-9" aria-label="Help">
          <CircleHelp className="size-4" />
        </Button>
        <Button variant="secondary" size="sm" onClick={onSignOut}>
          <LogOut className="size-4" />
          Sign out
        </Button>
      </div>
    </header>
  );
}
