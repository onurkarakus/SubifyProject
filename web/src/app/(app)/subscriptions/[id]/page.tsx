"use client";

import { SubscriptionForm } from "@/components/subscriptions/subscription-form";
import { PageLoader } from "@/components/ui/spinner";
import { api, ApiError } from "@/lib/api/client";
import type { SubscriptionItem } from "@/lib/api/types";
import { useI18n } from "@/lib/i18n/context";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { toast } from "sonner";

export default function EditSubscriptionPage() {
  const { t } = useI18n();
  const params = useParams();
  const id = params.id as string;
  const [item, setItem] = useState<SubscriptionItem | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    void (async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await api.get<SubscriptionItem>(`/subscriptions/${id}`);
        if (!cancelled) setItem(data);
      } catch (e) {
        const msg = e instanceof ApiError ? e.message : t("errorGeneric");
        if (!cancelled) {
          setError(msg);
          toast.error(msg);
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [id, t]);

  if (loading) return <PageLoader />;

  if (error || !item) {
    return (
      <div className="mx-auto max-w-xl space-y-4">
        <h1 className="text-2xl font-bold">{t("errorGeneric")}</h1>
        <p className="text-sm text-muted">{error ?? t("empty")}</p>
        <Link
          href="/subscriptions"
          className="inline-flex h-10 items-center rounded-lg border border-border px-4 text-sm font-medium hover:bg-muted/40"
        >
          {t("subscriptions")}
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-xl space-y-4">
      <h1 className="text-2xl font-bold">
        {t("edit")}: {item.name}
      </h1>
      <SubscriptionForm mode="edit" initial={item} />
    </div>
  );
}
