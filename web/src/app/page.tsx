"use client";

import { SubifyLogo } from "@/components/brand/logo";
import { useAuth } from "@/lib/auth/context";
import { useI18n } from "@/lib/i18n/context";
import {
  Bell,
  CreditCard,
  LineChart,
  Sparkles,
} from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

const featureKeys = [
  {
    key: "landingFeatSubs" as const,
    desc: "landingFeatSubsDesc" as const,
    icon: CreditCard,
  },
  {
    key: "landingFeatAi" as const,
    desc: "landingFeatAiDesc" as const,
    icon: Sparkles,
  },
  {
    key: "landingFeatAlerts" as const,
    desc: "landingFeatAlertsDesc" as const,
    icon: Bell,
  },
  {
    key: "landingFeatReports" as const,
    desc: "landingFeatReportsDesc" as const,
    icon: LineChart,
  },
];

export default function LandingPage() {
  const { t } = useI18n();
  const { isAuthenticated, loading, checkSetup } = useAuth();
  const router = useRouter();
  const [checking, setChecking] = useState(true);

  useEffect(() => {
    void (async () => {
      const status = await checkSetup();
      if (status && status.isSetupComplete === false) {
        router.replace("/setup");
        return;
      }
      if (!loading && isAuthenticated) {
        router.replace("/dashboard");
        return;
      }
      setChecking(false);
    })();
  }, [loading, isAuthenticated, router, checkSetup]);

  if (checking || loading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background text-muted">
        {t("loading")}
      </div>
    );
  }

  return (
    <div className="flex min-h-screen flex-col bg-aurora text-foreground">
      {/* Nav — mockup pill bar */}
      <header className="mx-auto flex w-full max-w-6xl items-center justify-between px-4 py-5 md:px-6">
        <SubifyLogo wordmark={t("appName")} />
        <nav className="hidden items-center gap-8 text-sm text-muted md:flex">
          <a href="#features" className="hover:text-foreground">
            {t("landingFeatures")}
          </a>
        </nav>
        <div className="flex items-center gap-2">
          <Link
            href="/login"
            className="hidden h-10 items-center rounded-full px-4 text-sm font-medium text-muted hover:text-foreground sm:inline-flex"
          >
            {t("login")}
          </Link>
          <Link
            href="/register"
            className="inline-flex h-10 items-center rounded-full bg-primary px-5 text-sm font-medium text-white shadow-[var(--shadow-glow)] hover:bg-primary-hover"
          >
            {t("landingCtaStart")}
          </Link>
        </div>
      </header>

      {/* Hero */}
      <section className="mx-auto grid w-full max-w-6xl flex-1 items-center gap-12 px-4 py-12 md:grid-cols-2 md:px-6 md:py-20">
        <div className="space-y-6">
          <h1 className="text-4xl font-bold tracking-tight md:text-5xl lg:text-[3.25rem] lg:leading-[1.1]">
            <span className="bg-gradient-to-br from-foreground via-foreground to-primary bg-clip-text text-transparent">
              {t("landingHeroTitle")}
            </span>
          </h1>
          <p className="max-w-lg text-lg text-muted">{t("landingHeroBody")}</p>
          <div className="flex flex-wrap gap-3">
            <Link
              href="/register"
              className="inline-flex h-12 items-center rounded-full bg-primary px-7 text-sm font-semibold text-white shadow-[var(--shadow-glow)] hover:bg-primary-hover"
            >
              {t("landingCtaStart")}
            </Link>
            <Link
              href="/login"
              className="inline-flex h-12 items-center rounded-full border border-border bg-surface/60 px-7 text-sm font-semibold backdrop-blur hover:border-primary/40"
            >
              {t("login")}
            </Link>
          </div>
          <p className="text-sm text-muted">{t("landingSocialProof")}</p>
        </div>

        {/* Phone-style preview card (mockup product shot) */}
        <div className="relative mx-auto w-full max-w-sm">
          <div
            className="absolute -inset-8 rounded-[2rem] opacity-70 blur-3xl"
            style={{
              background:
                "radial-gradient(circle, color-mix(in srgb, var(--primary) 45%, transparent), transparent 70%)",
            }}
          />
          <div className="relative overflow-hidden rounded-[1.75rem] border border-border bg-surface p-4 shadow-[var(--shadow-card)]">
            <div className="mb-4 flex items-center justify-between">
              <span className="text-sm font-semibold text-primary">
                {t("appName")}
              </span>
              <span className="rounded-full bg-primary-soft px-2.5 py-0.5 text-xs font-medium text-primary">
                {t("ai")}
              </span>
            </div>
            <p className="text-xs text-muted">{t("monthlyTotal")}</p>
            <p className="mb-4 text-3xl font-bold tracking-tight">₺450,50</p>
            <div className="space-y-2">
              {[
                { name: "Netflix", price: "₺120,99", color: "#E50914" },
                { name: "Spotify", price: "₺59,99", color: "#1DB954" },
                { name: "Amazon Prime", price: "₺29,00", color: "#FF9900" },
              ].map((row) => (
                <div
                  key={row.name}
                  className="flex items-center justify-between rounded-xl border border-border bg-background/50 px-3 py-2.5"
                >
                  <div className="flex items-center gap-2.5">
                    <span
                      className="h-8 w-8 rounded-lg"
                      style={{ background: row.color }}
                    />
                    <span className="text-sm font-medium">{row.name}</span>
                  </div>
                  <span className="text-sm font-semibold">{row.price}</span>
                </div>
              ))}
            </div>
            <div className="mt-4 h-16 rounded-xl bg-gradient-to-t from-primary/30 to-primary/5" />
          </div>
        </div>
      </section>

      {/* Features */}
      <section
        id="features"
        className="border-t border-border/60 bg-surface/40 py-16 backdrop-blur-sm"
      >
        <div className="mx-auto max-w-6xl px-4 md:px-6">
          <h2 className="mb-10 text-center text-2xl font-bold md:text-3xl">
            {t("landingFeatures")}
          </h2>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {featureKeys.map(({ key, desc, icon: Icon }) => (
              <div
                key={key}
                className="rounded-2xl border border-border bg-surface p-5 shadow-[var(--shadow-card)] transition hover:border-primary/30 hover:shadow-[var(--shadow-glow)]"
              >
                <div className="mb-4 inline-flex h-11 w-11 items-center justify-center rounded-xl bg-primary-soft text-primary">
                  <Icon className="h-5 w-5" />
                </div>
                <h3 className="mb-1.5 font-semibold">{t(key)}</h3>
                <p className="text-sm leading-relaxed text-muted">{t(desc)}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <footer className="border-t border-border py-8 text-center text-xs text-muted">
        {t("appName")} · Self-hosted
      </footer>
    </div>
  );
}
