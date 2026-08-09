"use client";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PasswordInput } from "@/components/ui/password-input";
import { api, ApiError } from "@/lib/api/client";
import { useI18n } from "@/lib/i18n/context";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { FormEvent, Suspense, useState } from "react";
import { toast } from "sonner";

function ResetForm() {
  const { t } = useI18n();
  const router = useRouter();
  const search = useSearchParams();
  const tokenFromLink = search.get("token") ?? "";
  const emailFromLink = search.get("email") ?? "";
  const [email, setEmail] = useState(emailFromLink);
  const [token, setToken] = useState(tokenFromLink);
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [busy, setBusy] = useState(false);
  const hasLinkToken = tokenFromLink.length > 0;

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    if (password !== confirm) {
      toast.error(t("passwordMismatch"));
      return;
    }
    setBusy(true);
    try {
      await api.post(
        "/auth/reset-password",
        { email, token, newPassword: password },
        false
      );
      toast.success(t("passwordResetOk"));
      router.replace("/login");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("resetPasswordTitle")}</CardTitle>
        <CardDescription>{t("resetPasswordHint")}</CardDescription>
      </CardHeader>
      <CardContent>
        <form className="space-y-4" onSubmit={onSubmit}>
          <div className="space-y-2">
            <Label htmlFor="email">{t("email")}</Label>
            <Input
              id="email"
              type="email"
              required
              value={email}
              readOnly={!!emailFromLink}
              onChange={(e) => setEmail(e.target.value)}
            />
          </div>
          {hasLinkToken ? (
            <input type="hidden" name="token" value={token} />
          ) : (
            <div className="space-y-2">
              <Label htmlFor="token">Token</Label>
              <Input
                id="token"
                required
                value={token}
                onChange={(e) => setToken(e.target.value)}
              />
            </div>
          )}
          <div className="space-y-2">
            <Label htmlFor="password">{t("newPassword")}</Label>
            <PasswordInput
              id="password"
              required
              minLength={8}
              autoComplete="new-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              showLabel={t("showPassword")}
              hideLabel={t("hidePassword")}
            />
            <p className="text-xs text-muted">{t("passwordRules")}</p>
          </div>
          <div className="space-y-2">
            <Label htmlFor="confirm">{t("confirmPassword")}</Label>
            <PasswordInput
              id="confirm"
              required
              minLength={8}
              autoComplete="new-password"
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
              showLabel={t("showPassword")}
              hideLabel={t("hidePassword")}
            />
          </div>
          <Button type="submit" className="w-full" disabled={busy}>
            {busy ? t("loading") : t("resetPasswordTitle")}
          </Button>
        </form>
        <p className="mt-4 text-center text-sm text-muted">
          <Link href="/login" className="text-primary hover:underline">
            {t("login")}
          </Link>
        </p>
      </CardContent>
    </Card>
  );
}

/** 15.4.1 — complete reset from email link query params. */
export default function ResetPasswordPage() {
  return (
    <Suspense fallback={null}>
      <ResetForm />
    </Suspense>
  );
}
