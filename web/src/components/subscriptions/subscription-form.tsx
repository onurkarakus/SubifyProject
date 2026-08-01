"use client";

import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { api, ApiError } from "@/lib/api/client";
import type { CategoryItem, ProviderItem, SubscriptionItem } from "@/lib/api/types";
import { useI18n } from "@/lib/i18n/context";
import { normalizeBillingCycle } from "@/lib/utils";
import { useRouter } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";
import { toast } from "sonner";

type Props = {
  mode: "create" | "edit";
  initial?: SubscriptionItem;
};

export function SubscriptionForm({ mode, initial }: Props) {
  const { t } = useI18n();
  const router = useRouter();
  const [categories, setCategories] = useState<CategoryItem[]>([]);
  const [providers, setProviders] = useState<ProviderItem[]>([]);
  const [busy, setBusy] = useState(false);

  const [name, setName] = useState(initial?.name ?? "");
  const [price, setPrice] = useState(String(initial?.price ?? ""));
  const [currency, setCurrency] = useState(initial?.currency ?? "TRY");
  const [billingCycle, setBillingCycle] = useState<"monthly" | "yearly">(
    normalizeBillingCycle(initial?.billingCycle),
  );
  const [sharedWithCount, setSharedWithCount] = useState(
    String(initial?.sharedWithCount ?? 1),
  );
  const [nextRenewalDate, setNextRenewalDate] = useState(
    initial?.nextRenewalDate?.slice(0, 10) ??
      new Date(Date.now() + 7 * 86400000).toISOString().slice(0, 10),
  );
  const [categoryId, setCategoryId] = useState(initial?.categoryId ?? "");
  const [providerId, setProviderId] = useState(initial?.providerId ?? "");
  const [notes, setNotes] = useState(initial?.notes ?? "");

  useEffect(() => {
    void (async () => {
      try {
        const [cats, provs] = await Promise.all([
          api.get<{ data: CategoryItem[] }>("/categories"),
          api.get<{ data: ProviderItem[] }>("/providers"),
        ]);
        setCategories(cats.data ?? []);
        setProviders(provs.data ?? []);
      } catch {
        // optional catalogs
      }
    })();
  }, []);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    const body = {
      name,
      price: Number(price),
      currency,
      billingCycle,
      sharedWithCount: Number(sharedWithCount) || 1,
      nextRenewalDate,
      categoryId: categoryId || null,
      providerId: providerId || null,
      notes: notes || null,
    };
    try {
      if (mode === "create") {
        const created = await api.post<{ id: string }>("/subscriptions", body);
        toast.success(t("save"));
        router.push(`/subscriptions/${created.id}`);
      } else if (initial) {
        await api.put(`/subscriptions/${initial.id}`, body);
        toast.success(t("save"));
        router.push("/subscriptions");
      }
    } catch (err) {
      toast.error(
        err instanceof ApiError
          ? err.problem.detail || err.message
          : t("errorGeneric"),
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card>
      <CardContent className="p-5">
        <form className="space-y-4" onSubmit={onSubmit}>
          <div className="space-y-2">
            <Label>{t("name")}</Label>
            <Input required value={name} onChange={(e) => setName(e.target.value)} />
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label>{t("price")}</Label>
              <Input
                required
                type="number"
                step="0.01"
                min="0.01"
                value={price}
                onChange={(e) => setPrice(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label>{t("currency")}</Label>
              <select
                className="flex h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm"
                value={currency}
                onChange={(e) => setCurrency(e.target.value)}
              >
                {["TRY", "USD", "EUR", "GBP"].map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label>{t("billingCycle")}</Label>
              <select
                className="flex h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm"
                value={billingCycle}
                onChange={(e) =>
                  setBillingCycle(normalizeBillingCycle(e.target.value))
                }
              >
                <option value="monthly">{t("monthly")}</option>
                <option value="yearly">{t("yearly")}</option>
              </select>
            </div>
            <div className="space-y-2">
              <Label>{t("sharedWith")}</Label>
              <Input
                type="number"
                min={1}
                value={sharedWithCount}
                onChange={(e) => setSharedWithCount(e.target.value)}
              />
            </div>
          </div>
          <div className="space-y-2">
            <Label>{t("nextRenewal")}</Label>
            <Input
              type="date"
              required
              value={nextRenewalDate}
              onChange={(e) => setNextRenewalDate(e.target.value)}
            />
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label>{t("category")}</Label>
              <select
                className="flex h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm"
                value={categoryId}
                onChange={(e) => setCategoryId(e.target.value)}
              >
                <option value="">—</option>
                {categories.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <Label>{t("provider")}</Label>
              <select
                className="flex h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm"
                value={providerId}
                onChange={(e) => setProviderId(e.target.value)}
              >
                <option value="">—</option>
                {providers.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div className="space-y-2">
            <Label>{t("notes")}</Label>
            <Input value={notes} onChange={(e) => setNotes(e.target.value)} />
          </div>
          <div className="flex gap-2">
            <Button type="submit" disabled={busy}>
              {busy ? t("loading") : t("save")}
            </Button>
            <Button
              type="button"
              variant="secondary"
              onClick={() => router.push("/subscriptions")}
            >
              {t("cancel")}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
