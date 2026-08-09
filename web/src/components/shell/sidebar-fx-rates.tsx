"use client";

import { api } from "@/lib/api/client";
import type { ProfileResponse } from "@/lib/api/types";
import { useFxRates } from "@/lib/fx/use-fx-rates";
import { useI18n } from "@/lib/i18n/context";
import { cn, formatDate } from "@/lib/utils";
import { ChevronDown, ChevronUp, RefreshCw } from "lucide-react";
import { useEffect, useMemo, useState } from "react";

/** Preferred quote currencies (1 foreign = X main). */
const QUOTE_PRIORITY = ["USD", "EUR", "GBP", "CHF", "TRY", "JPY", "CAD", "AUD"];

function formatRate(n: number, locale: string): string {
  return new Intl.NumberFormat(locale === "en" ? "en-US" : "tr-TR", {
    maximumFractionDigits: n >= 100 ? 2 : 4,
    minimumFractionDigits: 2,
  }).format(n);
}

/**
 * Sidebar rates strip (task 16.1.5): last snapshot for profile MainCurrency.
 * Shows 1 USD ≈ X TRY style lines; collapsible on mobile/desktop.
 */
export function SidebarFxRates({ className }: { className?: string }) {
  const { t, locale } = useI18n();
  const [mainCurrency, setMainCurrency] = useState<string | null>(null);
  const [expanded, setExpanded] = useState(true);

  useEffect(() => {
    let cancelled = false;
    void (async () => {
      try {
        const p = await api.get<ProfileResponse>("/profile");
        if (!cancelled) setMainCurrency(p.mainCurrency || "TRY");
      } catch {
        if (!cancelled) setMainCurrency("TRY");
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const { snapshot, isStale, lastUpdated, loading, error, refetch } =
    useFxRates(mainCurrency);

  const rows = useMemo(() => {
    if (!snapshot?.base || !snapshot.rates) return [];
    const base = snapshot.base.toUpperCase();
    const targets = QUOTE_PRIORITY.filter(
      (c) => c !== base && snapshot.rates[c] != null && snapshot.rates[c]! > 0,
    ).slice(0, 5);

    if (targets.length < 3) {
      for (const [code, rate] of Object.entries(snapshot.rates)) {
        const c = code.toUpperCase();
        if (c === base || rate <= 0 || targets.includes(c)) continue;
        targets.push(c);
        if (targets.length >= 5) break;
      }
    }

    return targets.map((code) => {
      const r = snapshot.rates[code]!;
      // API: 1 base = r code → 1 code = 1/r base (user-friendly quote)
      const oneForeignInMain = 1 / r;
      return {
        code,
        label: `1 ${code}`,
        value: formatRate(oneForeignInMain, locale),
        main: base,
      };
    });
  }, [snapshot, locale]);

  return (
    <div className={cn("border-t border-border px-3 py-2", className)}>
      <button
        type="button"
        onClick={() => setExpanded((e) => !e)}
        className="flex w-full items-center justify-between gap-2 rounded-lg px-2 py-1.5 text-left text-xs font-medium text-muted hover:bg-primary-soft/40 hover:text-foreground"
      >
        <span className="truncate">
          {t("fxSidebarTitle")}
          {mainCurrency ? (
            <span className="ml-1 font-normal opacity-80">
              ({mainCurrency})
            </span>
          ) : null}
        </span>
        {expanded ? (
          <ChevronDown className="h-3.5 w-3.5 shrink-0" />
        ) : (
          <ChevronUp className="h-3.5 w-3.5 shrink-0" />
        )}
      </button>

      {expanded ? (
        <div className="mt-1 space-y-1 px-2 pb-1">
          {loading && !snapshot ? (
            <p className="text-[11px] text-muted">{t("loading")}</p>
          ) : error || !rows.length ? (
            <p className="text-[11px] text-muted">{t("fxSidebarEmpty")}</p>
          ) : (
            <ul className="space-y-0.5">
              {rows.map((row) => (
                <li
                  key={row.code}
                  className="flex items-baseline justify-between gap-2 text-[11px] tabular-nums"
                >
                  <span className="text-muted">{row.label}</span>
                  <span className="font-medium text-foreground">
                    {row.value}{" "}
                    <span className="font-normal text-muted">{row.main}</span>
                  </span>
                </li>
              ))}
            </ul>
          )}

          <div className="flex items-center justify-between gap-1 pt-1">
            <p className="min-w-0 truncate text-[10px] text-muted">
              {isStale ? (
                <span className="text-warning">{t("fxSidebarStale")}</span>
              ) : lastUpdated ? (
                <>
                  {t("fxSidebarAsOf")} {formatDate(lastUpdated, locale)}
                </>
              ) : null}
            </p>
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                refetch();
              }}
              className="inline-flex h-6 w-6 shrink-0 items-center justify-center rounded-md text-muted hover:bg-primary-soft hover:text-primary"
              aria-label={t("fxSidebarRefresh")}
              title={t("fxSidebarRefresh")}
            >
              <RefreshCw
                className={cn("h-3 w-3", loading && "animate-spin")}
              />
            </button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
