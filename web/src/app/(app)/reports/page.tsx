"use client";

import { CategoryDonut } from "@/components/dashboard/category-donut";
import { SpendTrendChart } from "@/components/dashboard/spend-trend-chart";
import { StatCard } from "@/components/dashboard/stat-card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { FxStatusBanner } from "@/components/fx/fx-status-banner";
import { MoneyDual } from "@/components/ui/money-dual";
import { PageLoader } from "@/components/ui/spinner";
import { Tabs } from "@/components/ui/tabs";
import { api, ApiError } from "@/lib/api/client";
import type {
  AiReportCommentaryResponse,
  CategoryBreakdownResponse,
  CurrencyDistributionResponse,
  ListSubscriptionsResponse,
  MonthlySpendResponse,
  SendReportSummaryResponse,
  SubscriptionItem,
  UpcomingItem,
  UpcomingResponse,
} from "@/lib/api/types";
import {
  convertCurrency,
  formatMoneyDual,
  type FxRatesSnapshot,
} from "@/lib/fx/money-dual";
import { useFxRates } from "@/lib/fx/use-fx-rates";
import { useI18n } from "@/lib/i18n/context";
import type { MessageKey } from "@/lib/i18n/messages";
import {
  downloadTextFile,
  stampFilename,
  toCsv,
} from "@/lib/reports/export-csv";
import { downloadUpcomingIcs } from "@/lib/subscriptions/ics";
import { computeWhatIf } from "@/lib/subscriptions/what-if";
import {
  cn,
  formatDate,
  formatMoney,
  normalizeBillingCycle,
} from "@/lib/utils";
import {
  AlertTriangle,
  ArrowDownRight,
  ArrowUpRight,
  CalendarDays,
  CreditCard,
  Download,
  FileSpreadsheet,
  Mail,
  Minus,
  Percent,
  Printer,
  Sparkles,
  TrendingUp,
  Wallet,
} from "lucide-react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import {
  Suspense,
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";
import { toast } from "sonner";

type ReportsTab =
  | "overview"
  | "currency"
  | "subscriptions"
  | "cashflow"
  | "budget"
  | "ai"
  | "export";

type PeriodMonths = 3 | 6 | 12;
type CashflowDays = 30 | 60 | 90;

function parseTab(raw: string | null): ReportsTab {
  const allowed: ReportsTab[] = [
    "overview",
    "currency",
    "subscriptions",
    "cashflow",
    "budget",
    "ai",
    "export",
  ];
  if (raw && (allowed as string[]).includes(raw)) return raw as ReportsTab;
  return "overview";
}

function parsePeriod(raw: string | null): PeriodMonths {
  if (raw === "3" || raw === "12") return Number(raw) as PeriodMonths;
  return 6;
}

function parseCashflowDays(raw: string | null): CashflowDays {
  if (raw === "60" || raw === "90") return Number(raw) as CashflowDays;
  return 30;
}

/** Group renewals into week buckets starting from today (UTC date). */
function buildWeeklyDensity(items: UpcomingItem[], horizonDays: number) {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const weeks = Math.ceil(horizonDays / 7);
  const buckets: {
    index: number;
    start: Date;
    end: Date;
    count: number;
    label: string;
  }[] = [];
  for (let i = 0; i < weeks; i++) {
    const start = new Date(today);
    start.setDate(start.getDate() + i * 7);
    const end = new Date(start);
    end.setDate(end.getDate() + 6);
    buckets.push({
      index: i,
      start,
      end,
      count: 0,
      label: start.toISOString().slice(0, 10),
    });
  }
  for (const item of items) {
    if (item.isOverdue) {
      // count overdue in first bucket as pressure
      if (buckets[0]) buckets[0].count += 1;
      continue;
    }
    const d = item.daysUntilRenewal;
    if (d < 0 || d > horizonDays) continue;
    const idx = Math.min(weeks - 1, Math.floor(d / 7));
    if (buckets[idx]) buckets[idx].count += 1;
  }
  return buckets;
}

function ComingSoonCard({ title, body }: { title: string; body: string }) {
  return (
    <Card>
      <CardContent className="flex flex-col items-center justify-center gap-2 py-16 text-center">
        <Badge variant="muted">{title}</Badge>
        <p className="max-w-md text-sm text-muted">{body}</p>
      </CardContent>
    </Card>
  );
}

function ReportsPageInner() {
  const { t, locale } = useI18n();
  const router = useRouter();
  const searchParams = useSearchParams();
  const tab = parseTab(searchParams.get("tab"));
  const period = parsePeriod(searchParams.get("months"));
  const cashflowDays = parseCashflowDays(searchParams.get("days"));

  const [loading, setLoading] = useState(true);
  const [cashflowLoading, setCashflowLoading] = useState(false);
  const [aiBusy, setAiBusy] = useState(false);
  const [aiKeyMissing, setAiKeyMissing] = useState(false);
  const [aiCommentary, setAiCommentary] =
    useState<AiReportCommentaryResponse | null>(null);
  const [emailBusy, setEmailBusy] = useState(false);
  const [whatIfExclude, setWhatIfExclude] = useState<Set<string>>(
    () => new Set(),
  );
  const [whatIfYearly, setWhatIfYearly] = useState<Set<string>>(
    () => new Set(),
  );
  const [monthly, setMonthly] = useState<MonthlySpendResponse | null>(null);
  const [categories, setCategories] =
    useState<CategoryBreakdownResponse | null>(null);
  const [currencyDist, setCurrencyDist] =
    useState<CurrencyDistributionResponse | null>(null);
  const [subs, setSubs] = useState<ListSubscriptionsResponse | null>(null);
  const [cashflow, setCashflow] = useState<UpcomingResponse | null>(null);

  const mainCurrencyHint =
    monthly?.currency ??
    subs?.summary?.currency ??
    currencyDist?.currency ??
    null;
  const {
    snapshot: fxRates,
    isStale: fxStale,
    lastUpdated: fxUpdated,
  } = useFxRates(mainCurrencyHint);

  const setQuery = useCallback(
    (patch: {
      tab?: ReportsTab;
      months?: PeriodMonths;
      days?: CashflowDays;
    }) => {
      const q = new URLSearchParams(searchParams.toString());
      if (patch.tab) q.set("tab", patch.tab);
      if (patch.months) q.set("months", String(patch.months));
      if (patch.days) q.set("days", String(patch.days));
      router.replace(`/reports?${q.toString()}`, { scroll: false });
    },
    [router, searchParams],
  );

  const tabs = useMemo(
    () => [
      { id: "overview" as const, label: t("reportsTabOverview") },
      { id: "currency" as const, label: t("reportsTabCurrency") },
      { id: "subscriptions" as const, label: t("reportsTabSubscriptions") },
      { id: "cashflow" as const, label: t("reportsTabCashflow") },
      { id: "budget" as const, label: t("reportsTabBudget") },
      { id: "ai" as const, label: t("reportsTabAi") },
      { id: "export" as const, label: t("reportsTabExport") },
    ],
    [t],
  );

  useEffect(() => {
    void (async () => {
      setLoading(true);
      setAiCommentary(null);
      setAiKeyMissing(false);
      try {
        const [m, c, fx, s] = await Promise.all([
          api.get<MonthlySpendResponse>(
            `/reports/monthly-spend?months=${period}`,
          ),
          api.get<CategoryBreakdownResponse>("/reports/category-breakdown"),
          api.get<CurrencyDistributionResponse>(
            "/reports/currency-distribution",
          ),
          api.get<ListSubscriptionsResponse>(
            "/subscriptions?page=1&pageSize=100&includeArchived=false",
          ),
        ]);
        setMonthly(m);
        setCategories(c);
        setCurrencyDist(fx);
        setSubs(s);
      } catch (e) {
        toast.error(e instanceof ApiError ? e.message : t("errorGeneric"));
      } finally {
        setLoading(false);
      }
    })();
  }, [period, t]);

  async function generateAiCommentary() {
    setAiBusy(true);
    setAiKeyMissing(false);
    try {
      const res = await api.post<AiReportCommentaryResponse>(
        "/ai/report-commentary",
        { months: period, lang: locale },
      );
      setAiCommentary(res);
      toast.success(t("reportsAiGenerate"));
    } catch (e) {
      if (e instanceof ApiError && e.code === "AI_KEY_MISSING") {
        setAiKeyMissing(true);
        toast.error(t("reportsAiKeyMissing"));
      } else {
        toast.error(e instanceof ApiError ? e.message : t("errorGeneric"));
      }
    } finally {
      setAiBusy(false);
    }
  }

  function trendLabel(trend: string): string {
    if (trend === "up") return t("reportsAiTrendUp");
    if (trend === "down") return t("reportsAiTrendDown");
    return t("reportsAiTrendStable");
  }

  async function sendEmailSummary() {
    setEmailBusy(true);
    try {
      const res = await api.post<SendReportSummaryResponse>(
        "/reports/email-summary",
        { months: period, lang: locale },
      );
      toast.success(
        `${t("exportEmailSent")}: ${res.toEmail}`,
      );
    } catch (e) {
      if (e instanceof ApiError && e.code === "SET_003") {
        toast.error(t("exportEmailSmtpMissing"));
      } else {
        toast.error(e instanceof ApiError ? e.message : t("errorGeneric"));
      }
    } finally {
      setEmailBusy(false);
    }
  }

  // Cashflow loads when tab is active or days change (Priority 2)
  useEffect(() => {
    if (tab !== "cashflow") return;
    void (async () => {
      setCashflowLoading(true);
      try {
        const res = await api.get<UpcomingResponse>(
          `/subscriptions/upcoming?days=${cashflowDays}`,
        );
        setCashflow(res);
      } catch (e) {
        toast.error(e instanceof ApiError ? e.message : t("errorGeneric"));
      } finally {
        setCashflowLoading(false);
      }
    })();
  }, [tab, cashflowDays, t]);

  const weeklyDensity = useMemo(
    () => buildWeeklyDensity(cashflow?.data ?? [], cashflowDays),
    [cashflow, cashflowDays],
  );

  const peakWeek = useMemo(() => {
    if (!weeklyDensity.length) return null;
    return weeklyDensity.reduce((a, b) => (b.count > a.count ? b : a));
  }, [weeklyDensity]);

  /** Budget health vs monthly-spend series (Priority 3) */
  const budgetHealth = useMemo(() => {
    const budget = subs?.summary?.monthlyBudget;
    const points = monthly?.data ?? [];
    if (budget == null || budget <= 0) {
      return {
        hasBudget: false as const,
        budget: null as number | null,
        points: [] as {
          month: string;
          total: number;
          over: boolean;
          ratio: number;
        }[],
        monthsOver: 0,
        monthsOk: 0,
        avgHeadroom: 0,
        latest: 0,
        remaining: 0,
        overBy: 0,
        status: "none" as const,
      };
    }
    const analyzed = points.map((p) => {
      const total = p.total;
      const over = total > budget;
      return {
        month: p.month,
        total,
        over,
        ratio: budget > 0 ? total / budget : 0,
      };
    });
    const monthsOver = analyzed.filter((p) => p.over).length;
    const monthsOk = analyzed.length - monthsOver;
    const avgSpend =
      analyzed.length > 0
        ? analyzed.reduce((s, p) => s + p.total, 0) / analyzed.length
        : 0;
    const latest = analyzed.length
      ? analyzed[analyzed.length - 1]!.total
      : (subs?.summary?.monthlyTotal ?? 0);
    const remaining = Math.max(0, budget - latest);
    const overBy = Math.max(0, latest - budget);
    const pct = budget > 0 ? (latest / budget) * 100 : 0;
    const status =
      latest > budget
        ? ("over" as const)
        : pct >= 90
          ? ("near" as const)
          : ("ok" as const);
    return {
      hasBudget: true as const,
      budget,
      points: analyzed,
      monthsOver,
      monthsOk,
      avgHeadroom: budget - avgSpend,
      latest,
      remaining,
      overBy,
      status,
      pct: Math.min(999, Math.round(pct)),
    };
  }, [monthly, subs]);

  const mom = useMemo(() => {
    const pts = monthly?.data ?? [];
    if (pts.length < 2) return null;
    const prev = pts[pts.length - 2]?.total ?? 0;
    const curr = pts[pts.length - 1]?.total ?? 0;
    if (prev <= 0) return curr > 0 ? 100 : 0;
    return Math.round(((curr - prev) / prev) * 100);
  }, [monthly]);

  const sortedSubs: SubscriptionItem[] = useMemo(() => {
    const list = [...(subs?.data ?? [])];
    list.sort(
      (a, b) => (b.monthlyEquivalentShare ?? 0) - (a.monthlyEquivalentShare ?? 0),
    );
    return list;
  }, [subs]);

  const whatIf = useMemo(() => {
    const main =
      monthly?.currency ??
      subs?.summary?.currency ??
      currencyDist?.currency ??
      "TRY";
    return computeWhatIf(
      sortedSubs,
      main,
      fxRates,
      subs?.summary?.monthlyBudget,
      { excludeIds: whatIfExclude, forceYearlyIds: whatIfYearly },
    );
  }, [
    sortedSubs,
    monthly?.currency,
    subs?.summary?.currency,
    subs?.summary?.monthlyBudget,
    currencyDist?.currency,
    fxRates,
    whatIfExclude,
    whatIfYearly,
  ]);

  if (loading && !monthly) return <PageLoader />;

  const summary = subs?.summary;
  const mainCurrency =
    monthly?.currency ?? summary?.currency ?? currencyDist?.currency ?? "TRY";
  const lastMonthTotal =
    monthly?.data?.[monthly.data.length - 1]?.total ??
    summary?.monthlyTotal ??
    0;
  const budget = summary?.monthlyBudget;
  const budgetPct =
    budget && budget > 0
      ? Math.min(100, Math.round((lastMonthTotal / budget) * 100))
      : null;
  const momUp = mom != null && mom > 0;
  const momDown = mom != null && mom < 0;

  const periodChips: { value: PeriodMonths; label: string }[] = [
    { value: 3, label: t("reportsPeriod3") },
    { value: 6, label: t("reportsPeriod6") },
    { value: 12, label: t("reportsPeriod12") },
  ];

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="page-title">{t("reports")}</h1>
          <p className="mt-1 text-sm text-muted">{t("reportsHint")}</p>
        </div>

        {/* Period selector — drives monthly-spend (Priority 1) */}
        <div className="flex flex-col items-end gap-1.5">
          <span className="text-xs font-medium text-muted">
            {t("reportsPeriod")}
          </span>
          <div className="flex rounded-xl border border-border bg-surface p-1">
            {periodChips.map((p) => (
              <button
                key={p.value}
                type="button"
                onClick={() => setQuery({ months: p.value })}
                className={cn(
                  "rounded-lg px-3 py-1.5 text-sm font-medium transition",
                  period === p.value
                    ? "bg-primary text-white shadow-sm"
                    : "text-muted hover:text-foreground",
                )}
              >
                {p.label}
              </button>
            ))}
          </div>
        </div>
      </div>

      <Tabs
        tabs={tabs}
        value={tab}
        onChange={(id) => setQuery({ tab: id })}
        aria-label={t("reports")}
      />

      {!loading ? (
        <FxStatusBanner
          mainCurrency={
            monthly?.currency ??
            subs?.summary?.currency ??
            currencyDist?.currency ??
            "TRY"
          }
          items={[
            ...(subs?.data ?? []).map((s) => ({ currency: s.currency })),
            ...(cashflow?.data ?? []).map((c) => ({ currency: c.currency })),
          ]}
          rates={fxRates}
          isStale={fxStale}
          lastUpdated={fxUpdated}
          apiHasUnconverted={
            !!subs?.summary?.hasUnconvertedAmounts ||
            !!cashflow?.hasUnconvertedAmounts
          }
        />
      ) : null}

      {loading ? (
        <div className="py-8">
          <PageLoader />
        </div>
      ) : null}

      {/* —— Overview (Priority 1 hub) —— */}
      {!loading && tab === "overview" ? (
        <div className="space-y-4">
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard
              label={t("monthlyTotal")}
              icon={Wallet}
              value={formatMoney(lastMonthTotal, mainCurrency, locale)}
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
                      <ArrowUpRight className="h-3.5 w-3.5" />
                    ) : momDown ? (
                      <ArrowDownRight className="h-3.5 w-3.5" />
                    ) : null}
                    {mom > 0 ? "+" : ""}
                    {mom}% {t("reportsMomChange")}
                  </span>
                ) : (
                  <span className="text-muted">{t("reportsNoData")}</span>
                )
              }
            />
            <StatCard
              label={t("reportsAvgMonthly")}
              icon={TrendingUp}
              value={formatMoney(
                monthly?.average ?? 0,
                mainCurrency,
                locale,
              )}
            />
            <StatCard
              label={t("activeSubscriptions")}
              icon={CreditCard}
              value={subs?.pagination?.totalItems ?? sortedSubs.length}
              hint={
                <span className="text-muted">
                  {t("yearlyTotal")}:{" "}
                  {formatMoney(summary?.yearlyTotal ?? 0, mainCurrency, locale)}
                </span>
              }
            />
            <StatCard
              label={t("reportsBudgetUsage")}
              icon={Percent}
              value={budget != null ? `${budgetPct ?? 0}%` : "—"}
              hint={
                budget != null ? (
                  <div className="space-y-1.5">
                    <div className="h-1.5 overflow-hidden rounded-full bg-border">
                      <div
                        className={cn(
                          "h-full rounded-full",
                          summary?.isBudgetExceeded
                            ? "bg-danger"
                            : (budgetPct ?? 0) >= 90
                              ? "bg-warning"
                              : "bg-primary",
                        )}
                        style={{ width: `${budgetPct ?? 0}%` }}
                      />
                    </div>
                    <span className="text-muted">
                      {formatMoney(lastMonthTotal, mainCurrency, locale)} /{" "}
                      {formatMoney(budget, mainCurrency, locale)}
                    </span>
                  </div>
                ) : (
                  <Link
                    href="/profile?tab=profile"
                    className="font-medium text-primary hover:underline"
                  >
                    {t("setBudget")}
                  </Link>
                )
              }
            />
          </div>

          <div className="grid gap-4 lg:grid-cols-5">
            <Card className="lg:col-span-3">
              <CardHeader className="flex-row items-center justify-between space-y-0">
                <CardTitle className="text-base">
                  {t("monthlySpendTrend")}
                </CardTitle>
                <span className="text-xs text-muted">
                  {periodChips.find((p) => p.value === period)?.label}
                </span>
              </CardHeader>
              <CardContent>
                <SpendTrendChart
                  data={monthly?.data ?? []}
                  currency={mainCurrency}
                />
              </CardContent>
            </Card>
            <Card className="lg:col-span-2">
              <CardHeader>
                <CardTitle className="text-base">
                  {t("categoryBreakdown")}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <CategoryDonut data={categories?.data ?? []} />
              </CardContent>
            </Card>
          </div>

          {/* Compact currency + top costs on overview */}
          <div className="grid gap-4 lg:grid-cols-2">
            <CurrencyPanel data={currencyDist} locale={locale} t={t} />
            <TopCostsPreview
              items={sortedSubs.slice(0, 5)}
              mainCurrency={mainCurrency}
              locale={locale}
              t={t}
              fxRates={fxRates}
              onSeeAll={() => setQuery({ tab: "subscriptions" })}
            />
          </div>
        </div>
      ) : null}

      {/* —— Currency tab —— */}
      {!loading && tab === "currency" ? (
        <div className="space-y-4">
          <CurrencyPanel data={currencyDist} locale={locale} t={t} detailed />
        </div>
      ) : null}

      {/* —— Subscriptions cost table —— */}
      {!loading && tab === "subscriptions" ? (
        <Card>
          <CardHeader className="flex-row items-center justify-between space-y-0">
            <CardTitle className="text-base">
              {t("reportsSubsCostTable")}
            </CardTitle>
            <span className="text-xs text-muted">{t("reportsSortByCost")}</span>
          </CardHeader>
          <CardContent className="overflow-x-auto p-0">
            {!sortedSubs.length ? (
              <div className="p-6">
                <EmptyState title={t("reportsNoData")} />
              </div>
            ) : (
              <table className="w-full min-w-[640px] text-left text-sm">
                <thead className="border-b border-border bg-muted/15 text-xs text-muted">
                  <tr>
                    <th className="px-4 py-3 font-medium">{t("subscriptions")}</th>
                    <th className="px-4 py-3 font-medium">{t("category")}</th>
                    <th className="px-4 py-3 font-medium">{t("billingCycle")}</th>
                    <th className="px-4 py-3 font-medium text-right">
                      {t("price")}
                    </th>
                    <th className="px-4 py-3 font-medium text-right">
                      {t("reportsMonthlyShare")}
                    </th>
                    <th className="px-4 py-3 font-medium text-right">
                      {t("reportsYearlyShare")}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {sortedSubs.map((item) => {
                    const cycle = normalizeBillingCycle(item.billingCycle);
                    const monthly = item.monthlyEquivalentShare ?? item.userShare;
                    const yearly =
                      cycle === "yearly"
                        ? item.userShare
                        : monthly * 12;
                    return (
                      <tr
                        key={item.id}
                        className="border-b border-border last:border-0 hover:bg-primary-soft/20"
                      >
                        <td className="px-4 py-3">
                          <Link
                            href={`/subscriptions/${item.id}`}
                            className="font-medium text-foreground hover:text-primary"
                          >
                            {item.name}
                          </Link>
                          {item.sharedWithCount > 1 ? (
                            <span className="ml-2 text-xs text-muted">
                              ÷{item.sharedWithCount}
                            </span>
                          ) : null}
                        </td>
                        <td className="px-4 py-3 text-muted">
                          {item.category?.name || "—"}
                        </td>
                        <td className="px-4 py-3">
                          <Badge variant="muted">
                            {cycle === "yearly" ? t("yearly") : t("monthly")}
                          </Badge>
                        </td>
                        <td className="px-4 py-3 text-right">
                          <MoneyDual
                            amount={item.price}
                            currency={item.currency}
                            mainCurrency={mainCurrency}
                            rates={fxRates}
                            size="sm"
                            stacked
                            className="items-end"
                          />
                        </td>
                        <td className="px-4 py-3 text-right">
                          <MoneyDual
                            amount={monthly}
                            currency={item.currency}
                            mainCurrency={mainCurrency}
                            rates={fxRates}
                            size="sm"
                            stacked
                            className="items-end"
                          />
                        </td>
                        <td className="px-4 py-3 text-right">
                          <MoneyDual
                            amount={yearly}
                            currency={item.currency}
                            mainCurrency={mainCurrency}
                            rates={fxRates}
                            size="sm"
                            stacked
                            className="items-end text-muted"
                          />
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            )}
          </CardContent>
        </Card>
      ) : null}

      {/* —— Cashflow (Priority 2) —— */}
      {tab === "cashflow" ? (
        <div className="space-y-4">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <p className="text-sm text-muted">{t("cashflowHorizon")}</p>
            <div className="flex flex-wrap items-center gap-2">
              <button
                type="button"
                disabled={!cashflow?.data?.length}
                onClick={() => {
                  if (!cashflow?.data?.length) {
                    toast.error(t("exportEmpty"));
                    return;
                  }
                  downloadUpcomingIcs(cashflow.data, t("cashflowTimeline"));
                  toast.success(t("icsDownloaded"));
                }}
                className="inline-flex h-9 items-center gap-1.5 rounded-full border border-border bg-surface px-3 text-xs font-medium hover:border-primary/40 disabled:opacity-50"
              >
                <CalendarDays className="h-3.5 w-3.5" />
                {t("icsExport")}
              </button>
              <div className="flex rounded-xl border border-border bg-surface p-1">
                {(
                  [
                    { d: 30 as CashflowDays, label: t("cashflowDays30") },
                    { d: 60 as CashflowDays, label: t("cashflowDays60") },
                    { d: 90 as CashflowDays, label: t("cashflowDays90") },
                  ] as const
                ).map((h) => (
                  <button
                    key={h.d}
                    type="button"
                    onClick={() => setQuery({ days: h.d })}
                    className={cn(
                      "rounded-lg px-3 py-1.5 text-sm font-medium transition",
                      cashflowDays === h.d
                        ? "bg-primary text-white shadow-sm"
                        : "text-muted hover:text-foreground",
                    )}
                  >
                    {h.label}
                  </button>
                ))}
              </div>
            </div>
          </div>

          {cashflowLoading || !cashflow ? (
            <PageLoader />
          ) : (
            <>
              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                <StatCard
                  label={t("cashflowTotal")}
                  icon={Wallet}
                  value={formatMoney(
                    cashflow.total,
                    cashflow.currency,
                    locale,
                  )}
                  hint={
                    <span className="text-muted">
                      {cashflowDays} · {t("cashflowHorizonHint")}
                    </span>
                  }
                />
                <StatCard
                  label={t("cashflowOverdue")}
                  icon={AlertTriangle}
                  value={cashflow.overdueCount}
                  hint={
                    cashflow.overdueCount > 0 ? (
                      <span className="font-medium text-danger">
                        {t("overdue")}
                      </span>
                    ) : (
                      <span className="text-muted">—</span>
                    )
                  }
                />
                <StatCard
                  label={t("cashflowUpcoming")}
                  icon={CalendarDays}
                  value={cashflow.upcomingCount}
                />
                <StatCard
                  label={t("cashflowPeakWeek")}
                  icon={TrendingUp}
                  value={
                    peakWeek && peakWeek.count > 0
                      ? `${peakWeek.count}`
                      : "—"
                  }
                  hint={
                    peakWeek && peakWeek.count > 0 ? (
                      <span className="text-muted">
                        {t("cashflowWeekOf")}{" "}
                        {formatDate(peakWeek.start.toISOString(), locale)}
                      </span>
                    ) : null
                  }
                />
              </div>

              {cashflow.warnings?.length ? (
                <div className="rounded-2xl border border-warning/40 bg-warning/10 px-4 py-3 text-sm text-warning">
                  <p className="font-medium">{t("cashflowWarnings")}</p>
                  <ul className="mt-1 list-inside list-disc text-xs opacity-90">
                    {cashflow.warnings.map((w) => (
                      <li key={w}>{w}</li>
                    ))}
                  </ul>
                </div>
              ) : null}

              <Card>
                <CardHeader>
                  <CardTitle className="text-base">
                    {t("cashflowWeekly")}
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  {!weeklyDensity.some((w) => w.count > 0) ? (
                    <p className="text-sm text-muted">{t("cashflowEmpty")}</p>
                  ) : (
                    <div className="flex items-end gap-2 sm:gap-3">
                      {(() => {
                        const maxC = Math.max(
                          ...weeklyDensity.map((w) => w.count),
                          1,
                        );
                        return weeklyDensity.map((w) => (
                          <div
                            key={w.index}
                            className="flex min-w-0 flex-1 flex-col items-center gap-1.5"
                          >
                            <span className="text-xs font-medium tabular-nums text-muted">
                              {w.count || ""}
                            </span>
                            <div className="flex h-28 w-full items-end justify-center rounded-lg bg-border/40 px-1 pb-1">
                              <div
                                className={cn(
                                  "w-full max-w-[40px] rounded-md transition-all",
                                  w.count === maxC && w.count > 0
                                    ? "bg-primary"
                                    : "bg-primary/50",
                                )}
                                style={{
                                  height: `${Math.max(
                                    w.count ? 12 : 0,
                                    (w.count / maxC) * 100,
                                  )}%`,
                                }}
                              />
                            </div>
                            <span className="truncate text-[10px] text-muted">
                              W{w.index + 1}
                            </span>
                          </div>
                        ));
                      })()}
                    </div>
                  )}
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle className="text-base">
                    {t("cashflowTimeline")}
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-2">
                  {!cashflow.data?.length ? (
                    <EmptyState title={t("cashflowEmpty")} />
                  ) : (
                    cashflow.data.map((item) => (
                      <Link
                        key={item.id}
                        href={`/subscriptions/${item.id}`}
                        className={cn(
                          "flex items-center justify-between gap-3 rounded-xl border border-border px-3 py-2.5 transition hover:border-primary/30 hover:bg-primary-soft/30",
                          item.isOverdue && "card-overdue",
                          item.isUpcoming &&
                            !item.isOverdue &&
                            item.daysUntilRenewal <= 3 &&
                            "card-soon",
                        )}
                      >
                        <div className="min-w-0">
                          <p className="truncate font-medium">{item.name}</p>
                          <p className="text-xs text-muted">
                            {formatDate(item.nextRenewalDate, locale)}
                            {" · "}
                            {item.isOverdue
                              ? t("cashflowDaysOverdue").replace(
                                  "{n}",
                                  String(Math.abs(item.daysUntilRenewal)),
                                )
                              : item.daysUntilRenewal === 0
                                ? t("cashflowToday")
                                : t("cashflowDaysUntil").replace(
                                    "{n}",
                                    String(item.daysUntilRenewal),
                                  )}
                          </p>
                        </div>
                        <div className="flex shrink-0 flex-col items-end gap-1">
                          {item.isOverdue ? (
                            <Badge variant="danger">{t("overdue")}</Badge>
                          ) : item.isUpcoming && item.daysUntilRenewal <= 3 ? (
                            <Badge variant="warning">{t("soon")}</Badge>
                          ) : item.isUpcoming ? (
                            <Badge variant="muted">{t("upcoming")}</Badge>
                          ) : null}
                          <MoneyDual
                            amount={item.userShare ?? item.price}
                            currency={item.currency}
                            mainCurrency={mainCurrency}
                            rates={fxRates}
                            size="sm"
                            stacked
                            className="items-end"
                          />
                        </div>
                      </Link>
                    ))
                  )}
                </CardContent>
              </Card>              
            </>
          )}
        </div>
      ) : null}

      {/* —— Budget health (Priority 3) —— */}
      {!loading && tab === "budget" ? (
        <div className="space-y-4">
          {!budgetHealth.hasBudget ? (
            <Card>
              <CardContent className="flex flex-col items-center gap-3 py-14 text-center">
                <p className="max-w-md text-sm text-muted">
                  {t("budgetNoBudget")}
                </p>
                <Link href="/profile?tab=profile">
                  <span className="inline-flex h-10 items-center rounded-full bg-primary px-5 text-sm font-semibold text-white">
                    {t("budgetSetCta")}
                  </span>
                </Link>
              </CardContent>
            </Card>
          ) : (
            <>
              <div
                className={cn(
                  "rounded-2xl border px-4 py-3 text-sm font-medium",
                  budgetHealth.status === "over" &&
                    "border-danger/40 bg-danger/10 text-danger",
                  budgetHealth.status === "near" &&
                    "border-warning/40 bg-warning/10 text-warning",
                  budgetHealth.status === "ok" &&
                    "border-success/40 bg-success/10 text-success",
                )}
              >
                {budgetHealth.status === "over"
                  ? t("budgetStatusOver")
                  : budgetHealth.status === "near"
                    ? t("budgetStatusNear")
                    : t("budgetStatusOk")}
                {budgetHealth.status === "over" ? (
                  <span className="ml-2 font-normal opacity-90">
                    · {t("budgetOverBy")}{" "}
                    {formatMoney(
                      budgetHealth.overBy,
                      mainCurrency,
                      locale,
                    )}
                  </span>
                ) : null}
              </div>

              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                <StatCard
                  label={t("budgetTarget")}
                  icon={Wallet}
                  value={formatMoney(
                    budgetHealth.budget!,
                    mainCurrency,
                    locale,
                  )}
                />
                <StatCard
                  label={t("budgetSpent")}
                  icon={TrendingUp}
                  value={formatMoney(
                    budgetHealth.latest,
                    mainCurrency,
                    locale,
                  )}
                  hint={
                    <span className="text-muted">
                      {budgetHealth.pct}% {t("reportsBudgetUsage").toLowerCase()}
                    </span>
                  }
                />
                <StatCard
                  label={
                    budgetHealth.status === "over"
                      ? t("budgetOver")
                      : t("budgetRemaining")
                  }
                  icon={Percent}
                  value={formatMoney(
                    budgetHealth.status === "over"
                      ? budgetHealth.overBy
                      : budgetHealth.remaining,
                    mainCurrency,
                    locale,
                  )}
                />
                <StatCard
                  label={t("budgetMonthsOver")}
                  icon={AlertTriangle}
                  value={`${budgetHealth.monthsOver} / ${budgetHealth.points.length || "—"}`}
                  hint={
                    <span className="text-muted">
                      {t("budgetMonthsOk")}: {budgetHealth.monthsOk}
                    </span>
                  }
                />
              </div>

              <Card>
                <CardHeader>
                  <CardTitle className="text-base">
                    {t("budgetHistory")}
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-3">
                  {!budgetHealth.points.length ? (
                    <EmptyState title={t("reportsNoData")} />
                  ) : (
                    budgetHealth.points.map((p) => {
                      const widthPct = Math.min(
                        100,
                        Math.round(p.ratio * 100),
                      );
                      return (
                        <div key={p.month} className="space-y-1">
                          <div className="flex items-center justify-between text-sm">
                            <span className="text-muted">{p.month}</span>
                            <span className="flex items-center gap-2 tabular-nums">
                              {formatMoney(p.total, mainCurrency, locale)}
                              {p.over ? (
                                <Badge variant="danger">{t("budgetOver")}</Badge>
                              ) : p.ratio >= 0.9 ? (
                                <Badge variant="warning">
                                  {t("budgetStatusNear")}
                                </Badge>
                              ) : (
                                <Badge variant="success">
                                  {t("budgetStatusOk")}
                                </Badge>
                              )}
                            </span>
                          </div>
                          <div className="relative h-2.5 overflow-hidden rounded-full bg-border">
                            {/* budget line at 100% of bar = budget amount; bar fill is spend/budget capped visual */}
                            <div
                              className={cn(
                                "absolute inset-y-0 left-0 rounded-full",
                                p.over
                                  ? "bg-danger"
                                  : p.ratio >= 0.9
                                    ? "bg-warning"
                                    : "bg-primary",
                              )}
                              style={{
                                width: `${Math.max(p.total > 0 ? 4 : 0, Math.min(widthPct, 100))}%`,
                              }}
                            />
                          </div>
                        </div>
                      );
                    })
                  )}
                  <p className="pt-1 text-xs text-muted">
                    {t("budgetHeadroom")}:{" "}
                    <span
                      className={cn(
                        "font-medium tabular-nums",
                        budgetHealth.avgHeadroom < 0
                          ? "text-danger"
                          : "text-foreground",
                      )}
                    >
                      {formatMoney(
                        budgetHealth.avgHeadroom,
                        mainCurrency,
                        locale,
                      )}
                    </span>
                  </p>
                </CardContent>
              </Card>

              <Card>
                <CardHeader className="flex-row items-center justify-between space-y-0">
                  <CardTitle className="text-base">{t("whatIfTitle")}</CardTitle>
                  {(whatIfExclude.size > 0 || whatIfYearly.size > 0) && (
                    <button
                      type="button"
                      className="text-xs font-medium text-primary hover:underline"
                      onClick={() => {
                        setWhatIfExclude(new Set());
                        setWhatIfYearly(new Set());
                      }}
                    >
                      {t("whatIfReset")}
                    </button>
                  )}
                </CardHeader>
                <CardContent className="space-y-3">
                  <p className="text-xs text-muted">{t("whatIfHint")}</p>
                  <div className="grid gap-2 sm:grid-cols-3">
                    <div className="rounded-xl border border-border px-3 py-2">
                      <p className="text-[11px] text-muted">
                        {t("whatIfBaseline")}
                      </p>
                      <p className="text-sm font-semibold tabular-nums">
                        {formatMoney(
                          whatIf.baselineMonthly,
                          mainCurrency,
                          locale,
                        )}
                      </p>
                    </div>
                    <div className="rounded-xl border border-border px-3 py-2">
                      <p className="text-[11px] text-muted">
                        {t("whatIfScenario")}
                      </p>
                      <p className="text-sm font-semibold tabular-nums">
                        {formatMoney(
                          whatIf.scenarioMonthly,
                          mainCurrency,
                          locale,
                        )}
                      </p>
                    </div>
                    <div className="rounded-xl border border-primary/30 bg-primary-soft/30 px-3 py-2">
                      <p className="text-[11px] text-muted">
                        {t("whatIfSaved")}
                      </p>
                      <p
                        className={cn(
                          "text-sm font-semibold tabular-nums",
                          whatIf.savedMonthly > 0
                            ? "text-success"
                            : "text-foreground",
                        )}
                      >
                        {formatMoney(
                          whatIf.savedMonthly,
                          mainCurrency,
                          locale,
                        )}
                      </p>
                      {whatIf.scenarioRemaining != null ? (
                        <p className="mt-0.5 text-[10px] text-muted">
                          {t("budgetRemaining")}:{" "}
                          {formatMoney(
                            whatIf.scenarioRemaining,
                            mainCurrency,
                            locale,
                          )}
                        </p>
                      ) : null}
                    </div>
                  </div>
                  <div className="max-h-64 space-y-1.5 overflow-y-auto">
                    {sortedSubs.slice(0, 12).map((item) => (
                      <div
                        key={item.id}
                        className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-border px-3 py-2 text-sm"
                      >
                        <span className="min-w-0 truncate font-medium">
                          {item.name}
                        </span>
                        <div className="flex items-center gap-3 text-xs">
                          <label className="flex items-center gap-1.5 text-muted">
                            <input
                              type="checkbox"
                              checked={whatIfExclude.has(item.id)}
                              onChange={(e) => {
                                setWhatIfExclude((prev) => {
                                  const n = new Set(prev);
                                  if (e.target.checked) n.add(item.id);
                                  else n.delete(item.id);
                                  return n;
                                });
                              }}
                            />
                            {t("whatIfExclude")}
                          </label>
                          <label className="flex items-center gap-1.5 text-muted">
                            <input
                              type="checkbox"
                              checked={whatIfYearly.has(item.id)}
                              disabled={whatIfExclude.has(item.id)}
                              onChange={(e) => {
                                setWhatIfYearly((prev) => {
                                  const n = new Set(prev);
                                  if (e.target.checked) n.add(item.id);
                                  else n.delete(item.id);
                                  return n;
                                });
                              }}
                            />
                            {t("whatIfYearly")}
                          </label>
                        </div>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle className="text-base">
                    {t("budgetCutHints")}
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-2">
                  <p className="mb-2 text-xs text-muted">
                    {t("budgetCutHintBody")}
                  </p>
                  {!sortedSubs.length ? (
                    <EmptyState title={t("reportsNoData")} />
                  ) : (
                    sortedSubs.slice(0, 5).map((item, i) => (
                      <Link
                        key={item.id}
                        href={`/subscriptions/${item.id}`}
                        className="flex items-center justify-between rounded-xl border border-border px-3 py-2.5 transition hover:border-primary/30 hover:bg-primary-soft/30"
                      >
                        <div className="flex min-w-0 items-center gap-2.5">
                          <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-lg bg-primary-soft text-xs font-bold text-primary">
                            {i + 1}
                          </span>
                          <span className="truncate text-sm font-medium">
                            {item.name}
                          </span>
                        </div>
                        <span className="flex shrink-0 flex-col items-end">
                          <MoneyDual
                            amount={
                              item.monthlyEquivalentShare ?? item.userShare
                            }
                            currency={item.currency}
                            mainCurrency={mainCurrency}
                            rates={fxRates}
                            size="sm"
                            stacked
                            className="items-end"
                          />
                          <span className="text-[10px] text-muted">/mo</span>
                        </span>
                      </Link>
                    ))
                  )}
                </CardContent>
              </Card>
            </>
          )}
        </div>
      ) : null}

      {/* —— AI period commentary —— */}
      {!loading && tab === "ai" ? (
        <div className="space-y-4">
          <p className="text-sm text-muted">{t("reportsAiHint")}</p>

          <div className="flex flex-wrap items-center gap-3">
            <Button onClick={generateAiCommentary} disabled={aiBusy}>
              <Sparkles className="mr-2 h-4 w-4" />
              {aiBusy ? t("reportsAiGenerating") : t("reportsAiGenerate")}
            </Button>
            <span className="text-xs text-muted">
              {t("reportsPeriod")}: {period}m
            </span>
          </div>

          {aiKeyMissing ? (
            <div className="rounded-lg border border-warning/40 bg-warning/10 px-4 py-3 text-sm">
              {t("reportsAiKeyMissing")}
            </div>
          ) : null}

          {aiCommentary ? (
            <Card>
              <CardHeader className="space-y-2">
                <div className="flex flex-wrap items-center gap-2">
                  <CardTitle className="text-lg leading-snug">
                    {aiCommentary.summary}
                  </CardTitle>
                  <Badge
                    className={cn(
                      "shrink-0 border",
                      aiCommentary.trend === "up" &&
                        "border-danger/30 bg-danger/10 text-danger",
                      aiCommentary.trend === "down" &&
                        "border-success/30 bg-success/10 text-success",
                      aiCommentary.trend !== "up" &&
                        aiCommentary.trend !== "down" &&
                        "border-border bg-surface text-foreground",
                    )}
                  >
                    {aiCommentary.trend === "up" ? (
                      <ArrowUpRight className="mr-1 h-3.5 w-3.5" />
                    ) : aiCommentary.trend === "down" ? (
                      <ArrowDownRight className="mr-1 h-3.5 w-3.5" />
                    ) : (
                      <Minus className="mr-1 h-3.5 w-3.5" />
                    )}
                    {t("reportsAiTrend")}: {trendLabel(aiCommentary.trend)}
                  </Badge>
                </div>
                <p className="text-xs text-muted">
                  {formatDate(aiCommentary.generatedAt, locale)} ·{" "}
                  {aiCommentary.months}m · {aiCommentary.currency}
                </p>
              </CardHeader>
              <CardContent className="space-y-4">
                {aiCommentary.highlights?.length ? (
                  <div>
                    <p className="mb-2 text-sm font-medium">
                      {t("reportsAiHighlights")}
                    </p>
                    <ul className="space-y-2">
                      {aiCommentary.highlights.map((h, i) => (
                        <li
                          key={i}
                          className="rounded-lg border border-border px-3 py-2 text-sm"
                        >
                          {h}
                        </li>
                      ))}
                    </ul>
                  </div>
                ) : null}
                {aiCommentary.budgetNote ? (
                  <div className="rounded-lg border border-primary/20 bg-primary-soft/40 px-3 py-2.5 text-sm">
                    <p className="mb-0.5 text-xs font-medium text-primary">
                      {t("reportsAiBudgetNote")}
                    </p>
                    {aiCommentary.budgetNote}
                  </div>
                ) : null}
              </CardContent>
            </Card>
          ) : !aiBusy && !aiKeyMissing ? (
            <EmptyState title={t("reportsAiEmpty")} />
          ) : null}
        </div>
      ) : null}

      {/* —— Export (Priority 4) —— */}
      {!loading && tab === "export" ? (
        <div className="space-y-4">
          <p className="text-sm text-muted">{t("exportHint")}</p>

          <div className="grid gap-3 sm:grid-cols-2">
            <ExportActionCard
              icon={FileSpreadsheet}
              title={t("exportSubsCsv")}
              description={t("exportSubsCsvDesc")}
              onClick={() => {
                if (!sortedSubs.length) {
                  toast.error(t("exportEmpty"));
                  return;
                }
                const csv = toCsv(
                  [
                    "name",
                    "category",
                    "billingCycle",
                    "priceOriginal",
                    "priceCurrency",
                    "priceMain",
                    "userShareOriginal",
                    "userShareMain",
                    "monthlyEquivalentOriginal",
                    "monthlyEquivalentMain",
                    "yearlyShareOriginal",
                    "yearlyShareMain",
                    "mainCurrency",
                    "sharedWithCount",
                    "nextRenewalDate",
                    "archived",
                  ],
                  sortedSubs.map((item) => {
                    const cycle = normalizeBillingCycle(item.billingCycle);
                    const monthly =
                      item.monthlyEquivalentShare ?? item.userShare;
                    const yearly =
                      cycle === "yearly" ? item.userShare : monthly * 12;
                    const priceMain = convertCurrency(
                      item.price,
                      item.currency,
                      mainCurrency,
                      fxRates,
                    );
                    const shareMain = convertCurrency(
                      item.userShare,
                      item.currency,
                      mainCurrency,
                      fxRates,
                    );
                    const monthlyMain = convertCurrency(
                      monthly,
                      item.currency,
                      mainCurrency,
                      fxRates,
                    );
                    const yearlyMain = convertCurrency(
                      yearly,
                      item.currency,
                      mainCurrency,
                      fxRates,
                    );
                    return [
                      item.name,
                      item.category?.name ?? "",
                      cycle,
                      item.price,
                      item.currency,
                      priceMain.converted ? priceMain.amount : "",
                      item.userShare,
                      shareMain.converted ? shareMain.amount : "",
                      monthly,
                      monthlyMain.converted ? monthlyMain.amount : "",
                      yearly,
                      yearlyMain.converted ? yearlyMain.amount : "",
                      mainCurrency,
                      item.sharedWithCount,
                      item.nextRenewalDate,
                      item.archived,
                    ];
                  }),
                );
                downloadTextFile(stampFilename("subify-subscriptions"), csv);
                toast.success(t("exportDownloaded"));
              }}
            />

            <ExportActionCard
              icon={Download}
              title={t("exportMonthlyCsv")}
              description={t("exportMonthlyCsvDesc")}
              onClick={() => {
                const rows = monthly?.data ?? [];
                if (!rows.length) {
                  toast.error(t("exportEmpty"));
                  return;
                }
                const csv = toCsv(
                  ["month", "total", "currency"],
                  rows.map((r) => [
                    r.month,
                    r.total,
                    monthly?.currency ?? mainCurrency,
                  ]),
                );
                downloadTextFile(
                  stampFilename(`subify-monthly-${period}m`),
                  csv,
                );
                toast.success(t("exportDownloaded"));
              }}
            />

            <ExportActionCard
              icon={FileSpreadsheet}
              title={t("exportCurrencyCsv")}
              description={t("exportCurrencyCsvDesc")}
              onClick={() => {
                const rows = currencyDist?.data ?? [];
                if (!rows.length) {
                  toast.error(t("exportEmpty"));
                  return;
                }
                const csv = toCsv(
                  [
                    "currency",
                    "monthlyTotal",
                    "convertedMonthlyTotal",
                    "percentage",
                    "count",
                    "mainCurrency",
                  ],
                  rows.map((r) => [
                    r.currency,
                    r.monthlyTotal,
                    r.convertedMonthlyTotal,
                    r.percentage,
                    r.count,
                    currencyDist?.currency ?? mainCurrency,
                  ]),
                );
                downloadTextFile(stampFilename("subify-currency"), csv);
                toast.success(t("exportDownloaded"));
              }}
            />

            <ExportActionCard
              icon={Mail}
              title={emailBusy ? t("exportEmailSending") : t("exportEmail")}
              description={t("exportEmailDesc")}
              onClick={() => {
                if (emailBusy) return;
                if (!sortedSubs.length && !(monthly?.data?.length)) {
                  toast.error(t("exportEmpty"));
                  return;
                }
                void sendEmailSummary();
              }}
            />

            <ExportActionCard
              icon={Printer}
              title={t("exportPrint")}
              description={t("exportPrintDesc")}
              onClick={() => {
                const w = window.open("", "_blank", "noopener,noreferrer");
                if (!w) {
                  toast.error(t("errorGeneric"));
                  return;
                }
                const last =
                  monthly?.data?.[monthly.data.length - 1]?.total ??
                  summary?.monthlyTotal ??
                  0;
                const rowsHtml = sortedSubs
                  .slice(0, 40)
                  .map((item) => {
                    const monthly =
                      item.monthlyEquivalentShare ?? item.userShare;
                    const dual = formatMoneyDual(
                      monthly,
                      item.currency,
                      mainCurrency,
                      { locale, rates: fxRates },
                    );
                    return `<tr><td>${escapeHtml(item.name)}</td><td>${escapeHtml(item.category?.name ?? "")}</td><td style="text-align:right">${escapeHtml(dual.displayText)}</td></tr>`;
                  })
                  .join("");
                w.document.write(`<!DOCTYPE html><html><head><title>Subify</title>
<style>
  body{font-family:system-ui,sans-serif;padding:24px;color:#111}
  h1{font-size:20px;margin:0 0 8px}
  p{color:#555;font-size:13px}
  table{width:100%;border-collapse:collapse;margin-top:16px;font-size:12px}
  th,td{border-bottom:1px solid #ddd;padding:8px;text-align:left}
  th{color:#666;font-weight:600}
</style></head><body>
<h1>Subify — ${escapeHtml(t("reports"))}</h1>
<p>${new Date().toLocaleString(locale === "tr" ? "tr-TR" : "en-US")}</p>
<p><strong>${escapeHtml(t("monthlyTotal"))}:</strong> ${escapeHtml(formatMoney(last, mainCurrency, locale))}
 · <strong>${escapeHtml(t("activeSubscriptions"))}:</strong> ${sortedSubs.length}
 · <strong>${escapeHtml(t("reportsPeriod"))}:</strong> ${period}m</p>
<table><thead><tr>
<th>${escapeHtml(t("subscriptions"))}</th>
<th>${escapeHtml(t("category"))}</th>
<th style="text-align:right">${escapeHtml(t("reportsMonthlyShare"))} (${escapeHtml(mainCurrency)})</th>
</tr></thead><tbody>${rowsHtml}</tbody></table>
<script>window.onload=function(){window.print();}</script>
</body></html>`);
                w.document.close();
              }}
            />
          </div>
        </div>
      ) : null}
    </div>
  );
}

function escapeHtml(s: string): string {
  return s
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function ExportActionCard({
  icon: Icon,
  title,
  description,
  onClick,
}: {
  icon: typeof Download;
  title: string;
  description: string;
  onClick: () => void;
}) {
  return (
    <Card className="transition hover:border-primary/30">
      <CardContent className="flex flex-col gap-3 p-5">
        <div className="inline-flex h-10 w-10 items-center justify-center rounded-xl bg-primary-soft text-primary">
          <Icon className="h-5 w-5" />
        </div>
        <div>
          <p className="font-semibold">{title}</p>
          <p className="mt-1 text-sm text-muted">{description}</p>
        </div>
        <button
          type="button"
          onClick={onClick}
          className="mt-auto inline-flex h-10 w-full items-center justify-center rounded-full bg-primary text-sm font-semibold text-white hover:bg-primary-hover"
        >
          {title}
        </button>
      </CardContent>
    </Card>
  );
}

function CurrencyPanel({
  data,
  locale,
  t,
  detailed = false,
}: {
  data: CurrencyDistributionResponse | null;
  locale: string;
  t: (k: MessageKey) => string;
  detailed?: boolean;
}) {
  const rows = data?.data ?? [];
  const main = data?.currency ?? "TRY";

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">{t("reportsCurrencyDist")}</CardTitle>
      </CardHeader>
      <CardContent>
        {!rows.length ? (
          <EmptyState title={data?.message || t("reportsNoData")} />
        ) : (
          <div className="space-y-3">
            {rows.map((row) => (
              <div key={row.currency} className="space-y-1.5">
                <div className="flex items-center justify-between gap-2 text-sm">
                  <div className="flex items-center gap-2">
                    <Badge variant="muted">{row.currency}</Badge>
                    <span className="text-xs text-muted">×{row.count}</span>
                  </div>
                  <div className="text-right">
                    {/* Main first, original in parentheses when different */}
                    <p className="font-medium tabular-nums">
                      {formatMoney(row.convertedMonthlyTotal, main, locale)}
                      <span className="ml-1 text-xs font-normal text-muted">
                        ({row.percentage}%)
                      </span>
                    </p>
                    {detailed || row.currency !== main ? (
                      <p className="text-xs tabular-nums text-muted">
                        (
                        {formatMoney(row.monthlyTotal, row.currency, locale)}
                        )
                        {detailed ? (
                          <span className="ml-1">
                            · {t("reportsCurrencyOriginal")}
                          </span>
                        ) : null}
                      </p>
                    ) : null}
                  </div>
                </div>
                <div className="h-2 overflow-hidden rounded-full bg-border">
                  <div
                    className="h-full rounded-full bg-primary"
                    style={{
                      width: `${Math.max(4, Math.min(100, row.percentage))}%`,
                    }}
                  />
                </div>
              </div>
            ))}
            {detailed ? (
              <p className="pt-2 text-xs text-muted">
                {t("reportsCurrencyConverted")}: {main} · Σ{" "}
                {formatMoney(data?.grandTotal ?? 0, main, locale)}
              </p>
            ) : null}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function TopCostsPreview({
  items,
  mainCurrency,
  t,
  fxRates,
  onSeeAll,
}: {
  items: SubscriptionItem[];
  mainCurrency: string;
  locale: string;
  t: (k: MessageKey) => string;
  fxRates: FxRatesSnapshot | null;
  onSeeAll: () => void;
}) {
  return (
    <Card>
      <CardHeader className="flex-row items-center justify-between space-y-0">
        <CardTitle className="text-base">{t("reportsSubsCostTable")}</CardTitle>
        <button
          type="button"
          onClick={onSeeAll}
          className="text-xs font-medium text-primary hover:underline"
        >
          {t("viewAll")}
        </button>
      </CardHeader>
      <CardContent className="space-y-2">
        {!items.length ? (
          <EmptyState title={t("reportsNoData")} />
        ) : (
          items.map((item, i) => (
            <Link
              key={item.id}
              href={`/subscriptions/${item.id}`}
              className="flex items-center justify-between rounded-xl border border-border px-3 py-2.5 transition hover:border-primary/30 hover:bg-primary-soft/30"
            >
              <div className="flex min-w-0 items-center gap-2.5">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-lg bg-primary-soft text-xs font-bold text-primary">
                  {i + 1}
                </span>
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium">{item.name}</p>
                  <p className="text-xs text-muted">
                    {item.category?.name || "—"}
                  </p>
                </div>
              </div>
              <span className="flex shrink-0 flex-col items-end">
                <MoneyDual
                  amount={item.monthlyEquivalentShare ?? item.userShare}
                  currency={item.currency}
                  mainCurrency={mainCurrency}
                  rates={fxRates}
                  size="sm"
                  stacked
                  className="items-end"
                />
                <span className="text-[10px] text-muted">/mo</span>
              </span>
            </Link>
          ))
        )}
      </CardContent>
    </Card>
  );
}

export default function ReportsPage() {
  return (
    <Suspense fallback={<PageLoader />}>
      <ReportsPageInner />
    </Suspense>
  );
}
