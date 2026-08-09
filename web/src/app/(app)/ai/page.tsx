"use client";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { PageLoader } from "@/components/ui/spinner";
import { api, ApiError } from "@/lib/api/client";
import type {
  AiAnalyzeResponse,
  AiHistoryDetailResponse,
  AiHistoryResponse,
} from "@/lib/api/types";
import { useI18n } from "@/lib/i18n/context";
import { cn, formatDate, formatMoney } from "@/lib/utils";
import { Archive, ChevronRight, ExternalLink } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";
import { toast } from "sonner";

type DetailView = (AiAnalyzeResponse | AiHistoryDetailResponse) & {
  source: "live" | "history";
};

export default function AiPage() {
  const { t, locale } = useI18n();
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [detailLoadingId, setDetailLoadingId] = useState<string | null>(null);
  const [detail, setDetail] = useState<DetailView | null>(null);
  const [selectedHistoryId, setSelectedHistoryId] = useState<string | null>(
    null,
  );
  const [history, setHistory] = useState<AiHistoryResponse | null>(null);
  const [keyMissing, setKeyMissing] = useState(false);
  const [archivingId, setArchivingId] = useState<string | null>(null);

  async function loadHistory() {
    try {
      const h = await api.get<AiHistoryResponse>(
        "/ai/history?page=1&pageSize=20",
      );
      setHistory(h);
    } catch {
      // ignore
    }
  }

  useEffect(() => {
    void (async () => {
      await loadHistory();
      setLoading(false);
    })();
  }, []);

  async function analyze() {
    setBusy(true);
    setKeyMissing(false);
    try {
      const res = await api.post<AiAnalyzeResponse>("/ai/analyze", {
        lang: locale,
      });
      setDetail({ ...res, source: "live" });
      setSelectedHistoryId(null);
      toast.success(t("analyze"));
      await loadHistory();
    } catch (e) {
      if (e instanceof ApiError && e.code === "AI_KEY_MISSING") {
        setKeyMissing(true);
        toast.error(t("aiKeyMissing"));
      } else {
        toast.error(e instanceof ApiError ? e.message : t("errorGeneric"));
      }
    } finally {
      setBusy(false);
    }
  }

  async function openHistoryItem(id: string) {
    setDetailLoadingId(id);
    try {
      const res = await api.get<AiHistoryDetailResponse>(`/ai/history/${id}`);
      setDetail({ ...res, source: "history" });
      setSelectedHistoryId(id);
      // Scroll detail into view on mobile
      document
        .getElementById("ai-detail")
        ?.scrollIntoView({ behavior: "smooth", block: "start" });
    } catch (e) {
      toast.error(e instanceof ApiError ? e.message : t("errorGeneric"));
    } finally {
      setDetailLoadingId(null);
    }
  }

  async function archiveFromTip(subscriptionId: string) {
    if (!confirm(`${t("archive")}?`)) return;
    setArchivingId(subscriptionId);
    try {
      await api.delete(`/subscriptions/${subscriptionId}`);
      toast.success(t("archive"));
    } catch (e) {
      toast.error(e instanceof ApiError ? e.message : t("errorGeneric"));
    } finally {
      setArchivingId(null);
    }
  }

  if (loading) return <PageLoader />;

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="page-title">{t("ai")}</h1>
        <Button onClick={analyze} disabled={busy}>
          {busy ? t("loading") : t("analyze")}
        </Button>
      </div>

      {keyMissing ? (
        <div className="rounded-lg border border-warning/40 bg-warning/10 px-4 py-3 text-sm">
          {t("aiKeyMissing")}
        </div>
      ) : null}

      {detail ? (
        <Card id="ai-detail">
          <CardHeader className="space-y-1">
            <div className="flex flex-wrap items-center gap-2">
              <CardTitle className="text-lg">{detail.summary}</CardTitle>
              {detail.source === "history" ? (
                <Badge className="shrink-0 border border-border bg-surface text-foreground">
                  {t("aiHistoryFromPast")}
                </Badge>
              ) : null}
            </div>
            <p className="text-xs text-muted">
              {formatDate(
                "analyzedAt" in detail && detail.analyzedAt
                  ? detail.analyzedAt
                  : "createdAt" in detail
                    ? (detail as AiHistoryDetailResponse).createdAt
                    : detail.analyzedAt,
                locale,
              )}
            </p>
          </CardHeader>
          <CardContent className="space-y-3">
            <p className="text-sm text-muted">
              {t("aiSavingsLabel")}:{" "}
              {formatMoney(detail.estimatedMonthlySaving, "TRY", locale)}/
              {locale === "tr" ? "ay" : "mo"} ·{" "}
              {formatMoney(detail.estimatedYearlySaving, "TRY", locale)}/
              {locale === "tr" ? "yıl" : "yr"}
            </p>
            {detail.tips?.length ? (
              detail.tips.map((tip, i) => {
                const tipType = (tip.type || "").toLowerCase();
                const subId = tip.subscriptionId ?? null;
                return (
                  <div
                    key={i}
                    className="rounded-lg border border-border px-3 py-2"
                  >
                    <div className="mb-1 flex flex-wrap items-center gap-2">
                      <Badge>{tip.type}</Badge>
                      {tip.subscriptionName ? (
                        <span className="text-xs text-muted">
                          {tip.subscriptionName}
                        </span>
                      ) : null}
                    </div>
                    <p className="text-sm">{tip.message}</p>
                    {tip.potentialSaving != null ? (
                      <p className="mt-1 text-xs text-success">
                        ~{formatMoney(tip.potentialSaving, "TRY", locale)}
                      </p>
                    ) : null}
                    {subId || tipType === "unused" || tipType === "yearly" ? (
                      <div className="mt-2 flex flex-wrap gap-2">
                        {subId ? (
                          <Link
                            href={`/subscriptions/${subId}`}
                            className="inline-flex h-8 items-center gap-1 rounded-md border border-border px-2.5 text-xs font-medium hover:bg-muted/40"
                          >
                            <ExternalLink className="h-3 w-3" />
                            {t("aiTipOpenSub")}
                          </Link>
                        ) : null}
                        {subId && tipType === "unused" ? (
                          <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            disabled={archivingId === subId}
                            onClick={() => void archiveFromTip(subId)}
                          >
                            <Archive className="h-3 w-3" />
                            {archivingId === subId
                              ? t("loading")
                              : t("aiTipArchive")}
                          </Button>
                        ) : null}
                        {subId && tipType === "yearly" ? (
                          <Link
                            href={`/subscriptions/${subId}`}
                            className="inline-flex h-8 items-center gap-1 rounded-md border border-border px-2.5 text-xs font-medium text-primary hover:bg-primary-soft/40"
                          >
                            {t("aiTipReviewYearly")}
                          </Link>
                        ) : null}
                      </div>
                    ) : null}
                  </div>
                );
              })
            ) : (
              <p className="text-sm text-muted">{t("empty")}</p>
            )}
          </CardContent>
        </Card>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle>{t("aiHistory")}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-1">
          {!history?.data?.length ? (
            <EmptyState title={t("aiHistoryEmpty")} />
          ) : (
            history.data.map((h) => {
              const active = selectedHistoryId === h.id;
              const rowBusy = detailLoadingId === h.id;
              return (
                <button
                  key={h.id}
                  type="button"
                  disabled={rowBusy || detailLoadingId !== null}
                  onClick={() => void openHistoryItem(h.id)}
                  className={cn(
                    "flex w-full items-center gap-3 rounded-lg border border-transparent px-3 py-2.5 text-left transition-colors",
                    "hover:border-border hover:bg-muted/20",
                    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary",
                    "disabled:opacity-60",
                    active && "border-primary/40 bg-primary/5",
                  )}
                  aria-label={`${t("aiHistoryOpen")}: ${h.summary}`}
                >
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-medium">{h.summary}</p>
                    <p className="text-xs text-muted">
                      {formatDate(h.createdAt, locale)} ·{" "}
                      {formatMoney(h.estimatedMonthlySaving, "TRY", locale)}/mo
                    </p>
                  </div>
                  <span className="shrink-0 text-xs text-muted">
                    {rowBusy ? t("loading") : t("aiHistoryOpen")}
                  </span>
                  <ChevronRight
                    className="h-4 w-4 shrink-0 text-muted"
                    aria-hidden
                  />
                </button>
              );
            })
          )}
        </CardContent>
      </Card>
    </div>
  );
}
