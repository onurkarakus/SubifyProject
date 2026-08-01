"use client";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PasswordInput } from "@/components/ui/password-input";
import { api, ApiError } from "@/lib/api/client";
import type { SetupStatus } from "@/lib/api/types";
import { useAuth } from "@/lib/auth/context";
import { useI18n } from "@/lib/i18n/context";
import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { toast } from "sonner";

export default function RegisterPage() {
  const { t } = useI18n();
  const { register } = useAuth();
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [allowed, setAllowed] = useState<boolean | null>(null);

  useEffect(() => {
    void (async () => {
      try {
        const status = await api.get<SetupStatus>("/setup/status", false);
        if (!status.isSetupComplete) {
          window.location.href = "/setup";
          return;
        }
        setAllowed(status.allowPublicRegistration !== false);
      } catch {
        setAllowed(true);
      }
    })();
  }, []);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    try {
      await register(fullName, email, password);
      toast.success(t("createAccount"));
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

  if (allowed === null) {
    return <p className="text-center text-muted">{t("loading")}</p>;
  }

  if (!allowed) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>{t("register")}</CardTitle>
          <CardDescription>{t("noPublicRegister")}</CardDescription>
        </CardHeader>
        <CardContent>
          <Link href="/login" className="text-primary hover:underline">
            {t("login")}
          </Link>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("createAccount")}</CardTitle>
        <CardDescription>{t("tagline")}</CardDescription>
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
            <Label htmlFor="email">{t("email")}</Label>
            <Input
              id="email"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="password">{t("password")}</Label>
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
          <Button type="submit" className="w-full" disabled={busy}>
            {busy ? t("loading") : t("register")}
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
