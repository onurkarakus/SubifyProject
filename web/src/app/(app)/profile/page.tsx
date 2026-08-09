"use client";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { InfoTip, LabelWithInfo } from "@/components/ui/info-tip";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PasswordInput } from "@/components/ui/password-input";
import { PageLoader } from "@/components/ui/spinner";
import { Tabs } from "@/components/ui/tabs";
import { api, ApiError } from "@/lib/api/client";
import type {
  NotificationSettingsResponse,
  ProfileResponse,
} from "@/lib/api/types";
import { useAuth } from "@/lib/auth/context";
import { useI18n } from "@/lib/i18n/context";
import { THEME_COLOR_PRESETS } from "@/lib/theme/accents";
import { useTheme } from "@/lib/theme/context";
import { useRouter, useSearchParams } from "next/navigation";
import {
  FormEvent,
  Suspense,
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";
import { toast } from "sonner";

type ProfileTab = "profile" | "notifications" | "password";

function parseTab(raw: string | null): ProfileTab {
  if (raw === "notifications" || raw === "password" || raw === "profile") {
    return raw;
  }
  return "profile";
}

function ProfilePageInner() {
  const { t, setLocale } = useI18n();
  const { refreshUser, user } = useAuth();
  const { resolved, accent, setTheme, setAccent } = useTheme();
  const router = useRouter();
  const searchParams = useSearchParams();
  const tab = parseTab(searchParams.get("tab"));

  const [loading, setLoading] = useState(true);
  const [profile, setProfile] = useState<ProfileResponse | null>(null);
  const [notifications, setNotifications] =
    useState<NotificationSettingsResponse | null>(null);
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [busy, setBusy] = useState(false);

  const tabs = useMemo(
    () =>
      [
        { id: "profile" as const, label: t("profile") },
        { id: "notifications" as const, label: t("notifications") },
        { id: "password" as const, label: t("changePassword") },
      ] as const,
    [t],
  );

  const setTab = useCallback(
    (next: ProfileTab) => {
      const q = new URLSearchParams(searchParams.toString());
      q.set("tab", next);
      router.replace(`/profile?${q.toString()}`, { scroll: false });
    },
    [router, searchParams],
  );

  useEffect(() => {
    void (async () => {
      try {
        const [p, n] = await Promise.all([
          api.get<ProfileResponse>("/profile"),
          api.get<NotificationSettingsResponse>("/profile/notifications"),
        ]);
        // Hydrate form only — do not force theme on open (stale DB vs shell).
        setProfile({
          ...p,
          darkTheme: resolved === "dark",
          applicationThemeColor:
            accent || p.applicationThemeColor || "Royal Purple",
        });
        setNotifications(n);
      } catch (e) {
        toast.error(e instanceof ApiError ? e.message : t("errorGeneric"));
      } finally {
        setLoading(false);
      }
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps -- first mount hydrate
  }, [t]);

  async function saveProfile(e: FormEvent) {
    e.preventDefault();
    if (!profile) return;
    setBusy(true);
    try {
      const updated = await api.put<ProfileResponse>("/profile", {
        fullName: profile.fullName,
        locale: profile.locale,
        mainCurrency: profile.mainCurrency,
        monthlyBudget: profile.monthlyBudget,
        applicationThemeColor: profile.applicationThemeColor,
        darkTheme: profile.darkTheme,
      });
      setProfile(updated);
      if (user) {
        refreshUser({
          ...user,
          fullName: updated.fullName,
          locale: updated.locale,
        });
      }
      if (updated.locale === "tr" || updated.locale === "en") {
        setLocale(updated.locale);
      }
      if (updated.applicationThemeColor) {
        setAccent(updated.applicationThemeColor);
      }
      setTheme(updated.darkTheme ? "dark" : "light");
      toast.success(t("save"));
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    } finally {
      setBusy(false);
    }
  }

  async function changePassword(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    try {
      await api.post("/auth/change-password", {
        currentPassword,
        newPassword,
      });
      setCurrentPassword("");
      setNewPassword("");
      toast.success(t("changePassword"));
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    } finally {
      setBusy(false);
    }
  }

  async function saveNotifications(e: FormEvent) {
    e.preventDefault();
    if (!notifications) return;
    setBusy(true);
    try {
      const updated = await api.put<NotificationSettingsResponse>(
        "/profile/notifications",
        {
          emailEnabled: notifications.emailEnabled,
          pushEnabled: notifications.pushEnabled,
          daysBeforeRenewal: notifications.daysBeforeRenewal,
        },
      );
      setNotifications(updated);
      toast.success(t("save"));
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    } finally {
      setBusy(false);
    }
  }

  if (loading || !profile || !notifications) return <PageLoader />;

  return (
    <div className="mx-auto max-w-xl space-y-6">
      <div>
        <h1 className="page-title">{t("profile")}</h1>
        <p className="mt-1 text-sm text-muted">{t("profileHint")}</p>
      </div>

      <Tabs
        tabs={[...tabs]}
        value={tab}
        onChange={setTab}
        aria-label={t("profile")}
      />

      {tab === "profile" ? (
        <Card>
          <CardHeader>
            <CardTitle>{t("profile")}</CardTitle>
          </CardHeader>
          <CardContent>
            <form className="space-y-4" onSubmit={saveProfile}>
              <div className="space-y-2">
                <Label>{t("email")}</Label>
                <Input value={profile.email} disabled />
              </div>
              <div className="space-y-2">
                <Label>{t("fullName")}</Label>
                <Input
                  value={profile.fullName}
                  onChange={(e) =>
                    setProfile({ ...profile, fullName: e.target.value })
                  }
                />
              </div>
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label>{t("locale")}</Label>
                  <select
                    className="flex h-10 w-full rounded-xl border border-border bg-surface px-3 text-sm"
                    value={profile.locale}
                    onChange={(e) =>
                      setProfile({ ...profile, locale: e.target.value })
                    }
                  >
                    <option value="tr">tr</option>
                    <option value="en">en</option>
                  </select>
                </div>
                <div className="space-y-2">
                  <Label>{t("mainCurrency")}</Label>
                  <select
                    className="flex h-10 w-full rounded-xl border border-border bg-surface px-3 text-sm"
                    value={profile.mainCurrency}
                    onChange={(e) =>
                      setProfile({ ...profile, mainCurrency: e.target.value })
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
              <div className="space-y-2">
                <Label>{t("monthlyBudget")}</Label>
                <Input
                  type="number"
                  min={0}
                  value={profile.monthlyBudget ?? ""}
                  onChange={(e) =>
                    setProfile({
                      ...profile,
                      monthlyBudget: e.target.value
                        ? Number(e.target.value)
                        : null,
                    })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label>{t("defaultThemeColor")}</Label>
                <select
                  className="flex h-10 w-full rounded-xl border border-border bg-surface px-3 text-sm"
                  value={profile.applicationThemeColor || "Royal Purple"}
                  onChange={(e) => {
                    const color = e.target.value;
                    setProfile({
                      ...profile,
                      applicationThemeColor: color,
                    });
                    setAccent(color);
                  }}
                >
                  {THEME_COLOR_PRESETS.map((c) => (
                    <option key={c} value={c}>
                      {c}
                    </option>
                  ))}
                </select>
              </div>
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={profile.darkTheme}
                  onChange={(e) => {
                    const dark = e.target.checked;
                    setProfile({ ...profile, darkTheme: dark });
                    setTheme(dark ? "dark" : "light");
                  }}
                />
                {t("darkTheme")}
              </label>
              <div className="flex justify-end">
                <Button type="submit" disabled={busy}>
                  {busy ? t("loading") : t("save")}
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      ) : null}

      {tab === "notifications" ? (
        <Card>
          <CardHeader>
            <CardTitle>{t("notifications")}</CardTitle>
          </CardHeader>
          <CardContent>
            <form className="space-y-4" onSubmit={saveNotifications}>
              <div className="space-y-2">
                <LabelWithInfo
                  htmlFor="daysBeforeRenewal"
                  info={t("daysBeforeRenewalHint")}
                  infoLabel={t("moreInfo")}
                >
                  {t("daysBeforeRenewal")}
                </LabelWithInfo>
                <Input
                  id="daysBeforeRenewal"
                  type="number"
                  min={0}
                  max={30}
                  required
                  value={notifications.daysBeforeRenewal}
                  onChange={(e) =>
                    setNotifications({
                      ...notifications,
                      daysBeforeRenewal: Number(e.target.value),
                    })
                  }
                />
              </div>

              <div className="flex items-center gap-2 text-sm">
                <input
                  id="profile-push-enabled"
                  type="checkbox"
                  checked={notifications.pushEnabled}
                  onChange={(e) =>
                    setNotifications({
                      ...notifications,
                      pushEnabled: e.target.checked,
                    })
                  }
                />
                <label htmlFor="profile-push-enabled">
                  {t("pushNotifications")}
                </label>
              </div>

              <div className="flex items-center gap-2 text-sm">
                <input
                  id="profile-email-enabled"
                  type="checkbox"
                  checked={notifications.emailEnabled}
                  onChange={(e) =>
                    setNotifications({
                      ...notifications,
                      emailEnabled: e.target.checked,
                    })
                  }
                />
                <label htmlFor="profile-email-enabled">
                  {t("emailNotifications")}
                </label>
                <InfoTip label={t("moreInfo")}>
                  {t("emailNotificationsHint")}
                </InfoTip>
              </div>

              <div className="flex justify-end">
                <Button type="submit" disabled={busy}>
                  {busy ? t("loading") : t("save")}
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      ) : null}

      {tab === "password" ? (
        <Card>
          <CardHeader>
            <CardTitle>{t("changePassword")}</CardTitle>
          </CardHeader>
          <CardContent>
            <form className="space-y-4" onSubmit={changePassword}>
              <div className="space-y-2">
                <Label>{t("currentPassword")}</Label>
                <PasswordInput
                  required
                  value={currentPassword}
                  onChange={(e) => setCurrentPassword(e.target.value)}
                  showLabel={t("showPassword")}
                  hideLabel={t("hidePassword")}
                />
              </div>
              <div className="space-y-2">
                <Label>{t("newPassword")}</Label>
                <PasswordInput
                  required
                  minLength={8}
                  autoComplete="new-password"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  showLabel={t("showPassword")}
                  hideLabel={t("hidePassword")}
                />
                <p className="text-xs text-muted">{t("passwordRules")}</p>
              </div>
              <div className="flex justify-end">
                <Button type="submit" disabled={busy}>
                  {busy ? t("loading") : t("changePassword")}
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      ) : null}
    </div>
  );
}

export default function ProfilePage() {
  return (
    <Suspense fallback={<PageLoader />}>
      <ProfilePageInner />
    </Suspense>
  );
}
