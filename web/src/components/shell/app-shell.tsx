"use client";

import { SubifyLogo } from "@/components/brand/logo";
import { SidebarFxRates } from "@/components/shell/sidebar-fx-rates";
import { Button } from "@/components/ui/button";
import { PageLoader } from "@/components/ui/spinner";
import { useAuth } from "@/lib/auth/context";
import { useI18n } from "@/lib/i18n/context";
import { useTheme } from "@/lib/theme/context";
import { cn } from "@/lib/utils";
import {
  Bot,
  CreditCard,
  LayoutDashboard,
  LogOut,
  Mail,
  Menu,
  Moon,
  PieChart,
  Settings,
  Sun,
  User,
  Users,
  X,
} from "lucide-react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";

const nav = [
  { href: "/dashboard", key: "dashboard" as const, icon: LayoutDashboard },
  { href: "/subscriptions", key: "subscriptions" as const, icon: CreditCard },
  { href: "/reports", key: "reports" as const, icon: PieChart },
  { href: "/ai", key: "ai" as const, icon: Bot },
  { href: "/profile", key: "profile" as const, icon: User },
];

const adminNav = [
  { href: "/admin/users", key: "adminUsers" as const, icon: Users },
  {
    href: "/admin/email-templates",
    key: "emailTemplates" as const,
    icon: Mail,
    superAdminOnly: true,
  },
  {
    href: "/admin/settings",
    key: "adminSettings" as const,
    icon: Settings,
    superAdminOnly: true,
  },
];

export function AppShell({ children }: { children: React.ReactNode }) {
  const { user, loading, isAuthenticated, isAdmin, isSuperAdmin, logout } =
    useAuth();
  const { t, locale, setLocale } = useI18n();
  const { resolved, toggle } = useTheme();
  const pathname = usePathname();
  const router = useRouter();
  const [open, setOpen] = useState(false);

  useEffect(() => {
    if (!loading && !isAuthenticated) {
      router.replace(`/login?next=${encodeURIComponent(pathname)}`);
    }
  }, [loading, isAuthenticated, router, pathname]);

  if (loading || !isAuthenticated) {
    return <PageLoader />;
  }

  const links = [
    ...nav,
    ...(isAdmin
      ? adminNav.filter(
          (l) => !("superAdminOnly" in l && l.superAdminOnly) || isSuperAdmin,
        )
      : []),
  ];

  const NavLinks = ({ onNavigate }: { onNavigate?: () => void }) => (
    <nav className="flex flex-col gap-1 px-3 py-2">
      {links.map((item) => {
        const Icon = item.icon;
        const active =
          pathname === item.href || pathname.startsWith(item.href + "/");
        return (
          <Link
            key={item.href}
            href={item.href}
            onClick={onNavigate}
            className={cn(
              "group flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-all",
              active
                ? "bg-primary text-white shadow-[var(--shadow-glow)]"
                : "text-muted hover:bg-primary-soft hover:text-foreground",
            )}
          >
            <Icon
              className={cn(
                "h-4 w-4 shrink-0",
                active ? "text-white" : "text-muted group-hover:text-primary",
              )}
            />
            {t(item.key)}
          </Link>
        );
      })}
    </nav>
  );

  const sidebarFooter = (
    <div className="mt-auto shrink-0">
      <SidebarFxRates />
      <div className="border-t border-border p-3">
        <div className="flex items-center gap-3 rounded-xl px-2 py-2">
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-primary-soft text-sm font-semibold text-primary">
            {(user?.fullName || user?.email || "?").charAt(0).toUpperCase()}
          </div>
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-medium">
              {user?.fullName || user?.email}
            </p>
            <p className="truncate text-xs text-muted">{user?.email}</p>
          </div>
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8 shrink-0"
            onClick={() => logout()}
            aria-label={t("logout")}
          >
            <LogOut className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );

  return (
    <div className="flex min-h-screen bg-background text-foreground">
      {/* Desktop sidebar — mockup style */}
      <aside className="hidden w-[260px] shrink-0 border-r border-border bg-sidebar md:flex md:flex-col">
        <div className="flex h-16 items-center px-4">
          <Link href="/dashboard">
            <SubifyLogo wordmark={t("appName")} />
          </Link>
        </div>
        <div className="flex-1 overflow-y-auto py-1">
          <NavLinks />
        </div>
        {sidebarFooter}
      </aside>

      {/* Mobile drawer */}
      {open ? (
        <div className="fixed inset-0 z-40 md:hidden">
          <button
            className="absolute inset-0 bg-black/50 backdrop-blur-sm"
            aria-label="Close menu"
            onClick={() => setOpen(false)}
          />
          <aside className="absolute left-0 top-0 flex h-full w-[280px] flex-col bg-sidebar shadow-2xl">
            <div className="flex h-16 items-center justify-between px-4">
              <SubifyLogo wordmark={t("appName")} />
              <Button
                variant="ghost"
                size="icon"
                onClick={() => setOpen(false)}
              >
                <X className="h-5 w-5" />
              </Button>
            </div>
            <div className="flex-1 overflow-y-auto">
              <NavLinks onNavigate={() => setOpen(false)} />
            </div>
            {sidebarFooter}
          </aside>
        </div>
      ) : null}

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-30 flex h-16 items-center justify-between gap-3 border-b border-border bg-surface/80 px-4 backdrop-blur-md md:px-6">
          <div className="flex items-center gap-2">
            <Button
              variant="ghost"
              size="icon"
              className="md:hidden"
              onClick={() => setOpen(true)}
            >
              <Menu className="h-5 w-5" />
            </Button>
            <span className="font-semibold md:hidden">{t("appName")}</span>
          </div>
          <div className="flex items-center gap-2">
            <select
              className="h-9 rounded-full border border-border bg-surface px-3 text-sm"
              value={locale}
              onChange={(e) => setLocale(e.target.value as "tr" | "en")}
              aria-label={t("locale")}
            >
              <option value="tr">TR</option>
              <option value="en">EN</option>
            </select>
            <Button
              variant="ghost"
              size="icon"
              onClick={toggle}
              aria-label="Theme"
            >
              {resolved === "dark" ? (
                <Sun className="h-4 w-4" />
              ) : (
                <Moon className="h-4 w-4" />
              )}
            </Button>
            <div className="hidden items-center gap-2 sm:flex">
              <span className="max-w-[160px] truncate text-sm text-muted">
                {user?.fullName || user?.email}
              </span>
            </div>
          </div>
        </header>
        <main className="flex-1 p-4 md:p-6 lg:p-8">{children}</main>
      </div>
    </div>
  );
}
