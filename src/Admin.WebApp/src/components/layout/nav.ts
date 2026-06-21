import { BarChart3, Boxes, LayoutGrid, type LucideIcon, Package, Settings } from "lucide-react";

export interface NavItem {
  to: string;
  label: string;
  icon: LucideIcon;
}

/** Primary navigation, mirroring the Figma sidebar order. */
export const navItems: NavItem[] = [
  { to: "/dashboard", label: "Dashboard", icon: LayoutGrid },
  { to: "/sales", label: "Sales Analytics", icon: BarChart3 },
  { to: "/products", label: "Products", icon: Package },
  { to: "/inventory", label: "Inventory", icon: Boxes },
  { to: "/settings", label: "Settings", icon: Settings },
];
