"use client";

import { SubscriptionForm } from "@/components/subscriptions/subscription-form";
import { useI18n } from "@/lib/i18n/context";

export default function NewSubscriptionPage() {
  const { t } = useI18n();
  return (
    <div className="mx-auto max-w-xl space-y-4">
      <h1 className="text-2xl font-bold">{t("addSubscription")}</h1>
      <SubscriptionForm mode="create" />
    </div>
  );
}
