import { Outlet } from "react-router-dom";
import { Sidebar } from "@/components/layout/Sidebar";
import { Topbar } from "@/components/layout/Topbar";

export interface AppLayoutProps {
  accessToken: string;
  onSignOut: () => void;
}

/** App shell: 240px sidebar + main column (64px top bar over scrollable content). */
export function AppLayout({ accessToken, onSignOut }: AppLayoutProps) {
  return (
    <div className="flex h-dvh overflow-hidden bg-background">
      <Sidebar accessToken={accessToken} />
      <div className="flex min-w-0 flex-1 flex-col">
        <Topbar onSignOut={onSignOut} />
        <main className="flex-1 overflow-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
