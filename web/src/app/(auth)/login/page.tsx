"use client";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PasswordInput } from "@/components/ui/password-input";
import { ApiError } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/context";
import { useI18n } from "@/lib/i18n/context";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { FormEvent, Suspense, useEffect, useState } from "react";
import { toast } from "sonner";

function LoginForm() {
  const { t } = useI18n();
  const { login, isAuthenticated, loading, checkSetup } = useAuth();
  const router = useRouter();
  const search = useSearchParams();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!loading && isAuthenticated) {
      const next = search.get("next") || "/dashboard";
      router.replace(next);
    }
  }, [loading, isAuthenticated, router, search]);

  useEffect(() => {
    void (async () => {
      const status = await checkSetup();
      // Only force setup when no SuperAdmin yet (fresh install).
      // If SuperAdmin exists, allow login so wizard can resume (3S.8).
      if (status && status.isSetupComplete === false && !status.hasSuperAdmin) {
        router.replace("/setup");
      }
    })();
  }, [checkSetup, router]);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    try {
      await login(email, password);
      toast.success(t("login"));
    } catch (err) {
      const msg =
        err instanceof ApiError
          ? err.problem.detail || err.message
          : t("errorGeneric");
      toast.error(msg);
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("signInTitle")}</CardTitle>
        <CardDescription>{t("appName")}</CardDescription>
      </CardHeader>
      <CardContent>
        <form className="space-y-4" onSubmit={onSubmit}>
          <div className="space-y-2">
            <Label htmlFor="email">{t("email")}</Label>
            <Input
              id="email"
              type="email"
              autoComplete="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="password">{t("password")}</Label>
            <PasswordInput
              id="password"
              autoComplete="current-password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              showLabel={t("showPassword")}
              hideLabel={t("hidePassword")}
            />
          </div>
          <Button type="submit" className="w-full" disabled={busy}>
            {busy ? t("loading") : t("login")}
          </Button>
        </form>
        <p className="mt-3 text-center text-sm text-muted">
          <Link href="/forgot-password" className="text-primary hover:underline">
            {t("forgotPassword")}
          </Link>
        </p>
        <p className="mt-2 text-center text-sm text-muted">
          <Link href="/register" className="text-primary hover:underline">
            {t("register")}
          </Link>
        </p>
      </CardContent>
    </Card>
  );
}

export default function LoginPage() {
  return (
    <Suspense>
      <LoginForm />
    </Suspense>
  );
}
