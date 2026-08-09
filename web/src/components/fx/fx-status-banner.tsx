"use client";

import {
  countMissingConversions,
  isFxSnapshotStale,
} from "@/lib/fx/fx-health";
import type { FxRatesSnapshot } from "@/lib/fx/money-dual";
import { useAuth } from "@/lib/auth/context";
import { useI18n } from "@/lib/i18n/context";
import { cn, formatDate } from "@/lib/utils";
import { AlertTriangle } from "lucide-react";
import Link from "next/link";
import { useMemo } from "react";

type FxStatusBannerProps = {
  mainCurrency: string;
  /** Rows with a currency field (subs, upcoming, etc.). */
  items?: { currency: string }[];
  rates?: FxRatesSnapshot | null;
  isStale?: boolean;
  lastUpdated?: string | null;
  /** Server already reported unconverted totals. */
  apiHasUnconverted?: boolean;
  className?: string;
};

/**
 * Shared FX health strip (task 16.1.6):
 * stale snapshot, missing conversion count, optional SuperAdmin hint.
 */
export function FxStatusBanner({
  mainCurrency,
  items = [],
  rates,
  isStale,
  lastUpdated,
  apiHasUnconverted,
  className,
}: FxStatusBannerProps) {
  const { t, locale } = useI18n();
  const { isSuperAdmin } = useAuth();

  const stale = isFxSnapshotStale(lastUpdated, isStale);
  const missing = useMemo(
    () => countMissingConversions(items, mainCurrency, rates),
    [items, mainCurrency, rates],
  );

  const showMissing =
    missing.missingItemCount > 0 ||
    missing.missingCurrencies.length > 0 ||
    !!apiHasUnconverted;

  if (!stale && !showMissing) return null;

  return (
    <div
      className={cn(
        "space-y-1.5 rounded-2xl border border-warning/40 bg-warning/10 px-4 py-3 text-sm text-foreground",
        className,
      )}
      role="status"
    >
      <div className="flex items-start gap-2">
        <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-warning" />
        <div className="min-w-0 space-y-1">
          {stale ? (
            <div className="space-y-0.5">
              <p>
                <span className="font-medium">{t("fxSidebarStale")}</span>
                {lastUpdated ? (
                  <span className="text-muted">
                    {" "}
                    · {t("fxSidebarAsOf")} {formatDate(lastUpdated, locale)}
                  </span>
                ) : null}
              </p>
              <p className="text-xs text-muted">{t("fxSidebarStaleHint")}</p>
            </div>
          ) : null}

          {showMissing ? (
            <p>
              {missing.missingItemCount > 0 ? (
                <>
                  {t("fxMissingCount").replace(
                    "{n}",
                    String(missing.missingItemCount),
                  )}
                  {missing.missingCurrencies.length > 0 ? (
                    <span className="text-muted">
                      {" "}
                      ({missing.missingCurrencies.join(", ")})
                    </span>
                  ) : null}
                </>
              ) : (
                t("fxUnconvertedWarning")
              )}
            </p>
          ) : null}

          {isSuperAdmin ? (
            <p className="text-xs text-muted">
              {t("fxSuperAdminHint")}{" "}
              <Link
                href="/admin/settings?tab=ops"
                className="font-medium text-primary hover:underline"
              >
                {t("adminOpsTab")}
              </Link>
            </p>
          ) : (
            <p className="text-xs text-muted">
              {t("fxUserHint")}{" "}
              <Link
                href="/profile"
                className="font-medium text-primary hover:underline"
              >
                {t("profile")}
              </Link>
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
