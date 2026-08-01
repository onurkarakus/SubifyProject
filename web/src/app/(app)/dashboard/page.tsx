"use client";

import { CategoryDonut } from "@/components/dashboard/category-donut";
import { SpendTrendChart } from "@/components/dashboard/spend-trend-chart";
import { StatCard } from "@/components/dashboard/stat-card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { PageLoader } from "@/components/ui/spinner";
import { api, ApiError } from "@/lib/api/client";
import type {
  ActivityItem,
  CategoryBreakdownResponse,
  ListSubscriptionsResponse,
  MonthlySpendResponse,
  UpcomingItem,
} from "@/lib/api/types";
import { useAuth } from "@/lib/auth/context";
import { useI18n } from "@/lib/i18n/context";
import { cn, formatDate, formatMoney } from "@/lib/utils";
import {
  Bell,
  Bot,
  CreditCard,
  Percent,
  Plus,
  Sparkles,
  TrendingDown,
  TrendingUp,
  Wallet,
} from "lucide-react";
import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { toast } from "sonner";

function brandColor(name: string): string {
  const palette = [
    "#E50914",
    "#1DB954",
    "#FF9900",
    "#7C3AED",
    "#3B82F6",
    "#EC4899",
    "#14B8A6",
  ];
  let h = 0;
  for (let i = 0; i < name.length; i++) h = (h + name.charCodeAt(i) * 17) % 997;
  return palette[h % palette.length];
}

export default function DashboardPage() {
  const { t, locale } = useI18n();
  const { user } = useAuth();
  const [loading, setLoading] = useState(true);
  const [subs, setSubs] = useState<ListSubscriptionsResponse | null>(null);
  const [upcoming, setUpcoming] = useState<UpcomingItem[]>([]);
  const [activity, setActivity] = useState<ActivityItem[]>([]);
  const [monthly, setMonthly] = useState<MonthlySpendResponse | null>(null);
  const [categories, setCategories] =
    useState<CategoryBreakdownResponse | null>(null);

  useEffect(() => {
    void (async () => {
      try {
        const [s, u, a, m, c] = await Promise.all([
          api.get<ListSubscriptionsResponse>(
            "/subscriptions?page=1&pageSize=5",
          ),
          api.get<{ data: UpcomingItem[] }>("/subscriptions/upcoming?days=30"),
          api.get<{ data: ActivityItem[] }>("/activity?page=1&pageSize=8"),
          api.get<MonthlySpendResponse>("/reports/monthly-spend?months=6"),
          api.get<CategoryBreakdownResponse>("/reports/category-breakdown"),
        ]);
        setSubs(s);
        setUpcoming(u.data ?? (u as unknown as UpcomingItem[]));
        setActivity(a.data ?? []);
        setMonthly(m);
        setCategories(c);
      } catch (e) {
        toast.error(e instanceof ApiError ? e.message : t("errorGeneric"));
      } finally {
        setLoading(false);
      }
    })();
  }, [t]);

  const monthOverMonth = useMemo(() => {
    const pts = monthly?.data ?? [];
    if (pts.length < 2) return null;
    const prev = pts[pts.length - 2]?.total ?? 0;
    const curr = pts[pts.length - 1]?.total ?? 0;
    if (prev <= 0) return curr > 0 ? 100 : 0;
    return Math.round(((curr - prev) / prev) * 100);
  }, [monthly]);

  if (loading) return <PageLoader />;

  const summary = subs?.summary;
  const currency = summary?.currency ?? monthly?.currency ?? "TRY";
  const budget = summary?.monthlyBudget;
  const monthlyTotal = summary?.monthlyTotal ?? 0;
  const activeCount = subs?.pagination?.totalItems ?? subs?.data?.length ?? 0;
  const budgetPct =
    budget && budget > 0
      ? Math.min(100, Math.round((monthlyTotal / budget) * 100))
      : null;
  const firstName =
    user?.fullName?.trim().split(/\s+/)[0] ||
    user?.email?.split("@")[0] ||
    "";

  const mom = monthOverMonth;
  const momUp = mom != null && mom > 0;
  const momDown = mom != null && mom < 0;

  return (
    <div className="mx-auto max-w-[1400px] space-y-5">
      {/* Header row — greeting + soft actions */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="page-title">
            {t("dashboardHello")}
            {firstName ? `, ${firstName}` : ""}{" "}
            <span aria-hidden>👋</span>
          </h1>
          <p className="mt-1 text-sm text-muted">{t("dashboardHelloHint")}</p>
        </div>
        <div className="flex items-center gap-2">
          <Link
            href="/subscriptions"
            className="relative inline-flex h-10 w-10 items-center justify-center rounded-full border border-border bg-surface text-muted hover:text-foreground"
            aria-label={t("upcoming")}
          >
            <Bell className="h-4 w-4" />
            {upcoming.some((x) => x.isUpcoming || x.isOverdue) ? (
              <span className="absolute right-2 top-2 h-2 w-2 rounded-full bg-danger" />
            ) : null}
          </Link>
        </div>
      </div>

      {summary?.isBudgetExceeded ? (
        <div className="rounded-2xl border border-danger/40 bg-danger/10 px-4 py-3 text-sm text-danger">
          {t("budgetExceeded")}
        </div>
      ) : null}

      {/* 12-col mockup grid */}
      <div className="grid gap-4 lg:grid-cols-12">
        {/* —— Main column —— */}
        <div className="flex flex-col gap-4 lg:col-span-8">
          {/* KPI row */}
          <div className="grid gap-3 sm:grid-cols-3">
            <StatCard
              label={t("monthlyTotal")}
              icon={Wallet}
              value={formatMoney(monthlyTotal, currency, locale)}
              hint={
                mom != null ? (
                  <span
                    className={cn(
                      "inline-flex items-center gap-1 font-medium",
                      momUp && "text-danger",
                      momDown && "text-success",
                      !momUp && !momDown && "text-muted",
                    )}
                  >
                    {momUp ? (
                      <TrendingUp className="h-3.5 w-3.5" />
                    ) : momDown ? (
                      <TrendingDown className="h-3.5 w-3.5" />
                    ) : null}
                    {mom > 0 ? "+" : ""}
                    {mom}% {t("vsLastMonth")}
                  </span>
                ) : (
                  <span className="text-muted">{t("dashboardHelloHint")}</span>
                )
              }
            />
            <StatCard
              label={t("activeSubscriptions")}
              icon={CreditCard}
              value={activeCount}
              hint={
                <span className="text-muted">
                  {t("yearlyTotal")}:{" "}
                  {formatMoney(summary?.yearlyTotal ?? 0, currency, locale)}
                </span>
              }
            />
            <StatCard
              label={t("budgetUsage")}
              icon={Percent}
              value={budget != null ? `${budgetPct ?? 0}%` : "—"}
              hint={
                budget != null ? (
                  <div className="space-y-1.5">
                    <div className="h-1.5 overflow-hidden rounded-full bg-border">
                      <div
                        className={cn(
                          "h-full rounded-full transition-all",
                          (budgetPct ?? 0) >= 90
                            ? "bg-warning"
                            : summary?.isBudgetExceeded
                              ? "bg-danger"
                              : "bg-primary",
                        )}
                        style={{ width: `${budgetPct ?? 0}%` }}
                      />
                    </div>
                    <span
                      className={cn(
                        "text-muted",
                        (budgetPct ?? 0) >= 90 && "text-warning",
                      )}
                    >
                      {(budgetPct ?? 0) >= 90
                        ? t("budgetNearLimit")
                        : `${formatMoney(monthlyTotal, currency, locale)} / ${formatMoney(budget, currency, locale)}`}
                    </span>
                  </div>
                ) : (
                  <Link
                    href="/profile"
                    className="font-medium text-primary hover:underline"
                  >
                    {t("setBudget")}
                  </Link>
                )
              }
            />
          </div>

          {/* Monthly trend — full width of main */}
          <Card className="overflow-hidden">
            <CardHeader className="flex-row items-center justify-between space-y-0 pb-0">
              <CardTitle className="text-base">
                {t("monthlySpendTrend")}
              </CardTitle>
              <Link
                href="/reports"
                className="text-xs font-medium text-primary hover:underline"
              >
                {t("reports")}
              </Link>
            </CardHeader>
            <CardContent className="pt-2">
              <SpendTrendChart
                data={monthly?.data ?? []}
                currency={monthly?.currency ?? currency}
              />
            </CardContent>
          </Card>

          {/* Category + Upcoming */}
          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">
                  {t("categoryBreakdown")}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <CategoryDonut data={categories?.data ?? []} />
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex-row items-center justify-between space-y-0">
                <CardTitle className="text-base">{t("upcoming")}</CardTitle>
                <Link
                  href="/subscriptions"
                  className="text-xs font-medium text-primary hover:underline"
                >
                  {t("viewAll")}
                </Link>
              </CardHeader>
              <CardContent className="space-y-2">
                {upcoming.length === 0 ? (
                  <EmptyState title={t("empty")} className="py-8" />
                ) : (
                  upcoming.slice(0, 5).map((item) => (
                    <Link
                      key={item.id}
                      href={`/subscriptions/${item.id}`}
                      className={cn(
                        "flex items-center gap-3 rounded-xl border border-border px-3 py-2.5 transition hover:border-primary/30 hover:bg-primary-soft/30",
                        item.isOverdue && "card-overdue",
                        item.isUpcoming && !item.isOverdue && "card-soon",
                      )}
                    >
                      <span
                        className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl text-xs font-bold text-white"
                        style={{ background: brandColor(item.name) }}
                      >
                        {item.name.slice(0, 1).toUpperCase()}
                      </span>
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-medium">
                          {item.name}
                        </p>
                        <p className="text-xs text-muted">
                          {formatDate(item.nextRenewalDate, locale)}
                        </p>
                      </div>
                      <div className="flex shrink-0 flex-col items-end gap-0.5">
                        {item.isOverdue ? (
                          <Badge variant="danger">{t("overdue")}</Badge>
                        ) : item.isUpcoming ? (
                          <Badge variant="warning">{t("soon")}</Badge>
                        ) : null}
                        <span className="text-sm font-semibold tabular-nums">
                          {formatMoney(item.price, item.currency, locale)}
                        </span>
                      </div>
                    </Link>
                  ))
                )}
              </CardContent>
            </Card>
          </div>
        </div>

        {/* —— Right rail (mockup) —— */}
        <div className="flex flex-col gap-4 lg:col-span-4">
          <Card>
            <CardHeader>
              <CardTitle className="text-base">{t("quickActions")}</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-2.5">
              <Link href="/subscriptions/new" className="block">
                <Button className="h-11 w-full justify-start rounded-xl px-4">
                  <Plus className="h-4 w-4" />
                  {t("addSubscription")}
                </Button>
              </Link>
              <Link href="/profile" className="block">
                <Button
                  variant="secondary"
                  className="h-11 w-full justify-start rounded-xl px-4"
                >
                  <Wallet className="h-4 w-4" />
                  {t("setBudget")}
                </Button>
              </Link>
              <Link href="/ai" className="block">
                <Button
                  variant="secondary"
                  className="h-11 w-full justify-start rounded-xl px-4"
                >
                  <Sparkles className="h-4 w-4" />
                  {t("runAiAnalysis")}
                </Button>
              </Link>
              <Link href="/profile" className="block">
                <Button
                  variant="outline"
                  className="h-11 w-full justify-start rounded-xl px-4"
                >
                  <Bot className="h-4 w-4" />
                  {t("createReminder")}
                </Button>
              </Link>
            </CardContent>
          </Card>

          <Card className="flex min-h-0 flex-1 flex-col">
            <CardHeader>
              <CardTitle className="text-base">{t("recentActivity")}</CardTitle>
            </CardHeader>
            <CardContent className="flex-1 space-y-0">
              {activity.length === 0 ? (
                <EmptyState title={t("empty")} className="py-8" />
              ) : (
                <ul className="space-y-0">
                  {activity.map((a, idx) => (
                    <li
                      key={a.id}
                      className={cn(
                        "flex gap-3 py-3",
                        idx < activity.length - 1 && "border-b border-border",
                      )}
                    >
                      <span className="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-primary-soft text-primary">
                        <Bell className="h-3.5 w-3.5" />
                      </span>
                      <div className="min-w-0">
                        <p className="text-sm leading-snug">
                          {a.description || a.action}
                        </p>
                        <p className="mt-0.5 text-xs text-muted">
                          {formatDate(a.createdAt, locale)}
                        </p>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
