"use client";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { Input } from "@/components/ui/input";
import { FxStatusBanner } from "@/components/fx/fx-status-banner";
import { MoneyDual } from "@/components/ui/money-dual";
import { PageLoader } from "@/components/ui/spinner";
import { api, ApiError } from "@/lib/api/client";
import type { ListSubscriptionsResponse, SubscriptionItem } from "@/lib/api/types";
import { useFxRates } from "@/lib/fx/use-fx-rates";
import { useI18n } from "@/lib/i18n/context";
import {
  draftToCreateBody,
  importDraftIsValid,
  importTemplateCsv,
  parseSubscriptionCsv,
  type ImportRowDraft,
} from "@/lib/subscriptions/import-csv";
import {
  downloadTextFile,
  stampFilename,
} from "@/lib/reports/export-csv";
import { cn, formatDate, formatMoney, normalizeBillingCycle } from "@/lib/utils";
import { Archive, Pencil, Plus, Upload } from "lucide-react";
import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
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
  const [importOpen, setImportOpen] = useState(false);
  const [importDrafts, setImportDrafts] = useState<ImportRowDraft[]>([]);
  const [importHeaderOk, setImportHeaderOk] = useState(true);
  const [importBusy, setImportBusy] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);

  const mainCurrency = data?.summary?.currency ?? "TRY";
  const {
    snapshot: fxRates,
    isStale: fxStale,
    lastUpdated: fxUpdated,
  } = useFxRates(data?.summary?.currency ?? null);

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

  const importValidCount = importDrafts.filter(importDraftIsValid).length;

  async function onImportFile(file: File) {
    const text = await file.text();
    const parsed = parseSubscriptionCsv(text);
    setImportHeaderOk(parsed.headerOk);
    setImportDrafts(parsed.drafts);
    setImportOpen(true);
    if (!parsed.headerOk) {
      toast.error(t("importHeaderError"));
    } else if (!parsed.drafts.length) {
      toast.error(t("importEmpty"));
    }
  }

  async function runImport() {
    const valid = importDrafts.filter(importDraftIsValid);
    if (!valid.length) {
      toast.error(t("importEmpty"));
      return;
    }
    setImportBusy(true);
    let ok = 0;
    let fail = 0;
    for (const d of valid) {
      try {
        await api.post("/subscriptions", draftToCreateBody(d));
        ok += 1;
      } catch {
        fail += 1;
      }
    }
    setImportBusy(false);
    toast.success(
      t("importDone")
        .replace("{ok}", String(ok))
        .replace("{fail}", String(fail)),
    );
    setImportDrafts([]);
    setImportOpen(false);
    await load();
  }

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="page-title">{t("subscriptions")}</h1>
        <div className="flex flex-wrap items-center gap-2">
          <input
            ref={fileRef}
            type="file"
            accept=".csv,text/csv"
            className="hidden"
            onChange={(e) => {
              const f = e.target.files?.[0];
              if (f) void onImportFile(f);
              e.target.value = "";
            }}
          />
          <Button
            type="button"
            variant="secondary"
            onClick={() => fileRef.current?.click()}
          >
            <Upload className="h-4 w-4" />
            {t("importCsv")}
          </Button>
          <button
            type="button"
            className="text-xs font-medium text-primary hover:underline"
            onClick={() => {
              downloadTextFile(
                stampFilename("subify-import-template"),
                importTemplateCsv(),
              );
              toast.success(t("exportDownloaded"));
            }}
          >
            {t("importTemplate")}
          </button>
          <Link
            href="/subscriptions/new"
            className="inline-flex h-10 items-center gap-2 rounded-full bg-primary px-5 text-sm font-medium text-white shadow-[var(--shadow-glow)] hover:bg-primary-hover"
          >
            <Plus className="h-4 w-4" />
            {t("addSubscription")}
          </Link>
        </div>
      </div>

      {importOpen && importDrafts.length > 0 ? (
        <Card>
          <CardContent className="space-y-3 p-5">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div>
                <p className="font-semibold">{t("importDryRun")}</p>
                <p className="text-xs text-muted">
                  {t("importReady")
                    .replace("{n}", String(importValidCount))
                    .replace("{t}", String(importDrafts.length))}
                  {!importHeaderOk ? ` · ${t("importHeaderError")}` : ""}
                </p>
              </div>
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => {
                    setImportOpen(false);
                    setImportDrafts([]);
                  }}
                >
                  {t("cancel")}
                </Button>
                <Button
                  type="button"
                  disabled={importBusy || importValidCount === 0}
                  onClick={() => void runImport()}
                >
                  {importBusy ? t("loading") : t("importConfirm")}
                </Button>
              </div>
            </div>
            <div className="max-h-56 overflow-auto rounded-xl border border-border">
              <table className="w-full text-left text-xs">
                <thead className="bg-muted/20 text-muted">
                  <tr>
                    <th className="px-2 py-1.5">#</th>
                    <th className="px-2 py-1.5">{t("name")}</th>
                    <th className="px-2 py-1.5">{t("price")}</th>
                    <th className="px-2 py-1.5">{t("currency")}</th>
                    <th className="px-2 py-1.5">{t("status")}</th>
                  </tr>
                </thead>
                <tbody>
                  {importDrafts.map((d) => (
                    <tr key={d.line} className="border-t border-border">
                      <td className="px-2 py-1.5 text-muted">{d.line}</td>
                      <td className="px-2 py-1.5 font-medium">{d.name || "—"}</td>
                      <td className="px-2 py-1.5 tabular-nums">{d.price}</td>
                      <td className="px-2 py-1.5">{d.currency}</td>
                      <td className="px-2 py-1.5">
                        {importDraftIsValid(d) ? (
                          <span className="text-success">{t("importRowOk")}</span>
                        ) : (
                          <span className="text-danger">
                            {d.errors.join(", ")}
                          </span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      ) : null}

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

      {data ? (
        <FxStatusBanner
          mainCurrency={mainCurrency}
          items={data.data ?? []}
          rates={fxRates}
          isStale={fxStale}
          lastUpdated={fxUpdated}
          apiHasUnconverted={data.summary?.hasUnconvertedAmounts}
        />
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
                    <div className="flex flex-col items-end gap-1">
                      {state === "overdue" ? (
                        <Badge variant="danger">{t("overdue")}</Badge>
                      ) : state === "soon" ? (
                        <Badge variant="warning">{t("soon")}</Badge>
                      ) : (
                        <Badge variant="muted">{t("normal")}</Badge>
                      )}
                      {item.latestPriceChange?.isIncrease ? (
                        <Badge variant="danger">{t("priceIncreaseBadge")}</Badge>
                      ) : item.latestPriceChange?.isDecrease ? (
                        <Badge variant="muted">{t("priceDecreaseBadge")}</Badge>
                      ) : item.latestPriceChange ? (
                        <Badge variant="muted">{t("priceChangeBadge")}</Badge>
                      ) : null}
                    </div>
                  </div>
                  <div className="space-y-1">
                    <MoneyDual
                      amount={item.price}
                      currency={item.currency}
                      mainCurrency={mainCurrency}
                      rates={fxRates}
                      size="lg"
                      stacked
                    />
                    <p className="text-xs text-muted">
                      {t("yourShare")}:{" "}
                      <MoneyDual
                        amount={item.userShare}
                        currency={item.currency}
                        mainCurrency={mainCurrency}
                        rates={fxRates}
                        size="sm"
                        className="align-baseline"
                      />{" "}
                      ·{" "}
                      {normalizeBillingCycle(item.billingCycle) === "yearly"
                        ? t("yearly")
                        : t("monthly")}
                    </p>
                  </div>
                  <p className="text-sm text-muted">
                    {t("nextRenewal")}: {formatDate(item.nextRenewalDate, locale)}
                  </p>
                  <div className="flex flex-wrap gap-2 pt-1">
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
