"use client";

import { InfoTip } from "@/components/ui/info-tip";
import {
  formatMoneyDual,
  type FxRatesSnapshot,
  type MoneyDualResult,
} from "@/lib/fx/money-dual";
import { useI18n } from "@/lib/i18n/context";
import { cn } from "@/lib/utils";

type MoneyDualProps = {
  amount: number;
  currency: string;
  mainCurrency: string;
  rates?: FxRatesSnapshot | null;
  /** When API already converted to main. */
  mainAmount?: number | null;
  className?: string;
  /** Primary amount size */
  size?: "sm" | "md" | "lg";
  /** Show secondary on its own line under primary */
  stacked?: boolean;
};

const sizeClass = {
  sm: "text-sm font-semibold",
  md: "text-base font-semibold",
  lg: "text-2xl font-bold",
} as const;

/**
 * Dual currency display (16.1.1 / 16.1.2):
 * main first, original in parentheses when different + rate available.
 */
export function MoneyDual({
  amount,
  currency,
  mainCurrency,
  rates,
  mainAmount,
  className,
  size = "md",
  stacked = false,
}: MoneyDualProps) {
  const { t, locale } = useI18n();
  const dual = formatMoneyDual(amount, currency, mainCurrency, {
    locale,
    rates,
    mainAmount,
    rateMissingLabel: t("fxRateMissing"),
  });

  return (
    <MoneyDualView
      dual={dual}
      className={className}
      size={size}
      stacked={stacked}
    />
  );
}

export function MoneyDualView({
  dual,
  className,
  size = "md",
  stacked = false,
}: {
  dual: MoneyDualResult;
  className?: string;
  size?: "sm" | "md" | "lg";
  stacked?: boolean;
}) {
  const { t } = useI18n();

  if (stacked) {
    return (
      <span className={cn("inline-flex flex-col items-start gap-0.5", className)}>
        <span
          className={cn(
            "inline-flex flex-wrap items-baseline gap-1 tabular-nums text-foreground",
            sizeClass[size],
          )}
        >
          {dual.primaryText}
          {dual.rateMissing ? (
            <InfoTip label={t("moreInfo")}>{t("fxRateMissingHint")}</InfoTip>
          ) : null}
        </span>
        {dual.secondaryText ? (
          <span className="text-xs tabular-nums text-muted">
            {dual.secondaryText}
          </span>
        ) : dual.rateMissing ? (
          <span className="text-[10px] font-normal text-warning">
            {t("fxRateMissing")}
          </span>
        ) : null}
      </span>
    );
  }

  return (
    <span
      className={cn(
        "inline-flex flex-wrap items-baseline gap-x-1.5 gap-y-0.5 tabular-nums",
        className,
      )}
    >
      <span className={cn("text-foreground", sizeClass[size])}>
        {dual.primaryText}
      </span>
      {dual.secondaryText ? (
        <span className="text-xs font-normal text-muted sm:text-sm">
          {dual.secondaryText}
        </span>
      ) : null}
      {dual.rateMissing ? (
        <InfoTip label={t("moreInfo")}>{t("fxRateMissingHint")}</InfoTip>
      ) : null}
    </span>
  );
}
