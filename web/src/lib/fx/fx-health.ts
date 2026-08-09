import {
  convertCurrency,
  type FxRatesSnapshot,
} from "@/lib/fx/money-dual";

/** Align with API GetExchangeRatesHandler StaleAfter (6h). */
export const FX_STALE_HOURS = 6;

/**
 * Client-side stale check when API flag is false but lastUpdated is old.
 */
export function isFxSnapshotStale(
  lastUpdated: string | null | undefined,
  isStaleFlag?: boolean,
  staleAfterHours: number = FX_STALE_HOURS,
): boolean {
  if (isStaleFlag) return true;
  if (!lastUpdated) return false;
  const t = new Date(lastUpdated).getTime();
  if (Number.isNaN(t)) return false;
  const ageMs = Date.now() - t;
  return ageMs > staleAfterHours * 60 * 60 * 1000;
}

export type FxMissingStats = {
  /** Items whose currency ≠ main and conversion failed. */
  missingItemCount: number;
  /** Distinct foreign currencies without a usable rate. */
  missingCurrencies: string[];
};

/**
 * Count list/report rows that cannot convert to main currency.
 */
export function countMissingConversions(
  items: { currency: string }[],
  mainCurrency: string,
  rates: FxRatesSnapshot | null | undefined,
): FxMissingStats {
  const main = (mainCurrency ?? "").trim().toUpperCase();
  const missingCurrencies = new Set<string>();
  let missingItemCount = 0;

  for (const item of items) {
    const c = (item.currency ?? "").trim().toUpperCase();
    if (!c || c === main) continue;
    const conv = convertCurrency(1, c, main, rates);
    if (!conv.converted) {
      missingItemCount += 1;
      missingCurrencies.add(c);
    }
  }

  return {
    missingItemCount,
    missingCurrencies: [...missingCurrencies].sort(),
  };
}
