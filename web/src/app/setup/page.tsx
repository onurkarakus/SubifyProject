"use client";

import { WizardSteps, type SetupStepId } from "@/components/setup/wizard-steps";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { AiSettingsFields } from "@/components/ai/ai-settings-fields";
import { InfoTip, LabelWithInfo } from "@/components/ui/info-tip";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PasswordInput } from "@/components/ui/password-input";
import { getAiPreset } from "@/lib/ai/presets";
import { PageLoader } from "@/components/ui/spinner";
import { api, ApiError } from "@/lib/api/client";
import type { CreateSetupAdminResponse, SetupStatus } from "@/lib/api/types";
import { useAuth } from "@/lib/auth/context";
import { tokenStorage, type AuthUser } from "@/lib/auth/storage";
import { useI18n } from "@/lib/i18n/context";
import { THEME_COLOR_PRESETS } from "@/lib/theme/accents";
import { useTheme } from "@/lib/theme/context";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useCallback, useEffect, useState } from "react";
import { toast } from "sonner";

/**
 * 3S.8 — First-run setup wizard.
 * Flow: Welcome → SuperAdmin → Instance → Users(skip) → SMTP(skip) → AI(skip) → Finish
 */
export default function SetupWizardPage() {
  const { t, locale, setLocale } = useI18n();
  const { refreshUser, isAuthenticated, isSuperAdmin, checkSetup } = useAuth();
  const { setTheme, setAccent } = useTheme();
  const router = useRouter();

  const [boot, setBoot] = useState(true);
  const [step, setStep] = useState<SetupStepId>("welcome");
  const [busy, setBusy] = useState(false);
  const [status, setStatus] = useState<SetupStatus | null>(null);

  // admin form
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  // instance form
  const [instanceName, setInstanceName] = useState("Subify");
  const [defaultLocale, setDefaultLocale] = useState<"tr" | "en">(locale);
  const [defaultCurrency, setDefaultCurrency] = useState("TRY");
  const [timeZoneId, setTimeZoneId] = useState("Europe/Istanbul");
  const [allowPublicReg, setAllowPublicReg] = useState(false);
  const [defaultThemeColor, setDefaultThemeColor] = useState("Royal Purple");
  const [defaultDarkTheme, setDefaultDarkTheme] = useState(false);

  // smtp form
  const [smtpEnabled, setSmtpEnabled] = useState(false);
  const [smtpHost, setSmtpHost] = useState("");
  const [smtpPort, setSmtpPort] = useState("587");
  const [smtpUser, setSmtpUser] = useState("");
  const [smtpPassword, setSmtpPassword] = useState("");
  const [smtpFromName, setSmtpFromName] = useState("Subify");
  const [smtpFromEmail, setSmtpFromEmail] = useState("");

  // users form (3S.4.1)
  const [userFullName, setUserFullName] = useState("");
  const [userEmail, setUserEmail] = useState("");
  const [userPassword, setUserPassword] = useState("");
  const [userRole, setUserRole] = useState<"User" | "Admin">("User");
  const [addedUsers, setAddedUsers] = useState<
    { email: string; fullName: string; roles: string[] }[]
  >([]);

  // ai form (BYOK presets)
  const [aiProvider, setAiProvider] = useState("openai");
  const [aiBaseUrl, setAiBaseUrl] = useState(
    () => getAiPreset("openai").baseUrl,
  );
  const [aiModel, setAiModel] = useState("gpt-4o-mini");
  const [aiKey, setAiKey] = useState("");

  const stepLabels: Record<string, string> = {
    setupStepWelcome: t("setupStepWelcome"),
    setupStepAdmin: t("setupStepAdmin"),
    setupStepInstance: t("setupStepInstance"),
    setupStepUsers: t("setupStepUsers"),
    setupStepSmtp: t("setupStepSmtp"),
    setupStepAi: t("setupStepAi"),
    setupStepFinish: t("setupStepFinish"),
  };

  const loadStatus = useCallback(async () => {
    const s = await checkSetup();
    setStatus(s);
    return s;
  }, [checkSetup]);

  useEffect(() => {
    void (async () => {
      const s = await loadStatus();
      if (!s) {
        setBoot(false);
        return;
      }

      // 3S.8.10 — setup complete → login/dashboard
      if (s.isSetupComplete) {
        router.replace(isAuthenticated ? "/dashboard" : "/login");
        return;
      }

      // Prefill instance fields from status
      if (s.instanceName) setInstanceName(s.instanceName);
      if (s.defaultLocale === "tr" || s.defaultLocale === "en") {
        setDefaultLocale(s.defaultLocale);
      }
      if (s.defaultCurrency) setDefaultCurrency(s.defaultCurrency);
      if (s.defaultApplicationThemeColor) {
        setDefaultThemeColor(s.defaultApplicationThemeColor);
      }
      if (typeof s.defaultDarkTheme === "boolean") {
        setDefaultDarkTheme(s.defaultDarkTheme);
      }

      // Resume: SuperAdmin already exists → need auth for remaining steps
      if (s.hasSuperAdmin) {
        if (isAuthenticated && isSuperAdmin) {
          setStep("instance");
        } else {
          setStep("welcome");
        }
      } else {
        setStep("welcome");
      }

      setBoot(false);
    })();
  }, [loadStatus, router, isAuthenticated, isSuperAdmin]);

  function applyAdminSession(data: CreateSetupAdminResponse) {
    if (!data.accessToken || !data.refreshToken) return;
    const user: AuthUser = {
      id: data.userId,
      email: data.email,
      fullName: data.fullName,
      locale: defaultLocale,
      roles: [data.role || "SuperAdmin"],
      isSetupComplete: false,
    };
    tokenStorage.setSession(data.accessToken, data.refreshToken, user);
    refreshUser(user);
  }

  async function createAdmin(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    try {
      const data = await api.post<CreateSetupAdminResponse>(
        "/setup/admin",
        { fullName, email, password },
        false,
      );
      applyAdminSession(data);
      toast.success(t("setupAdminCreated"));
      setStep("instance");
      await loadStatus();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    } finally {
      setBusy(false);
    }
  }

  async function saveInstance(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    try {
      await api.put("/setup/instance", {
        instanceName,
        defaultLocale,
        defaultCurrency,
        timeZoneId,
        allowPublicRegistration: allowPublicReg,
        defaultApplicationThemeColor: defaultThemeColor,
        defaultDarkTheme,
      });
      setLocale(defaultLocale);
      setAccent(defaultThemeColor);
      setTheme(defaultDarkTheme ? "dark" : "light");
      toast.success(t("save"));
      setStep("users");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    } finally {
      setBusy(false);
    }
  }

  async function addSetupUser(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    try {
      const created = await api.post<{
        id: string;
        email: string;
        fullName: string;
        roles: string[];
      }>("/setup/users", {
        email: userEmail,
        fullName: userFullName,
        password: userPassword,
        role: userRole,
      });
      setAddedUsers((prev) => [
        ...prev,
        {
          email: created.email,
          fullName: created.fullName,
          roles: created.roles ?? [userRole],
        },
      ]);
      setUserFullName("");
      setUserEmail("");
      setUserPassword("");
      setUserRole("User");
      toast.success(t("setupUserAdded"));
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    } finally {
      setBusy(false);
    }
  }

  async function saveSmtp(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    try {
      await api.put("/setup/smtp", {
        smtpEnabled,
        smtpHost: smtpHost || null,
        smtpPort: smtpPort ? Number(smtpPort) : null,
        smtpUser: smtpUser || null,
        smtpPassword: smtpPassword || null,
        smtpFromName: smtpFromName || null,
        smtpFromEmail: smtpFromEmail || null,
      });
      toast.success(t("save"));
      setStep("ai");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    } finally {
      setBusy(false);
    }
  }

  function resolveAiKeyForSave(raw: string): string | null {
    const trimmed = raw.trim();
    if (trimmed) return trimmed;
    // Local Ollama often has no real key; store a placeholder so resolver accepts config.
    if (getAiPreset(aiProvider).keyOptional) return "ollama";
    return null;
  }

  async function saveAi(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    try {
      await api.put("/setup/ai", {
        aiProvider: aiProvider || null,
        aiApiKey: resolveAiKeyForSave(aiKey),
        aiModel: aiModel || null,
        aiBaseUrl: aiBaseUrl || null,
      });
      toast.success(t("save"));
      setStep("finish");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    } finally {
      setBusy(false);
    }
  }

  async function testAiDuringSetup() {
    setBusy(true);
    try {
      const key = resolveAiKeyForSave(aiKey);
      if (!key) {
        toast.error(t("aiTestNeedKey"));
        return;
      }
      // Always persist current form before probe (test-ai reads DB only).
      await api.put("/setup/ai", {
        aiProvider: aiProvider || null,
        aiApiKey: key,
        aiModel: aiModel || null,
        aiBaseUrl: aiBaseUrl || null,
      });
      const res = await api.post<{
        model: string;
        latencyMs: number;
        replyPreview: string;
      }>("/admin/settings/test-ai", {});
      toast.success(
        `${t("testAiOk")}: ${res.model} · ${res.latencyMs}ms · ${res.replyPreview}`,
      );
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    } finally {
      setBusy(false);
    }
  }

  async function completeSetup() {
    setBusy(true);
    try {
      await api.post("/setup/complete", {});
      const access = tokenStorage.getAccess();
      const refresh = tokenStorage.getRefresh();
      const user = tokenStorage.getUser();
      if (access && refresh && user) {
        refreshUser({ ...user, isSetupComplete: true });
      }
      toast.success(t("setupCompleteTitle"));
      router.replace("/dashboard");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    } finally {
      setBusy(false);
    }
  }

  if (boot) return <PageLoader />;

  // SuperAdmin exists, not logged in, trying to go past welcome
  const showLoginGate =
    Boolean(status?.hasSuperAdmin) &&
    !status?.isSetupComplete &&
    !(isAuthenticated && isSuperAdmin) &&
    step !== "welcome" &&
    step !== "admin";

  return (
    <div className="min-h-screen bg-background px-4 py-10 text-foreground">
      <div className="mx-auto max-w-xl">
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-primary">{t("appName")}</h1>
            <p className="text-sm text-muted">{t("setupWizardTitle")}</p>
          </div>
          <select
            className="h-9 rounded-lg border border-border bg-surface px-2 text-sm"
            value={locale}
            onChange={(e) => setLocale(e.target.value as "tr" | "en")}
            aria-label={t("locale")}
          >
            <option value="tr">TR</option>
            <option value="en">EN</option>
          </select>
        </div>

        <WizardSteps current={step} labels={stepLabels} />

        {showLoginGate ? (
          <Card>
            <CardHeader>
              <CardTitle>{t("setupResumeTitle")}</CardTitle>
              <CardDescription>{t("setupResumeHint")}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              <Link
                href="/login?next=/setup"
                className="inline-flex h-10 w-full items-center justify-center rounded-lg bg-primary px-4 text-sm font-medium text-white hover:bg-primary/90"
              >
                {t("login")}
              </Link>
              <Button
                type="button"
                variant="secondary"
                className="w-full"
                onClick={() => setStep("welcome")}
              >
                {t("back")}
              </Button>
            </CardContent>
          </Card>
        ) : null}

        {!showLoginGate && step === "welcome" ? (
          <Card>
            <CardHeader>
              <CardTitle>{t("setupWelcomeTitle")}</CardTitle>
              <CardDescription>{t("setupWelcomeBody")}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <ul className="list-inside list-disc space-y-1 text-sm text-muted">
                <li>{t("setupWelcomeBullet1")}</li>
                <li>{t("setupWelcomeBullet2")}</li>
                <li>{t("setupWelcomeBullet3")}</li>
              </ul>
              <Button
                className="w-full"
                onClick={() => {
                  if (status?.hasSuperAdmin) {
                    if (isAuthenticated && isSuperAdmin) {
                      setStep("instance");
                    } else {
                      setStep("instance"); // triggers login gate
                    }
                  } else {
                    setStep("admin");
                  }
                }}
              >
                {t("setupStart")}
              </Button>
              {status?.hasSuperAdmin ? (
                <p className="text-center text-xs text-muted">
                  <Link href="/login?next=/setup" className="text-primary hover:underline">
                    {t("login")}
                  </Link>
                </p>
              ) : null}
            </CardContent>
          </Card>
        ) : null}

        {!showLoginGate && step === "admin" ? (
          <Card>
            <CardHeader>
              <CardTitle>{t("setupStepAdmin")}</CardTitle>
              <CardDescription>{t("setupAdminHint")}</CardDescription>
            </CardHeader>
            <CardContent>
              <form className="space-y-4" onSubmit={createAdmin}>
                <div className="space-y-2">
                  <Label>{t("fullName")}</Label>
                  <Input
                    required
                    value={fullName}
                    onChange={(e) => setFullName(e.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <Label>{t("email")}</Label>
                  <Input
                    type="email"
                    required
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <Label>{t("password")}</Label>
                  <PasswordInput
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
                <div className="flex gap-2">
                  <Button
                    type="button"
                    variant="secondary"
                    onClick={() => setStep("welcome")}
                  >
                    {t("back")}
                  </Button>
                  <Button type="submit" className="flex-1" disabled={busy}>
                    {busy ? t("loading") : t("setupCreateAdmin")}
                  </Button>
                </div>
              </form>
            </CardContent>
          </Card>
        ) : null}

        {!showLoginGate && step === "instance" ? (
          <Card>
            <CardHeader>
              <CardTitle>{t("setupStepInstance")}</CardTitle>
              <CardDescription>{t("setupInstanceHint")}</CardDescription>
            </CardHeader>
            <CardContent>
              <form className="space-y-4" onSubmit={saveInstance}>
                <div className="space-y-2">
                  <Label>{t("instance")}</Label>
                  <Input
                    value={instanceName}
                    onChange={(e) => setInstanceName(e.target.value)}
                  />
                </div>
                <div className="grid gap-3 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label>{t("locale")}</Label>
                    <select
                      className="flex h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm"
                      value={defaultLocale}
                      onChange={(e) =>
                        setDefaultLocale(e.target.value as "tr" | "en")
                      }
                    >
                      <option value="tr">tr</option>
                      <option value="en">en</option>
                    </select>
                  </div>
                  <div className="space-y-2">
                    <Label>{t("mainCurrency")}</Label>
                    <select
                      className="flex h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm"
                      value={defaultCurrency}
                      onChange={(e) => setDefaultCurrency(e.target.value)}
                    >
                      {["TRY", "USD", "EUR", "GBP"].map((c) => (
                        <option key={c} value={c}>
                          {c}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>
                <div className="space-y-2">
                  <LabelWithInfo
                    info={t("timeZoneHint")}
                    infoLabel={t("moreInfo")}
                  >
                    {t("timeZone")}
                  </LabelWithInfo>
                  <Input
                    value={timeZoneId}
                    onChange={(e) => setTimeZoneId(e.target.value)}
                    placeholder="Europe/Istanbul"
                  />
                </div>
                <div className="flex items-center gap-2 text-sm">
                  <input
                    id="setup-allow-public-reg"
                    type="checkbox"
                    checked={allowPublicReg}
                    onChange={(e) => setAllowPublicReg(e.target.checked)}
                  />
                  <label htmlFor="setup-allow-public-reg">
                    {t("allowPublicReg")}
                  </label>
                  <InfoTip label={t("moreInfo")}>{t("allowPublicRegHint")}</InfoTip>
                </div>
                <div className="space-y-2">
                  <LabelWithInfo
                    info={t("defaultThemeColorHint")}
                    infoLabel={t("moreInfo")}
                  >
                    {t("defaultThemeColor")}
                  </LabelWithInfo>
                  <select
                    className="flex h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm"
                    value={defaultThemeColor}
                    onChange={(e) => setDefaultThemeColor(e.target.value)}
                  >
                    {THEME_COLOR_PRESETS.map((c) => (
                      <option key={c} value={c}>
                        {c}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="flex items-center gap-2 text-sm">
                  <input
                    id="setup-default-dark"
                    type="checkbox"
                    checked={defaultDarkTheme}
                    onChange={(e) => setDefaultDarkTheme(e.target.checked)}
                  />
                  <label htmlFor="setup-default-dark">
                    {t("defaultDarkTheme")}
                  </label>
                  <InfoTip label={t("moreInfo")}>
                    {t("defaultDarkThemeHint")}
                  </InfoTip>
                </div>
                <div className="flex gap-2">
                  <Button
                    type="button"
                    variant="secondary"
                    onClick={() => setStep("welcome")}
                  >
                    {t("back")}
                  </Button>
                  <Button type="submit" className="flex-1" disabled={busy}>
                    {busy ? t("loading") : t("continue")}
                  </Button>
                </div>
              </form>
            </CardContent>
          </Card>
        ) : null}

        {!showLoginGate && step === "users" ? (
          <Card>
            <CardHeader>
              <CardTitle>{t("setupStepUsers")}</CardTitle>
              <CardDescription>{t("setupUsersHint")}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <p className="text-sm text-muted">{t("setupUsersSkipNote")}</p>

              <form className="space-y-3 rounded-lg border border-border p-3" onSubmit={addSetupUser}>
                <div className="space-y-2">
                  <Label>{t("fullName")}</Label>
                  <Input
                    required
                    value={userFullName}
                    onChange={(e) => setUserFullName(e.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <Label>{t("email")}</Label>
                  <Input
                    type="email"
                    required
                    value={userEmail}
                    onChange={(e) => setUserEmail(e.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <Label>{t("password")}</Label>
                  <PasswordInput
                    required
                    minLength={8}
                    autoComplete="new-password"
                    value={userPassword}
                    onChange={(e) => setUserPassword(e.target.value)}
                    showLabel={t("showPassword")}
                    hideLabel={t("hidePassword")}
                  />
                  <p className="text-xs text-muted">{t("passwordRules")}</p>
                </div>
                <div className="space-y-2">
                  <Label>{t("role")}</Label>
                  <select
                    className="flex h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm"
                    value={userRole}
                    onChange={(e) =>
                      setUserRole(e.target.value as "User" | "Admin")
                    }
                  >
                    <option value="User">User</option>
                    <option value="Admin">Admin</option>
                  </select>
                </div>
                <Button type="submit" variant="secondary" disabled={busy} className="w-full">
                  {busy ? t("loading") : t("setupAddUser")}
                </Button>
              </form>

              {addedUsers.length > 0 ? (
                <ul className="space-y-1 text-sm">
                  {addedUsers.map((u) => (
                    <li
                      key={u.email}
                      className="rounded-lg border border-border px-3 py-2"
                    >
                      <span className="font-medium">{u.fullName}</span>
                      <span className="text-muted"> · {u.email}</span>
                      <span className="text-muted">
                        {" "}
                        · {(u.roles ?? []).join(", ")}
                      </span>
                    </li>
                  ))}
                </ul>
              ) : null}

              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => setStep("instance")}
                >
                  {t("back")}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setStep("smtp")}
                >
                  {addedUsers.length > 0 ? t("continue") : t("skip")}
                </Button>
                {addedUsers.length > 0 ? (
                  <Button
                    type="button"
                    className="flex-1"
                    onClick={() => setStep("smtp")}
                  >
                    {t("continue")}
                  </Button>
                ) : null}
              </div>
            </CardContent>
          </Card>
        ) : null}

        {!showLoginGate && step === "smtp" ? (
          <Card>
            <CardHeader>
              <CardTitle>{t("setupStepSmtp")}</CardTitle>
              <CardDescription>{t("setupSmtpHint")}</CardDescription>
            </CardHeader>
            <CardContent>
              <form className="space-y-4" onSubmit={saveSmtp}>
                <div className="flex items-center gap-2 text-sm">
                  <input
                    id="setup-smtp-enabled"
                    type="checkbox"
                    checked={smtpEnabled}
                    onChange={(e) => setSmtpEnabled(e.target.checked)}
                  />
                  <label htmlFor="setup-smtp-enabled">{t("smtpEnabled")}</label>
                  <InfoTip label={t("moreInfo")}>{t("smtpEnabledHint")}</InfoTip>
                </div>
                <div className="space-y-2">
                  <Label>{t("smtpHost")}</Label>
                  <Input
                    value={smtpHost}
                    onChange={(e) => setSmtpHost(e.target.value)}
                    placeholder="smtp.example.com"
                  />
                </div>
                <div className="grid gap-3 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label>{t("smtpPort")}</Label>
                    <Input
                      value={smtpPort}
                      onChange={(e) => setSmtpPort(e.target.value)}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label>{t("smtpUser")}</Label>
                    <Input
                      value={smtpUser}
                      onChange={(e) => setSmtpUser(e.target.value)}
                    />
                  </div>
                </div>
                <div className="space-y-2">
                  <Label>{t("password")}</Label>
                  <PasswordInput
                    value={smtpPassword}
                    onChange={(e) => setSmtpPassword(e.target.value)}
                    showLabel={t("showPassword")}
                    hideLabel={t("hidePassword")}
                  />
                </div>
                <div className="grid gap-3 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label>{t("smtpFromName")}</Label>
                    <Input
                      value={smtpFromName}
                      onChange={(e) => setSmtpFromName(e.target.value)}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label>{t("smtpFromEmail")}</Label>
                    <Input
                      type="email"
                      value={smtpFromEmail}
                      onChange={(e) => setSmtpFromEmail(e.target.value)}
                    />
                  </div>
                </div>
                <div className="flex flex-wrap gap-2">
                  <Button
                    type="button"
                    variant="secondary"
                    onClick={() => setStep("users")}
                  >
                    {t("back")}
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => setStep("ai")}
                  >
                    {t("skip")}
                  </Button>
                  <Button type="submit" className="flex-1" disabled={busy}>
                    {busy ? t("loading") : t("saveAndContinue")}
                  </Button>
                </div>
              </form>
            </CardContent>
          </Card>
        ) : null}

        {!showLoginGate && step === "ai" ? (
          <Card>
            <CardHeader>
              <CardTitle>{t("setupStepAi")}</CardTitle>
              <CardDescription>{t("setupAiHint")}</CardDescription>
            </CardHeader>
            <CardContent>
              <form className="space-y-4" onSubmit={saveAi}>
                <AiSettingsFields
                  value={{
                    provider: aiProvider,
                    baseUrl: aiBaseUrl,
                    model: aiModel,
                    apiKey: aiKey,
                  }}
                  onChange={(next) => {
                    setAiProvider(next.provider);
                    setAiBaseUrl(next.baseUrl);
                    setAiModel(next.model);
                    setAiKey(next.apiKey);
                  }}
                />
                <div className="flex flex-wrap gap-2">
                  <Button
                    type="button"
                    variant="secondary"
                    onClick={() => setStep("smtp")}
                  >
                    {t("back")}
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => setStep("finish")}
                  >
                    {t("skip")}
                  </Button>
                  <Button
                    type="button"
                    variant="secondary"
                    disabled={busy || !aiKey}
                    onClick={() => void testAiDuringSetup()}
                  >
                    {t("testAi")}
                  </Button>
                  <Button type="submit" className="flex-1" disabled={busy}>
                    {busy ? t("loading") : t("saveAndContinue")}
                  </Button>
                </div>
              </form>
            </CardContent>
          </Card>
        ) : null}

        {!showLoginGate && step === "finish" ? (
          <Card>
            <CardHeader>
              <CardTitle>{t("setupCompleteTitle")}</CardTitle>
              <CardDescription>{t("setupCompleteHint")}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <ul className="list-inside list-disc space-y-1 text-sm text-muted">
                <li>{t("setupCompleteBullet1")}</li>
                <li>{t("setupCompleteBullet2")}</li>
              </ul>
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => setStep("ai")}
                >
                  {t("back")}
                </Button>
                <Button
                  type="button"
                  className="flex-1"
                  disabled={busy}
                  onClick={() => void completeSetup()}
                >
                  {busy ? t("loading") : t("setupFinish")}
                </Button>
              </div>
            </CardContent>
          </Card>
        ) : null}
      </div>
    </div>
  );
}
