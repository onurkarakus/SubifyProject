import type { SubscriptionItem } from "@/lib/api/types";
import { normalizeBillingCycle } from "@/lib/utils";

export type ImportRowDraft = {
  line: number;
  name: string;
  price: number;
  currency: string;
  billingCycle: "monthly" | "yearly";
  sharedWithCount: number;
  nextRenewalDate: string;
  notes: string | null;
  errors: string[];
};

export type ImportParseResult = {
  drafts: ImportRowDraft[];
  headerOk: boolean;
  rawCount: number;
};

function parseCsvLine(line: string): string[] {
  const cells: string[] = [];
  let cur = "";
  let inQuotes = false;
  for (let i = 0; i < line.length; i++) {
    const ch = line[i]!;
    if (inQuotes) {
      if (ch === '"') {
        if (line[i + 1] === '"') {
          cur += '"';
          i++;
        } else {
          inQuotes = false;
        }
      } else {
        cur += ch;
      }
    } else if (ch === '"') {
      inQuotes = true;
    } else if (ch === ",") {
      cells.push(cur);
      cur = "";
    } else {
      cur += ch;
    }
  }
  cells.push(cur);
  return cells.map((c) => c.trim());
}

function normalizeHeader(h: string): string {
  return h
    .replace(/^\uFEFF/, "")
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]/g, "");
}

/** Map flexible export headers → field keys. */
const HEADER_MAP: Record<string, string> = {
  name: "name",
  category: "category",
  billingcycle: "billingCycle",
  price: "price",
  priceoriginal: "price",
  pricecurrency: "currency",
  currency: "currency",
  sharedwithcount: "sharedWithCount",
  usershare: "userShare",
  monthlyequivalentshare: "monthly",
  monthlyequivalentoriginal: "monthly",
  nextrenewaldate: "nextRenewalDate",
  notes: "notes",
  archived: "archived",
};

function isValidDate(s: string): boolean {
  return /^\d{4}-\d{2}-\d{2}$/.test(s) && !Number.isNaN(Date.parse(s));
}

/**
 * Parse subscription CSV (export-compatible) for dry-run import (16.3.2).
 */
export function parseSubscriptionCsv(text: string): ImportParseResult {
  const lines = text
    .replace(/^\uFEFF/, "")
    .split(/\r?\n/)
    .filter((l) => l.trim().length > 0);

  if (lines.length < 2) {
    return { drafts: [], headerOk: false, rawCount: 0 };
  }

  const headers = parseCsvLine(lines[0]!).map(normalizeHeader);
  const indices: Record<string, number> = {};
  headers.forEach((h, i) => {
    const key = HEADER_MAP[h];
    if (key && indices[key] == null) indices[key] = i;
  });

  const headerOk = indices.name != null && (indices.price != null || indices.monthly != null);

  const drafts: ImportRowDraft[] = [];
  for (let li = 1; li < lines.length; li++) {
    const cells = parseCsvLine(lines[li]!);
    const get = (key: string) => {
      const idx = indices[key];
      return idx == null ? "" : (cells[idx] ?? "").trim();
    };

    const errors: string[] = [];
    const name = get("name");
    if (!name) errors.push("name");

    const priceRaw = get("price") || get("monthly");
    const price = Number(priceRaw);
    if (!(price > 0)) errors.push("price");

    let currency = (get("currency") || "TRY").toUpperCase();
    if (!/^[A-Z]{3}$/.test(currency)) {
      currency = "TRY";
      errors.push("currency");
    }

    const billingCycle = normalizeBillingCycle(get("billingCycle") || "monthly");
    const shared = Math.max(1, Number(get("sharedWithCount") || 1) || 1);

    let nextRenewalDate = get("nextRenewalDate");
    if (!isValidDate(nextRenewalDate)) {
      // default +30 days
      const d = new Date();
      d.setUTCDate(d.getUTCDate() + 30);
      nextRenewalDate = d.toISOString().slice(0, 10);
      if (get("nextRenewalDate")) errors.push("nextRenewalDate");
    }

    const archived = /^(true|1|yes)$/i.test(get("archived"));
    drafts.push({
      line: li + 1,
      name,
      price,
      currency,
      billingCycle,
      sharedWithCount: shared,
      nextRenewalDate,
      notes: get("notes") || null,
      // archived rows skipped on import (dry-run shows reason)
      errors: archived ? ["archived"] : errors,
    });
  }

  return { drafts, headerOk, rawCount: drafts.length };
}

export function importDraftIsValid(d: ImportRowDraft): boolean {
  return d.errors.length === 0 && d.name.length > 0 && d.price > 0;
}

export type CreateBodyFromDraft = {
  name: string;
  price: number;
  currency: string;
  billingCycle: "monthly" | "yearly";
  sharedWithCount: number;
  nextRenewalDate: string;
  notes: string | null;
};

export function draftToCreateBody(d: ImportRowDraft): CreateBodyFromDraft {
  return {
    name: d.name,
    price: d.price,
    currency: d.currency,
    billingCycle: d.billingCycle,
    sharedWithCount: d.sharedWithCount,
    nextRenewalDate: d.nextRenewalDate,
    notes: d.notes,
  };
}

/** Template CSV for download (import template). */
export function importTemplateCsv(): string {
  const headers = [
    "name",
    "price",
    "priceCurrency",
    "billingCycle",
    "sharedWithCount",
    "nextRenewalDate",
    "notes",
  ];
  const sample = [
    "Netflix",
    "199.99",
    "TRY",
    "monthly",
    "1",
    new Date(Date.now() + 14 * 86400000).toISOString().slice(0, 10),
    "",
  ];
  return `\uFEFF${headers.join(",")}\n${sample.join(",")}\n`;
}

export function exportCompatibleFromItems(items: SubscriptionItem[]): string {
  // lightweight re-export for symmetry if needed
  void items;
  return importTemplateCsv();
}
