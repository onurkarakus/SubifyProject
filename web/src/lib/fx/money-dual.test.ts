/**
 * Lightweight checks for 16.1.1 — run: npx --yes tsx src/lib/fx/money-dual.test.ts
 * (from web/)
 */
import assert from "node:assert/strict";
import {
  convertCurrency,
  formatMoneyDual,
  toFxRatesSnapshot,
} from "./money-dual";

const trySnapshot = {
  base: "TRY",
  rates: {
    USD: 0.03, // 1 TRY = 0.03 USD → 1 USD ≈ 33.333 TRY
    EUR: 0.028,
  },
};

// same currency
{
  const r = formatMoneyDual(100, "TRY", "TRY", { locale: "en" });
  assert.equal(r.kind, "same");
  assert.equal(r.rateMissing, false);
  assert.ok(!r.secondaryText);
  assert.match(r.primaryText, /100/);
}

// convert USD → TRY via inverse of TRY→USD
{
  const c = convertCurrency(3, "USD", "TRY", trySnapshot);
  assert.equal(c.converted, true);
  assert.equal(c.currency, "TRY");
  assert.equal(c.amount, 100); // 3 / 0.03
}

// dual converted
{
  const r = formatMoneyDual(3, "USD", "TRY", {
    locale: "en",
    rates: trySnapshot,
  });
  assert.equal(r.kind, "converted");
  assert.equal(r.primaryCurrency, "TRY");
  assert.equal(r.primaryAmount, 100);
  assert.equal(r.originalCurrency, "USD");
  // en-US formats USD as "$3.00"
  assert.ok(r.secondaryText?.includes("3"));
  assert.ok(r.displayText.includes("("));
}

// mainAmount shortcut (API already converted)
{
  const r = formatMoneyDual(10, "USD", "TRY", {
    locale: "en",
    mainAmount: 340,
  });
  assert.equal(r.kind, "converted");
  assert.equal(r.primaryAmount, 340);
  assert.equal(r.originalAmount, 10);
}

// rate missing
{
  const r = formatMoneyDual(10, "GBP", "TRY", {
    locale: "en",
    rates: trySnapshot,
    rateMissingLabel: "No rate",
  });
  assert.equal(r.kind, "rate_missing");
  assert.equal(r.rateMissing, true);
  assert.equal(r.primaryCurrency, "GBP");
  assert.match(r.displayText, /No rate/);
}

// toFxRatesSnapshot
{
  const s = toFxRatesSnapshot({
    base: "try",
    rates: { usd: 0.03, bad: -1 },
  });
  assert.ok(s);
  assert.equal(s!.base, "TRY");
  assert.equal(s!.rates.USD, 0.03);
  assert.equal(s!.rates.bad, undefined);
}

// EUR → USD cross via TRY
{
  // 1 EUR = 1/0.028 TRY; * 0.03 USD
  const c = convertCurrency(1, "EUR", "USD", trySnapshot);
  assert.equal(c.converted, true);
  assert.equal(c.currency, "USD");
  assert.ok(Math.abs(c.amount - round2(0.03 / 0.028)) < 0.02);
}

function round2(n: number) {
  return Math.round(n * 100) / 100;
}

console.log("money-dual tests: ok");
