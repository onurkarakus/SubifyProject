/**
 * API `GET /api/exchange-rates` shape: 1 <base> = rates[target] <target>.
 * Matches snapshot rows (BaseCurrency → TargetCurrency).
 */
export type FxRatesSnapshot = {
  base: string;
  rates: Record<string, number>;
};

function formatMoneyLocal(
  amount: number,
  currency: string,
  locale: string,
): string {
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

export type ConvertCurrencyResult = {
  amount: number;
  currency: string;
  converted: boolean;
  /** Multiplier applied: amount_from * rateUsed ≈ amount_to (when converted). */
  rateUsed?: number;
};

export type MoneyDualKind = "same" | "converted" | "rate_missing";

/**
 * Dual money display (task 16.1.1).
 * - same currency → only primary
 * - foreign + rate → primary main + secondary "(original)"
 * - foreign + no rate → primary original, rateMissing
 */
export type MoneyDualResult = {
  kind: MoneyDualKind;
  /** Formatted main (or original when rate missing / same). */
  primaryText: string;
  /** e.g. "(49,99 USD)" — only when kind === "converted". */
  secondaryText?: string;
  /** Combined for simple one-line UI. */
  displayText: string;
  primaryAmount: number;
  primaryCurrency: string;
  originalAmount: number;
  originalCurrency: string;
  converted: boolean;
  rateMissing: boolean;
  rateUsed?: number;
};

function normalizeCurrency(code: string): string {
  return (code ?? "").trim().toUpperCase();
}

function roundMoney(n: number): number {
  return Math.round((n + Number.EPSILON) * 100) / 100;
}

/**
 * Convert amount from → to using a rates snapshot (base + targets).
 * Supports direct base→target, inverse target→base, and cross via base.
 */
export function convertCurrency(
  amount: number,
  fromCurrency: string,
  toCurrency: string,
  snapshot?: FxRatesSnapshot | null,
): ConvertCurrencyResult {
  const from = normalizeCurrency(fromCurrency);
  const to = normalizeCurrency(toCurrency);

  if (!from || !to || Number.isNaN(amount)) {
    return { amount, currency: from || to, converted: false };
  }

  if (from === to) {
    return { amount: roundMoney(amount), currency: to, converted: true, rateUsed: 1 };
  }

  if (!snapshot?.base || !snapshot.rates) {
    return { amount: roundMoney(amount), currency: from, converted: false };
  }

  const base = normalizeCurrency(snapshot.base);
  const rates = snapshot.rates;

  // 1 base = rates[target] target
  const rateBaseTo = (target: string): number | null => {
    const t = normalizeCurrency(target);
    if (t === base) return 1;
    const r = rates[t] ?? rates[t.toLowerCase()];
    if (r == null || !(r > 0)) return null;
    return r;
  };

  // from → base → to
  let inBase: number | null = null;
  let rateFromToBase: number | null = null;

  if (from === base) {
    inBase = amount;
    rateFromToBase = 1;
  } else {
    const r = rateBaseTo(from);
    if (r != null) {
      // 1 base = r from ⇒ 1 from = 1/r base
      inBase = amount / r;
      rateFromToBase = 1 / r;
    }
  }

  if (inBase == null || rateFromToBase == null) {
    return { amount: roundMoney(amount), currency: from, converted: false };
  }

  if (to === base) {
    return {
      amount: roundMoney(inBase),
      currency: to,
      converted: true,
      rateUsed: rateFromToBase,
    };
  }

  const rTo = rateBaseTo(to);
  if (rTo == null) {
    return { amount: roundMoney(amount), currency: from, converted: false };
  }

  // amount_to = inBase * rates[to]
  const converted = inBase * rTo;
  return {
    amount: roundMoney(converted),
    currency: to,
    converted: true,
    rateUsed: rateFromToBase * rTo,
  };
}

export type FormatMoneyDualOptions = {
  locale?: string;
  /**
   * Snapshot for client-side conversion when `mainAmount` is not provided.
   */
  rates?: FxRatesSnapshot | null;
  /**
   * When API already converted to main (e.g. userShare in main currency),
   * skip rate lookup and show dual with this primary amount.
   */
  mainAmount?: number | null;
  /** Label shown only when rate is missing (caller passes i18n). Default: none in text. */
  rateMissingLabel?: string;
};

/**
 * Format amount in original currency for dual display against main currency.
 *
 * @example
 * // same
 * formatMoneyDual(100, "TRY", "TRY") → primary only
 * // converted
 * formatMoneyDual(10, "USD", "TRY", { rates: { base: "TRY", rates: { USD: 0.03 } } })
 * // rate missing
 * formatMoneyDual(10, "USD", "TRY", { rates: null, rateMissingLabel: "Kur yok" })
 */
export function formatMoneyDual(
  amount: number,
  currency: string,
  mainCurrency: string,
  options: FormatMoneyDualOptions = {},
): MoneyDualResult {
  const locale = options.locale ?? "tr";
  const originalCurrency = normalizeCurrency(currency);
  const main = normalizeCurrency(mainCurrency);
  const originalAmount = roundMoney(amount);

  const same = originalCurrency === main;

  if (same) {
    const primaryText = formatMoneyLocal(
      originalAmount,
      originalCurrency,
      locale,
    );
    return {
      kind: "same",
      primaryText,
      displayText: primaryText,
      primaryAmount: originalAmount,
      primaryCurrency: originalCurrency,
      originalAmount,
      originalCurrency,
      converted: true,
      rateMissing: false,
      rateUsed: 1,
    };
  }

  // Prefer API-provided main amount
  if (options.mainAmount != null && !Number.isNaN(options.mainAmount)) {
    const primaryAmount = roundMoney(options.mainAmount);
    const primaryText = formatMoneyLocal(primaryAmount, main, locale);
    const secondaryText = `(${formatMoneyLocal(originalAmount, originalCurrency, locale)})`;
    return {
      kind: "converted",
      primaryText,
      secondaryText,
      displayText: `${primaryText} ${secondaryText}`,
      primaryAmount,
      primaryCurrency: main,
      originalAmount,
      originalCurrency,
      converted: true,
      rateMissing: false,
    };
  }

  const conv = convertCurrency(
    originalAmount,
    originalCurrency,
    main,
    options.rates,
  );

  if (conv.converted && conv.currency === main) {
    const primaryText = formatMoneyLocal(conv.amount, main, locale);
    const secondaryText = `(${formatMoneyLocal(originalAmount, originalCurrency, locale)})`;
    return {
      kind: "converted",
      primaryText,
      secondaryText,
      displayText: `${primaryText} ${secondaryText}`,
      primaryAmount: conv.amount,
      primaryCurrency: main,
      originalAmount,
      originalCurrency,
      converted: true,
      rateMissing: false,
      rateUsed: conv.rateUsed,
    };
  }

  // Rate missing: show original as primary
  const primaryText = formatMoneyLocal(
    originalAmount,
    originalCurrency,
    locale,
  );
  const label = options.rateMissingLabel?.trim();
  const displayText = label ? `${primaryText} · ${label}` : primaryText;

  return {
    kind: "rate_missing",
    primaryText,
    displayText,
    primaryAmount: originalAmount,
    primaryCurrency: originalCurrency,
    originalAmount,
    originalCurrency,
    converted: false,
    rateMissing: true,
  };
}

/** Normalize API response into FxRatesSnapshot. */
export function toFxRatesSnapshot(input: {
  base?: string | null;
  rates?: Record<string, number> | null;
}): FxRatesSnapshot | null {
  if (!input.base || !input.rates) return null;
  const base = normalizeCurrency(input.base);
  const rates: Record<string, number> = {};
  for (const [k, v] of Object.entries(input.rates)) {
    if (typeof v === "number" && v > 0) {
      rates[normalizeCurrency(k)] = v;
    }
  }
  if (Object.keys(rates).length === 0) return null;
  return { base, rates };
}
