import { NavLink } from "react-router-dom";
import { AdminProfile } from "@/components/AdminProfile";
import { navItems } from "@/components/layout/nav";
import { cn } from "@/lib/utils";

/** Fixed 240px sidebar: brand, primary navigation, and the signed-in administrator. */
export function Sidebar({ accessToken }: { accessToken: string }) {
  return (
    <aside className="flex h-dvh w-60 shrink-0 flex-col border-r bg-card">
      <div className="flex h-16 items-center gap-2 px-6">
        <span className="grid size-7 place-items-center rounded-md bg-foreground text-sm font-semibold text-background">
          e
        </span>
        <span className="text-sm font-semibold tracking-tight text-foreground">eShop</span>
        <span className="text-[10px] font-medium uppercase tracking-widest text-muted-foreground">Admin</span>
      </div>

      <nav className="flex-1 space-y-1 px-3 py-2" aria-label="Primary">
        <p className="px-3 pb-2 text-[10px] font-medium uppercase tracking-widest text-muted-foreground">Menu</p>
        {navItems.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              cn(
                "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                isActive
                  ? "bg-foreground text-background"
                  : "text-muted-foreground hover:bg-secondary hover:text-foreground",
              )
            }
          >
            <Icon className="size-[18px] shrink-0" aria-hidden />
            {label}
          </NavLink>
        ))}
      </nav>

      <div className="border-t p-4">
        <AdminProfile accessToken={accessToken} />
      </div>
    </aside>
  );
}
