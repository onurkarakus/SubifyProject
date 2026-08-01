"use client";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { Input } from "@/components/ui/input";
import { PageLoader } from "@/components/ui/spinner";
import { api, ApiError } from "@/lib/api/client";
import type { ListSubscriptionsResponse, SubscriptionItem } from "@/lib/api/types";
import { useI18n } from "@/lib/i18n/context";
import { cn, formatDate, formatMoney, normalizeBillingCycle } from "@/lib/utils";
import { Archive, Pencil, Plus } from "lucide-react";
import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";

function cardState(item: SubscriptionItem) {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const next = new Date(item.nextRenewalDate);
  const days = Math.round(
    (next.getTime() - today.getTime()) / (1000 * 60 * 60 * 24),
  );
  if (days < 0) return "overdue" as const;
  if (days <= 3) return "soon" as const;
  return "normal" as const;
}

export default function SubscriptionsPage() {
  const { t, locale } = useI18n();
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [includeArchived, setIncludeArchived] = useState(false);
  const [data, setData] = useState<ListSubscriptionsResponse | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const q = new URLSearchParams({
        page: "1",
        pageSize: "50",
        includeArchived: String(includeArchived),
      });
      if (search.trim()) q.set("search", search.trim());
      const res = await api.get<ListSubscriptionsResponse>(
        `/subscriptions?${q.toString()}`,
      );
      setData(res);
    } catch (e) {
      toast.error(e instanceof ApiError ? e.message : t("errorGeneric"));
    } finally {
      setLoading(false);
    }
  }, [includeArchived, search, t]);

  useEffect(() => {
    void load();
  }, [load]);

  async function archive(id: string) {
    if (!confirm(`${t("archive")}?`)) return;
    try {
      await api.delete(`/subscriptions/${id}`);
      toast.success(t("archive"));
      await load();
    } catch (e) {
      toast.error(e instanceof ApiError ? e.message : t("errorGeneric"));
    }
  }

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="page-title">{t("subscriptions")}</h1>
        <Link
          href="/subscriptions/new"
          className="inline-flex h-10 items-center gap-2 rounded-full bg-primary px-5 text-sm font-medium text-white shadow-[var(--shadow-glow)] hover:bg-primary-hover"
        >
          <Plus className="h-4 w-4" />
          {t("addSubscription")}
        </Link>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <Input
          placeholder={t("search")}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="max-w-xs"
        />
        <label className="flex items-center gap-2 text-sm text-muted">
          <input
            type="checkbox"
            checked={includeArchived}
            onChange={(e) => setIncludeArchived(e.target.checked)}
          />
          {t("includeArchived")}
        </label>
      </div>

      {data?.summary ? (
        <p className="text-sm text-muted">
          {t("monthlyTotal")}:{" "}
          <strong className="text-foreground">
            {formatMoney(
              data.summary.monthlyTotal,
              data.summary.currency,
              locale,
            )}
          </strong>
        </p>
      ) : null}

      {loading ? (
        <PageLoader />
      ) : !data?.data?.length ? (
        <EmptyState
          title={t("empty")}
          action={
            <Link
              href="/subscriptions/new"
              className="text-sm font-medium text-primary"
            >
              {t("addSubscription")}
            </Link>
          }
        />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {data.data.map((item) => {
            const state = cardState(item);
            return (
              <Card
                key={item.id}
                className={cn(
                  "transition hover:border-primary/25",
                  state === "soon" && "card-soon",
                  state === "overdue" && "card-overdue",
                  item.archived && "opacity-70",
                )}
              >
                <CardContent className="space-y-3 p-5">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <h3 className="font-semibold">{item.name}</h3>
                      <p className="text-xs text-muted">
                        {item.category?.name || item.provider?.name || "—"}
                      </p>
                    </div>
                    {state === "overdue" ? (
                      <Badge variant="danger">{t("overdue")}</Badge>
                    ) : state === "soon" ? (
                      <Badge variant="warning">{t("soon")}</Badge>
                    ) : (
                      <Badge variant="muted">{t("normal")}</Badge>
                    )}
                  </div>
                  <div>
                    <p className="text-2xl font-bold">
                      {formatMoney(item.price, item.currency, locale)}
                    </p>
                    <p className="text-xs text-muted">
                      {t("yourShare")}:{" "}
                      {formatMoney(item.userShare, item.currency, locale)} ·{" "}
                      {normalizeBillingCycle(item.billingCycle) === "yearly"
                        ? t("yearly")
                        : t("monthly")}
                    </p>
                  </div>
                  <p className="text-sm text-muted">
                    {t("nextRenewal")}: {formatDate(item.nextRenewalDate, locale)}
                  </p>
                  <div className="flex gap-2 pt-1">
                    <Link
                      href={`/subscriptions/${item.id}`}
                      className="inline-flex h-8 items-center gap-1 rounded-md border border-border px-3 text-xs"
                    >
                      <Pencil className="h-3 w-3" />
                      {t("edit")}
                    </Link>
                    {!item.archived ? (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => archive(item.id)}
                      >
                        <Archive className="h-3 w-3" />
                        {t("archive")}
                      </Button>
                    ) : null}
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
