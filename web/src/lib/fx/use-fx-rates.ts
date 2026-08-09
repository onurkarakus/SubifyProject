"use client";

import { api } from "@/lib/api/client";
import type { ExchangeRatesResponse } from "@/lib/api/types";
import {
  toFxRatesSnapshot,
  type FxRatesSnapshot,
} from "@/lib/fx/money-dual";
import { useCallback, useEffect, useState } from "react";

export type UseFxRatesResult = {
  snapshot: FxRatesSnapshot | null;
  isStale: boolean;
  lastUpdated: string | null;
  loading: boolean;
  error: boolean;
  refetch: () => void;
};

/**
 * Loads latest FX snapshot for a base (main) currency — task 16.1.x.
 */
export function useFxRates(baseCurrency: string | null | undefined): UseFxRatesResult {
  const base = (baseCurrency ?? "").trim().toUpperCase() || null;
  const [snapshot, setSnapshot] = useState<FxRatesSnapshot | null>(null);
  const [isStale, setIsStale] = useState(false);
  const [lastUpdated, setLastUpdated] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(false);
  const [tick, setTick] = useState(0);

  const refetch = useCallback(() => setTick((n) => n + 1), []);

  useEffect(() => {
    if (!base) {
      setSnapshot(null);
      setIsStale(false);
      setLastUpdated(null);
      setLoading(false);
      setError(false);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(false);

    void (async () => {
      try {
        const res = await api.get<ExchangeRatesResponse>(
          `/exchange-rates?base=${encodeURIComponent(base)}`,
        );
        if (cancelled) return;
        setSnapshot(toFxRatesSnapshot(res));
        setIsStale(!!res.isStale);
        setLastUpdated(res.lastUpdated ?? null);
      } catch {
        if (cancelled) return;
        setSnapshot(null);
        setError(true);
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [base, tick]);

  return { snapshot, isStale, lastUpdated, loading, error, refetch };
}
