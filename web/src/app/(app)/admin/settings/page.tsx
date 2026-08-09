"use client";

import { AiSettingsFields } from "@/components/ai/ai-settings-fields";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { InfoTip, LabelWithInfo } from "@/components/ui/info-tip";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PasswordInput } from "@/components/ui/password-input";
import { PageLoader } from "@/components/ui/spinner";
import { Tabs } from "@/components/ui/tabs";
import { getAiPreset } from "@/lib/ai/presets";
import { api, ApiError } from "@/lib/api/client";
import type {
  ExchangeRatesResponse,
  ImportAdminProvidersResponse,
  ImportProviderItem,
  RunExchangeRateSyncResponse,
  SystemSettingsResponse,
} from "@/lib/api/types";
import { useAuth } from "@/lib/auth/context";
import { isFxSnapshotStale } from "@/lib/fx/fx-health";
import { useI18n } from "@/lib/i18n/context";
import { THEME_COLOR_PRESETS } from "@/lib/theme/accents";
import { cn } from "@/lib/utils";
import {
  Activity,
  CheckCircle2,
  Copy,
  Database,
  Package,
  RefreshCw,
  XCircle,
} from "lucide-react";
import { useRouter, useSearchParams } from "next/navigation";
import {
  FormEvent,
  Suspense,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { toast } from "sonner";

const BACKUP_CMD = `./docker/scripts/backup-postgres.sh`;
const BACKUP_CRON =
  `15 3 * * * cd /path/to/SubifyProject && ./docker/scripts/backup-postgres.sh >> /var/log/subify-backup.log 2>&1`;

type SettingsTab = "instance" | "ai" | "smtp" | "ops";

function parseTab(raw: string | null): SettingsTab {
  if (raw === "ai" || raw === "smtp" || raw === "instance" || raw === "ops") {
    return raw;
  }
  return "instance";
}

function AdminSettingsInner() {
  const { t, locale } = useI18n();
  const { isSuperAdmin, loading: authLoading } = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const tab = parseTab(searchParams.get("tab"));

  const [settings, setSettings] = useState<SystemSettingsResponse | null>(null);
  const [aiKey, setAiKey] = useState("");
  const [aiBaseUrl, setAiBaseUrl] = useState("");
  const [smtpPassword, setSmtpPassword] = useState("");
  const [testSmtpTo, setTestSmtpTo] = useState("");
  const [busy, setBusy] = useState(false);
  const [fx, setFx] = useState<ExchangeRatesResponse | null>(null);
  const [fxLoading, setFxLoading] = useState(false);
  const [fxError, setFxError] = useState(false);
  const [providerJson, setProviderJson] = useState("");
  const [providerUpdateExisting, setProviderUpdateExisting] = useState(false);
  const [providerImportBusy, setProviderImportBusy] = useState(false);
  const [providerImportResult, setProviderImportResult] =
    useState<ImportAdminProvidersResponse | null>(null);
  const providerFileRef = useRef<HTMLInputElement>(null);

  const tabs = useMemo(
    () =>
      [
        { id: "instance" as const, label: t("instance") },
        { id: "ai" as const, label: t("aiSettings") },
        { id: "smtp" as const, label: t("smtp") },
        { id: "ops" as const, label: t("adminOpsTab") },
      ] as const,
    [t],
  );

  const loadFx = useCallback(async (base: string) => {
    setFxLoading(true);
    setFxError(false);
    try {
      const res = await api.get<ExchangeRatesResponse>(
        `/exchange-rates?base=${encodeURIComponent(base)}`,
      );
      setFx(res);
    } catch {
      setFx(null);
      setFxError(true);
    } finally {
      setFxLoading(false);
    }
  }, []);

  /** SuperAdmin: live provider fetch (not just re-read cached GET). */
  const forceSyncFx = useCallback(async (base: string) => {
    setFxLoading(true);
    setFxError(false);
    try {
      const res = await api.post<RunExchangeRateSyncResponse>(
        `/admin/jobs/exchange-rates/sync?base=${encodeURIComponent(base)}`,
        {},
      );
      setFx({
        base: res.base,
        rates: res.rates ?? {},
        lastUpdated: res.fetchedAt ?? null,
        source: res.source ?? null,
        isStale: res.isStale,
        fromFallback: res.usedExistingFallback,
        message: res.message ?? null,
      });
      if (res.succeeded) {
        toast.success(
          t("adminOpsFxSyncOk").replace(
            "{n}",
            String(res.ratesPersisted ?? 0),
          ),
        );
      } else if (res.usedExistingFallback) {
        toast.error(
          res.errorMessage || res.message || t("adminOpsFxSyncFallback"),
        );
      } else {
        toast.error(
          res.errorMessage || res.message || t("adminOpsFxSyncFail"),
        );
      }
    } catch (e) {
      setFxError(true);
      toast.error(
        e instanceof ApiError
          ? e.problem.detail || e.message
          : t("adminOpsFxSyncFail"),
      );
    } finally {
      setFxLoading(false);
    }
  }, [t]);

  const setTab = useCallback(
    (next: SettingsTab) => {
      const q = new URLSearchParams(searchParams.toString());
      q.set("tab", next);
      router.replace(`/admin/settings?${q.toString()}`, { scroll: false });
    },
    [router, searchParams],
  );

  useEffect(() => {
    if (!authLoading && !isSuperAdmin) {
      router.replace("/dashboard");
      return;
    }
    if (!isSuperAdmin) return;
    void (async () => {
      try {
        const s = await api.get<SystemSettingsResponse>("/admin/settings");
        setSettings(s);
        const preset = getAiPreset(s.ai.provider);
        setAiBaseUrl(s.ai.baseUrl?.trim() || preset.baseUrl);
        const base = (s.instance.defaultCurrency || "TRY").toUpperCase();
        await loadFx(base);
      } catch (e) {
        toast.error(e instanceof ApiError ? e.message : t("errorGeneric"));
      }
    })();
  }, [authLoading, isSuperAdmin, router, t, loadFx]);

  async function applyUpdate(body: Record<string, unknown>) {
    const updated = await api.put<SystemSettingsResponse>(
      "/admin/settings",
      body,
    );
    setSettings(updated);
    setAiBaseUrl(
      updated.ai.baseUrl?.trim() || getAiPreset(updated.ai.provider).baseUrl,
    );
    return updated;
  }

  async function testSmtp() {
    setBusy(true);
    try {
      const body =
        testSmtpTo.trim().length > 0 ? { toEmail: testSmtpTo.trim() } : {};
      await api.post("/admin/settings/test-smtp", body);
      toast.success(t("testSmtpOk"));
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    } finally {
      setBusy(false);
    }
  }

  async function testAi() {
    if (!settings) return;
    setBusy(true);
    try {
      const body: Record<string, unknown> = {
        aiProvider: settings.ai.provider,
        aiModel: settings.ai.model,
        aiBaseUrl: aiBaseUrl || null,
      };
      if (aiKey.trim() !== "") {
        body.aiApiKey = aiKey.trim();
      } else if (
        !settings.ai.hasApiKey &&
        getAiPreset(settings.ai.provider).keyOptional
      ) {
        body.aiApiKey = "ollama";
      }

      if (!settings.ai.hasApiKey && !body.aiApiKey) {
        toast.error(t("aiTestNeedKey"));
        return;
      }

      await applyUpdate(body);
      setAiKey("");

      const res = await api.post<{
        ok: boolean;
        model: string;
        provider?: string | null;
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

  /** Save only the active tab (partial PUT). */
  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    if (!settings) return;
    setBusy(true);
    try {
      let body: Record<string, unknown> = {};

      if (tab === "instance") {
        body = {
          instanceName: settings.instance.instanceName,
          defaultLocale: settings.instance.defaultLocale,
          defaultCurrency: settings.instance.defaultCurrency,
          timeZoneId: settings.instance.timeZoneId,
          allowPublicRegistration: settings.instance.allowPublicRegistration,
          defaultApplicationThemeColor:
            settings.instance.defaultApplicationThemeColor ?? "Royal Purple",
          defaultDarkTheme: settings.instance.defaultDarkTheme ?? false,
        };
      } else if (tab === "ai") {
        body = {
          aiProvider: settings.ai.provider,
          aiModel: settings.ai.model,
          aiBaseUrl: aiBaseUrl || null,
        };
        if (aiKey !== "") {
          body.aiApiKey = aiKey;
        } else if (
          !settings.ai.hasApiKey &&
          getAiPreset(settings.ai.provider).keyOptional
        ) {
          body.aiApiKey = "ollama";
        }
      } else {
        body = {
          smtpEnabled: settings.smtp.enabled,
          smtpHost: settings.smtp.host,
          smtpPort: settings.smtp.port,
          smtpUser: settings.smtp.user,
          smtpFromName: settings.smtp.fromName,
          smtpFromEmail: settings.smtp.fromEmail,
        };
        if (smtpPassword !== "") body.smtpPassword = smtpPassword;
      }

      await applyUpdate(body);
      if (tab === "ai") setAiKey("");
      if (tab === "smtp") setSmtpPassword("");
      toast.success(t("save"));
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    } finally {
      setBusy(false);
    }
  }

  if (authLoading || !settings) return <PageLoader />;

  const defaultCurrency = (
    settings.instance.defaultCurrency || "TRY"
  ).toUpperCase();
  const fxStale = isFxSnapshotStale(fx?.lastUpdated, fx?.isStale);
  const smtpLooksReady =
    settings.smtp.enabled &&
    !!settings.smtp.host &&
    !!settings.smtp.fromEmail;

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div>
        <h1 className="page-title">{t("adminSettings")}</h1>
        <p className="mt-1 text-sm text-muted">{t("adminSettingsHint")}</p>
      </div>

      <Tabs
        tabs={[...tabs]}
        value={tab}
        onChange={setTab}
        aria-label={t("adminSettings")}
      />

      {/* —— Ops / health (read-only; not part of settings form) —— */}
      {tab === "ops" ? (
        <div className="space-y-4">
          <p className="text-sm text-muted">{t("adminOpsHint")}</p>

          <Card>
            <CardHeader className="flex-row items-center justify-between space-y-0">
              <CardTitle className="flex items-center gap-2 text-base">
                <Activity className="h-4 w-4 text-primary" />
                {t("adminOpsFxTitle")}
              </CardTitle>
              <Button
                type="button"
                variant="secondary"
                size="sm"
                disabled={fxLoading}
                onClick={() => void forceSyncFx(defaultCurrency)}
              >
                <RefreshCw
                  className={cn("h-3.5 w-3.5", fxLoading && "animate-spin")}
                />
                {t("adminOpsFxRefresh")}
              </Button>
            </CardHeader>
            <CardContent className="space-y-2 text-sm">
              <p className="text-xs text-muted">{t("adminOpsFxRefreshHint")}</p>
              <p className="text-muted">
                {t("mainCurrency")}:{" "}
                <span className="font-medium text-foreground">
                  {defaultCurrency}
                </span>
                {fx?.source ? (
                  <span className="text-muted"> · {fx.source}</span>
                ) : null}
              </p>
              {fxLoading && !fx ? (
                <p className="text-muted">{t("loading")}</p>
              ) : fxError || !fx ? (
                <p className="text-warning">{t("adminOpsFxEmpty")}</p>
              ) : (
                <>
                  <p>
                    {t("fxSidebarAsOf")}:{" "}
                    <span className="font-medium tabular-nums">
                      {fx.lastUpdated
                        ? new Intl.DateTimeFormat(
                            locale === "en" ? "en-GB" : "tr-TR",
                            {
                              year: "numeric",
                              month: "short",
                              day: "numeric",
                              hour: "2-digit",
                              minute: "2-digit",
                            },
                          ).format(new Date(fx.lastUpdated))
                        : "—"}
                    </span>
                  </p>
                  <p
                    className={
                      fxStale ? "font-medium text-warning" : "text-success"
                    }
                  >
                    {fxStale ? t("adminOpsFxStale") : t("adminOpsFxOk")}
                  </p>
                  {fx.message ? (
                    <p className="text-xs text-muted">{fx.message}</p>
                  ) : null}
                </>
              )}
              <p className="text-xs text-muted">{t("adminOpsFxEnv")}</p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">{t("status")}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm">
              <div className="flex items-center gap-2">
                {settings.ai.hasApiKey ? (
                  <CheckCircle2 className="h-4 w-4 text-success" />
                ) : (
                  <XCircle className="h-4 w-4 text-muted" />
                )}
                <span>
                  {settings.ai.hasApiKey
                    ? t("adminOpsAiOk")
                    : t("adminOpsAiMissing")}
                </span>
              </div>
              <div className="flex items-center gap-2">
                {smtpLooksReady ? (
                  <CheckCircle2 className="h-4 w-4 text-success" />
                ) : (
                  <XCircle className="h-4 w-4 text-muted" />
                )}
                <span>
                  {smtpLooksReady
                    ? t("adminOpsSmtpOk")
                    : t("adminOpsSmtpMissing")}
                </span>
              </div>
              <p className="pt-2 text-xs text-muted">
                {t("adminOpsProfileNote")}
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-base">
                <Database className="h-4 w-4 text-primary" />
                {t("adminOpsBackupTitle")}
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <p className="text-muted">{t("adminOpsBackupHint")}</p>
              <div className="space-y-1">
                <div className="flex items-center justify-between gap-2">
                  <Label className="text-xs text-muted">
                    {t("adminOpsBackupCmd")}
                  </Label>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="h-7 px-2 text-xs"
                    onClick={() => {
                      void navigator.clipboard.writeText(BACKUP_CMD);
                      toast.success(t("adminOpsBackupCopied"));
                    }}
                  >
                    <Copy className="h-3 w-3" />
                    {t("adminOpsBackupCopy")}
                  </Button>
                </div>
                <pre className="overflow-x-auto rounded-lg border border-border bg-muted/30 px-3 py-2 text-[11px] leading-relaxed">
                  {BACKUP_CMD}
                </pre>
              </div>
              <div className="space-y-1">
                <div className="flex items-center justify-between gap-2">
                  <Label className="text-xs text-muted">
                    {t("adminOpsBackupCron")}
                  </Label>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="h-7 px-2 text-xs"
                    onClick={() => {
                      void navigator.clipboard.writeText(BACKUP_CRON);
                      toast.success(t("adminOpsBackupCopied"));
                    }}
                  >
                    <Copy className="h-3 w-3" />
                    {t("adminOpsBackupCopy")}
                  </Button>
                </div>
                <pre className="overflow-x-auto rounded-lg border border-border bg-muted/30 px-3 py-2 text-[11px] leading-relaxed">
                  {BACKUP_CRON}
                </pre>
              </div>
              <p className="text-xs text-muted">{t("adminOpsBackupDocs")}</p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-base">
                <Package className="h-4 w-4 text-primary" />
                {t("adminOpsProvidersTitle")}
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <p className="text-muted">{t("adminOpsProvidersHint")}</p>
              <p className="text-xs text-muted">{t("adminOpsProvidersSample")}</p>
              <input
                ref={providerFileRef}
                type="file"
                accept="application/json,.json"
                className="hidden"
                onChange={(e) => {
                  const f = e.target.files?.[0];
                  if (!f) return;
                  void f.text().then((text) => setProviderJson(text));
                  e.target.value = "";
                }}
              />
              <div className="flex flex-wrap gap-2">
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  onClick={() => providerFileRef.current?.click()}
                >
                  JSON
                </Button>
                <label className="flex items-center gap-2 text-xs text-muted">
                  <input
                    type="checkbox"
                    checked={providerUpdateExisting}
                    onChange={(e) =>
                      setProviderUpdateExisting(e.target.checked)
                    }
                  />
                  {t("adminOpsProvidersUpdateExisting")}
                </label>
              </div>
              <textarea
                className="min-h-[140px] w-full rounded-xl border border-border bg-surface px-3 py-2 font-mono text-[11px]"
                placeholder='{ "providers": [ { "name": "…", "slug": "…", … } ] }'
                value={providerJson}
                onChange={(e) => setProviderJson(e.target.value)}
              />
              <Button
                type="button"
                size="sm"
                disabled={providerImportBusy || !providerJson.trim()}
                onClick={() => {
                  void (async () => {
                    setProviderImportBusy(true);
                    setProviderImportResult(null);
                    try {
                      const parsed = JSON.parse(providerJson) as {
                        providers?: ImportProviderItem[];
                        updateExisting?: boolean;
                      };
                      const providers = Array.isArray(parsed)
                        ? (parsed as ImportProviderItem[])
                        : parsed.providers;
                      if (!Array.isArray(providers) || !providers.length) {
                        toast.error(t("importEmpty"));
                        return;
                      }
                      const res = await api.post<ImportAdminProvidersResponse>(
                        "/admin/providers/import",
                        {
                          providers,
                          updateExisting:
                            providerUpdateExisting ||
                            !!parsed.updateExisting,
                        },
                      );
                      setProviderImportResult(res);
                      toast.success(
                        t("adminOpsProvidersResult")
                          .replace("{c}", String(res.created))
                          .replace("{u}", String(res.updated))
                          .replace("{s}", String(res.skipped))
                          .replace("{f}", String(res.failed)),
                      );
                    } catch (err) {
                      toast.error(
                        err instanceof ApiError
                          ? err.problem.detail || err.message
                          : err instanceof SyntaxError
                            ? err.message
                            : t("errorGeneric"),
                      );
                    } finally {
                      setProviderImportBusy(false);
                    }
                  })();
                }}
              >
                {providerImportBusy
                  ? t("loading")
                  : t("adminOpsProvidersImport")}
              </Button>
              {providerImportResult ? (
                <p className="text-xs text-muted">
                  {t("adminOpsProvidersResult")
                    .replace("{c}", String(providerImportResult.created))
                    .replace("{u}", String(providerImportResult.updated))
                    .replace("{s}", String(providerImportResult.skipped))
                    .replace("{f}", String(providerImportResult.failed))}
                </p>
              ) : null}
            </CardContent>
          </Card>
        </div>
      ) : null}

      <form className="space-y-4" onSubmit={onSubmit}>
        {/* —— Instance —— */}
        {tab === "instance" ? (
          <Card>
            <CardHeader>
              <CardTitle>{t("instance")}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="space-y-1">
                <Label>{t("instanceName")}</Label>
                <Input
                  value={settings.instance.instanceName ?? ""}
                  onChange={(e) =>
                    setSettings({
                      ...settings,
                      instance: {
                        ...settings.instance,
                        instanceName: e.target.value,
                      },
                    })
                  }
                />
              </div>
              <div className="grid gap-3 sm:grid-cols-2">
                <div className="space-y-1">
                  <Label>{t("locale")}</Label>
                  <select
                    className="flex h-10 w-full rounded-xl border border-border bg-surface px-3 text-sm"
                    value={settings.instance.defaultLocale}
                    onChange={(e) =>
                      setSettings({
                        ...settings,
                        instance: {
                          ...settings.instance,
                          defaultLocale: e.target.value,
                        },
                      })
                    }
                  >
                    <option value="tr">tr</option>
                    <option value="en">en</option>
                  </select>
                </div>
                <div className="space-y-1">
                  <Label>{t("mainCurrency")}</Label>
                  <select
                    className="flex h-10 w-full rounded-xl border border-border bg-surface px-3 text-sm"
                    value={settings.instance.defaultCurrency}
                    onChange={(e) =>
                      setSettings({
                        ...settings,
                        instance: {
                          ...settings.instance,
                          defaultCurrency: e.target.value,
                        },
                      })
                    }
                  >
                    {["TRY", "USD", "EUR", "GBP"].map((c) => (
                      <option key={c} value={c}>
                        {c}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <input
                  id="admin-allow-public-reg"
                  type="checkbox"
                  checked={settings.instance.allowPublicRegistration}
                  onChange={(e) =>
                    setSettings({
                      ...settings,
                      instance: {
                        ...settings.instance,
                        allowPublicRegistration: e.target.checked,
                      },
                    })
                  }
                />
                <label htmlFor="admin-allow-public-reg">
                  {t("allowPublicReg")}
                </label>
                <InfoTip label={t("moreInfo")}>
                  {t("allowPublicRegHint")}
                </InfoTip>
              </div>
              <div className="space-y-1">
                <LabelWithInfo
                  info={t("defaultThemeColorHint")}
                  infoLabel={t("moreInfo")}
                >
                  {t("defaultThemeColor")}
                </LabelWithInfo>
                <select
                  className="flex h-10 w-full rounded-xl border border-border bg-surface px-3 text-sm"
                  value={
                    settings.instance.defaultApplicationThemeColor ??
                    "Royal Purple"
                  }
                  onChange={(e) =>
                    setSettings({
                      ...settings,
                      instance: {
                        ...settings.instance,
                        defaultApplicationThemeColor: e.target.value,
                      },
                    })
                  }
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
                  id="admin-default-dark"
                  type="checkbox"
                  checked={settings.instance.defaultDarkTheme ?? false}
                  onChange={(e) =>
                    setSettings({
                      ...settings,
                      instance: {
                        ...settings.instance,
                        defaultDarkTheme: e.target.checked,
                      },
                    })
                  }
                />
                <label htmlFor="admin-default-dark">
                  {t("defaultDarkTheme")}
                </label>
                <InfoTip label={t("moreInfo")}>
                  {t("defaultDarkThemeHint")}
                </InfoTip>
              </div>
            </CardContent>
          </Card>
        ) : null}

        {/* —— AI —— */}
        {tab === "ai" ? (
          <Card>
            <CardHeader>
              <CardTitle>{t("aiSettings")}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <AiSettingsFields
                value={{
                  provider: settings.ai.provider ?? "openai",
                  baseUrl: aiBaseUrl,
                  model: settings.ai.model ?? "",
                  apiKey: aiKey,
                }}
                onChange={(next) => {
                  setSettings({
                    ...settings,
                    ai: {
                      ...settings.ai,
                      provider: next.provider,
                      model: next.model,
                    },
                  });
                  setAiBaseUrl(next.baseUrl);
                  setAiKey(next.apiKey);
                }}
                keyOptionalKeep
                hasExistingKey={settings.ai.hasApiKey}
                apiKeyMasked={settings.ai.apiKeyMasked}
              />
              <div className="flex flex-wrap items-center gap-2">
                <Button
                  type="button"
                  variant="secondary"
                  disabled={busy}
                  onClick={() => void testAi()}
                >
                  {t("testAi")}
                </Button>
                <p className="text-xs text-muted">{t("aiTestSavesFirst")}</p>
              </div>
            </CardContent>
          </Card>
        ) : null}

        {/* —— SMTP —— */}
        {tab === "smtp" ? (
          <Card>
            <CardHeader>
              <CardTitle>{t("smtp")}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex items-center gap-2 text-sm">
                <input
                  id="admin-smtp-enabled"
                  type="checkbox"
                  checked={settings.smtp.enabled}
                  onChange={(e) =>
                    setSettings({
                      ...settings,
                      smtp: { ...settings.smtp, enabled: e.target.checked },
                    })
                  }
                />
                <label htmlFor="admin-smtp-enabled">{t("smtpEnabled")}</label>
                <InfoTip label={t("moreInfo")}>{t("smtpEnabledHint")}</InfoTip>
              </div>
              <div className="space-y-1">
                <Label>{t("smtpHost")}</Label>
                <Input
                  value={settings.smtp.host ?? ""}
                  onChange={(e) =>
                    setSettings({
                      ...settings,
                      smtp: { ...settings.smtp, host: e.target.value },
                    })
                  }
                />
              </div>
              <div className="grid gap-3 sm:grid-cols-2">
                <div className="space-y-1">
                  <Label>{t("smtpPort")}</Label>
                  <Input
                    type="number"
                    value={settings.smtp.port ?? ""}
                    onChange={(e) =>
                      setSettings({
                        ...settings,
                        smtp: {
                          ...settings.smtp,
                          port: e.target.value ? Number(e.target.value) : null,
                        },
                      })
                    }
                  />
                </div>
                <div className="space-y-1">
                  <Label>{t("smtpUser")}</Label>
                  <Input
                    value={settings.smtp.user ?? ""}
                    onChange={(e) =>
                      setSettings({
                        ...settings,
                        smtp: { ...settings.smtp, user: e.target.value },
                      })
                    }
                  />
                </div>
              </div>
              <div className="space-y-1">
                <Label>
                  {t("password")}{" "}
                  {settings.smtp.hasPassword
                    ? `(${t("secretSet")})`
                    : `(${t("secretNotSet")})`}
                </Label>
                <PasswordInput
                  value={smtpPassword}
                  onChange={(e) => setSmtpPassword(e.target.value)}
                  placeholder={t("leaveBlankToKeep")}
                  showLabel={t("showPassword")}
                  hideLabel={t("hidePassword")}
                />
              </div>
              <div className="space-y-1">
                <Label>{t("smtpFromEmail")}</Label>
                <Input
                  value={settings.smtp.fromEmail ?? ""}
                  onChange={(e) =>
                    setSettings({
                      ...settings,
                      smtp: { ...settings.smtp, fromEmail: e.target.value },
                    })
                  }
                />
              </div>
              <div className="space-y-1 border-t border-border pt-3">
                <Label>{t("testSmtpTo")}</Label>
                <Input
                  type="email"
                  value={testSmtpTo}
                  onChange={(e) => setTestSmtpTo(e.target.value)}
                  placeholder="admin@example.com"
                />
                <Button
                  type="button"
                  variant="secondary"
                  className="mt-2"
                  disabled={busy}
                  onClick={() => void testSmtp()}
                >
                  {t("testSmtp")}
                </Button>
              </div>
            </CardContent>
          </Card>
        ) : null}

        {tab !== "ops" ? (
          <div className="flex items-center justify-between gap-3">
            <p className="text-xs text-muted">{t("adminSettingsSaveTab")}</p>
            <Button type="submit" disabled={busy}>
              {busy ? t("loading") : t("save")}
            </Button>
          </div>
        ) : null}
      </form>
    </div>
  );
}

export default function AdminSettingsPage() {
  return (
    <Suspense fallback={<PageLoader />}>
      <AdminSettingsInner />
    </Suspense>
  );
}
