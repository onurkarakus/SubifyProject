"use client";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PasswordInput } from "@/components/ui/password-input";
import { api, ApiError } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/context";
import { useI18n } from "@/lib/i18n/context";
import { useRouter, useSearchParams } from "next/navigation";
import { FormEvent, Suspense, useState } from "react";
import { toast } from "sonner";

function AcceptInviteForm() {
  const { t } = useI18n();
  const { login } = useAuth();
  const router = useRouter();
  const search = useSearchParams();
  const token = search.get("token") || "";
  const [fullName, setFullName] = useState("");
  const [password, setPassword] = useState("");
  const [emailHint, setEmailHint] = useState("");
  const [busy, setBusy] = useState(false);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    if (!token) {
      toast.error("Missing invite token");
      return;
    }
    setBusy(true);
    try {
      const res = await api.post<{ email: string }>(
        "/auth/accept-invite",
        { token, fullName, password },
        false,
      );
      toast.success(t("acceptInvite"));
      if (res?.email) {
        await login(res.email, password);
      } else if (emailHint) {
        await login(emailHint, password);
      } else {
        router.push("/login");
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
      <CardHeader>
        <CardTitle>{t("acceptInvite")}</CardTitle>
      </CardHeader>
      <CardContent>
        <form className="space-y-4" onSubmit={onSubmit}>
          <div className="space-y-2">
            <Label htmlFor="fullName">{t("fullName")}</Label>
            <Input
              id="fullName"
              required
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="password">{t("password")}</Label>
            <PasswordInput
              id="password"
              required
              minLength={8}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              showLabel={t("showPassword")}
              hideLabel={t("hidePassword")}
            />
          </div>
          {/* optional email for post-accept login fallback */}
          <div className="space-y-2">
            <Label htmlFor="email">{t("email")} (optional)</Label>
            <Input
              id="email"
              type="email"
              value={emailHint}
              onChange={(e) => setEmailHint(e.target.value)}
            />
          </div>
          <Button type="submit" className="w-full" disabled={busy || !token}>
            {busy ? t("loading") : t("createAccount")}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

export default function AcceptInvitePage() {
  return (
    <Suspense>
      <AcceptInviteForm />
    </Suspense>
  );
}
