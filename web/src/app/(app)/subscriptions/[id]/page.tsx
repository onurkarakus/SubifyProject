"use client";

import { SubscriptionForm } from "@/components/subscriptions/subscription-form";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { MoneyDual } from "@/components/ui/money-dual";
import { PageLoader } from "@/components/ui/spinner";
import { api, ApiError } from "@/lib/api/client";
import type { ProfileResponse, SubscriptionItem } from "@/lib/api/types";
import { useFxRates } from "@/lib/fx/use-fx-rates";
import { useI18n } from "@/lib/i18n/context";
import { formatDate, formatMoney, normalizeBillingCycle } from "@/lib/utils";
import { TrendingDown, TrendingUp } from "lucide-react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { toast } from "sonner";

export default function EditSubscriptionPage() {
  const { t, locale } = useI18n();
  const params = useParams();
  const id = params.id as string;
  const [item, setItem] = useState<SubscriptionItem | null>(null);
  const [mainCurrency, setMainCurrency] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const { snapshot: fxRates } = useFxRates(mainCurrency);

  useEffect(() => {
    let cancelled = false;
    void (async () => {
      setLoading(true);
      setError(null);
      try {
        const [data, profile] = await Promise.all([
          api.get<SubscriptionItem>(`/subscriptions/${id}`),
          api.get<ProfileResponse>("/profile").catch(() => null),
        ]);
        if (cancelled) return;
        setItem(data);
        setMainCurrency(profile?.mainCurrency ?? data.currency ?? "TRY");
      } catch (e) {
        const msg = e instanceof ApiError ? e.message : t("errorGeneric");
        if (!cancelled) {
          setError(msg);
          toast.error(msg);
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [id, t]);

  if (loading) return <PageLoader />;

  if (error || !item) {
    return (
      <div className="mx-auto max-w-xl space-y-4">
        <h1 className="text-2xl font-bold">{t("errorGeneric")}</h1>
        <p className="text-sm text-muted">{error ?? t("empty")}</p>
        <Link
          href="/subscriptions"
          className="inline-flex h-10 items-center rounded-lg border border-border px-4 text-sm font-medium hover:bg-muted/40"
        >
          {t("subscriptions")}
        </Link>
      </div>
    );
  }

  const main = mainCurrency ?? item.currency;
  const cycle = normalizeBillingCycle(item.billingCycle);
  const monthly = item.monthlyEquivalentShare ?? item.userShare;

  return (
    <div className="mx-auto max-w-xl space-y-4">
      <h1 className="text-2xl font-bold">
        {t("edit")}: {item.name}
      </h1>

      <Card>
        <CardContent className="grid gap-3 p-5 sm:grid-cols-2">
          <div>
            <p className="mb-1 text-xs text-muted">{t("price")}</p>
            <MoneyDual
              amount={item.price}
              currency={item.currency}
              mainCurrency={main}
              rates={fxRates}
              size="lg"
              stacked
            />
          </div>
          <div>
            <p className="mb-1 text-xs text-muted">{t("yourShare")}</p>
            <MoneyDual
              amount={item.userShare}
              currency={item.currency}
              mainCurrency={main}
              rates={fxRates}
              size="md"
              stacked
            />
            <p className="mt-1 text-xs text-muted">
              {cycle === "yearly" ? t("yearly") : t("monthly")}
            </p>
          </div>
          <div className="sm:col-span-2">
            <p className="mb-1 text-xs text-muted">{t("reportsMonthlyShare")}</p>
            <MoneyDual
              amount={monthly}
              currency={item.currency}
              mainCurrency={main}
              rates={fxRates}
              size="md"
            />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="space-y-3 p-5">
          <div className="flex items-center justify-between gap-2">
            <p className="text-sm font-semibold">{t("priceHistoryTitle")}</p>
            {item.latestPriceChange?.isIncrease ? (
              <Badge variant="danger">{t("priceIncreaseBadge")}</Badge>
            ) : item.latestPriceChange?.isDecrease ? (
              <Badge variant="muted">{t("priceDecreaseBadge")}</Badge>
            ) : null}
          </div>
          {!item.priceHistory?.length ? (
            <p className="text-sm text-muted">{t("priceHistoryEmpty")}</p>
          ) : (
            <ul className="space-y-2">
              {item.priceHistory.map((h) => (
                <li
                  key={h.id}
                  className="flex items-start justify-between gap-3 rounded-lg border border-border px-3 py-2 text-sm"
                >
                  <div className="min-w-0">
                    <p className="flex items-center gap-1.5 font-medium tabular-nums">
                      {h.isIncrease ? (
                        <TrendingUp className="h-3.5 w-3.5 shrink-0 text-danger" />
                      ) : h.isDecrease ? (
                        <TrendingDown className="h-3.5 w-3.5 shrink-0 text-success" />
                      ) : null}
                      {t("priceHistoryFromTo")
                        .replace(
                          "{old}",
                          formatMoney(h.oldPrice, h.oldCurrency, locale),
                        )
                        .replace(
                          "{new}",
                          formatMoney(h.newPrice, h.newCurrency, locale),
                        )}
                    </p>
                    <p className="text-xs text-muted">
                      {formatDate(h.changedAt, locale)}
                    </p>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <SubscriptionForm mode="edit" initial={item} />
    </div>
  );
}
