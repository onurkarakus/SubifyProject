import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatMoney(amount: number, currency = "TRY", locale = "tr") {
  try {
    return new Intl.NumberFormat(locale === "en" ? "en-US" : "tr-TR", {
      style: "currency",
      currency,
      maximumFractionDigits: 2,
    }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}

/** Multi-currency dual display (task 16.1.1). */
export {
  convertCurrency,
  formatMoneyDual,
  toFxRatesSnapshot,
  type ConvertCurrencyResult,
  type FormatMoneyDualOptions,
  type FxRatesSnapshot,
  type MoneyDualKind,
  type MoneyDualResult,
} from "@/lib/fx/money-dual";

export function formatDate(value: string | Date, locale = "tr") {
  const d = typeof value === "string" ? new Date(value) : value;
  return new Intl.DateTimeFormat(locale === "en" ? "en-GB" : "tr-TR", {
    year: "numeric",
    month: "short",
    day: "numeric",
  }).format(d);
}

/** API may send enum as number (1/2) or string ("monthly"/"Monthly"). */
export function normalizeBillingCycle(
  value: unknown,
): "monthly" | "yearly" {
  if (value === 1 || value === "1") return "monthly";
  if (value === 2 || value === "2") return "yearly";
  if (typeof value === "string") {
    const v = value.trim().toLowerCase();
    if (v === "monthly" || v === "yearly") return v;
  }
  return "monthly";
}
