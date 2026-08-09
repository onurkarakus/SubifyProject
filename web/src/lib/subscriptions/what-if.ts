import type { SubscriptionItem } from "@/lib/api/types";
import { convertCurrency, type FxRatesSnapshot } from "@/lib/fx/money-dual";
import { normalizeBillingCycle } from "@/lib/utils";

export type WhatIfScenario = {
  /** Subscription ids to exclude (simulate cancel). */
  excludeIds: Set<string>;
  /** Ids to treat as yearly (simulate switch). */
  forceYearlyIds: Set<string>;
};

export type WhatIfResult = {
  baselineMonthly: number;
  scenarioMonthly: number;
  savedMonthly: number;
  budget: number | null;
  baselineRemaining: number | null;
  scenarioRemaining: number | null;
};

function monthlyShareInMain(
  item: SubscriptionItem,
  mainCurrency: string,
  rates: FxRatesSnapshot | null,
  forceYearly: boolean,
): number {
  const cycle = forceYearly
    ? "yearly"
    : normalizeBillingCycle(item.billingCycle);
  let monthly = item.monthlyEquivalentShare ?? item.userShare;
  if (forceYearly && cycle === "yearly") {
    // userShare is full yearly share → monthly eq
    monthly = item.userShare / 12;
  } else if (forceYearly && normalizeBillingCycle(item.billingCycle) === "monthly") {
    // simulate yearly discount: keep monthly as-is (no price change model)
    monthly = item.monthlyEquivalentShare ?? item.userShare;
  }

  const conv = convertCurrency(monthly, item.currency, mainCurrency, rates);
  return conv.converted ? conv.amount : 0;
}

/**
 * Client-side budget what-if (16.3.3): exclude subs or mark yearly.
 * Yearly switch currently keeps same monthly equivalent (no external pricing).
 */
export function computeWhatIf(
  items: SubscriptionItem[],
  mainCurrency: string,
  rates: FxRatesSnapshot | null,
  budget: number | null | undefined,
  scenario: WhatIfScenario,
): WhatIfResult {
  const active = items.filter((i) => !i.archived);

  let baselineMonthly = 0;
  let scenarioMonthly = 0;

  for (const item of active) {
    const base = monthlyShareInMain(item, mainCurrency, rates, false);
    baselineMonthly += base;

    if (scenario.excludeIds.has(item.id)) continue;
    const forceY = scenario.forceYearlyIds.has(item.id);
    scenarioMonthly += monthlyShareInMain(item, mainCurrency, rates, forceY);
  }

  baselineMonthly = round2(baselineMonthly);
  scenarioMonthly = round2(scenarioMonthly);
  const savedMonthly = round2(baselineMonthly - scenarioMonthly);
  const b = budget != null && budget > 0 ? budget : null;

  return {
    baselineMonthly,
    scenarioMonthly,
    savedMonthly,
    budget: b,
    baselineRemaining: b != null ? round2(b - baselineMonthly) : null,
    scenarioRemaining: b != null ? round2(b - scenarioMonthly) : null,
  };
}

function round2(n: number): number {
  return Math.round((n + Number.EPSILON) * 100) / 100;
}
