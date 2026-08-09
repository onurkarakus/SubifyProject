"use client";

import { useAuth } from "@/lib/auth/context";
import { useI18n } from "@/lib/i18n/context";
import { cn } from "@/lib/utils";
import {
  Bell,
  LayoutGrid,
  LineChart,
  LogIn,
  Sparkles,
  Wifi,
} from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

const featureKeys = [
  {
    key: "landingFeatSubs" as const,
    desc: "landingFeatSubsDesc" as const,
    icon: LayoutGrid,
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

const phoneSubs = [
  { name: "Netflix", price: "29,90 ₺", color: "#E50914", letter: "N" },
  { name: "Spotify", price: "54,99 ₺", color: "#1DB954", letter: "S" },
  { name: "Amazon Prime", price: "39,90 ₺", color: "#FF9900", letter: "a" },
];

/** Landing is always dark (mockup), independent of app theme toggle. */
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
      <div className="flex min-h-screen items-center justify-center bg-[#0a0a12] text-zinc-400">
        {t("loading")}
      </div>
    );
  }

  return (
    <div className="landing-root relative min-h-screen overflow-x-hidden bg-[#0a0a12] text-white">
      {/* Ambient purple wash — mockup glow */}
      <div
        className="pointer-events-none absolute inset-0"
        aria-hidden
        style={{
          background: `
            radial-gradient(ellipse 90% 55% at 50% -15%, rgba(124, 58, 237, 0.5), transparent 55%),
            radial-gradient(ellipse 55% 45% at 88% 28%, rgba(139, 92, 246, 0.35), transparent 52%),
            radial-gradient(ellipse 40% 35% at 12% 75%, rgba(88, 28, 135, 0.22), transparent 50%)
          `,
        }}
      />

      {/* —— Floating nav pill —— */}
      <header className="relative z-20 mx-auto flex w-full max-w-5xl items-center justify-center px-4 pt-6 md:pt-8">
        <div className="flex w-full max-w-4xl items-center justify-between gap-3 rounded-full border border-white/10 bg-[#12121c]/95 px-2.5 py-2 shadow-[0_12px_48px_rgba(0,0,0,0.5)] backdrop-blur-xl sm:gap-4 sm:px-3 sm:py-2.5">
          <Link href="/" className="flex shrink-0 items-center gap-2.5 pl-1.5">
            <span className="inline-flex h-9 w-9 items-center justify-center rounded-full bg-gradient-to-br from-violet-400 to-violet-700 text-sm font-bold text-white shadow-[0_0_28px_rgba(139,92,246,0.65)] ring-2 ring-violet-400/30">
              S
            </span>
            <span className="text-base font-semibold tracking-tight text-white">
              Subify
            </span>
          </Link>

          <nav className="hidden items-center gap-2 md:flex">
            <a
              href="#features"
              className={cn(
                "group inline-flex h-10 items-center gap-2 rounded-full border border-white/10",
                "bg-white/[0.04] px-4 text-sm font-medium text-zinc-200",
                "shadow-inner transition",
                "hover:border-violet-400/40 hover:bg-violet-500/15 hover:text-white",
                "hover:shadow-[0_0_20px_rgba(139,92,246,0.25)]",
              )}
            >
              <span className="inline-flex h-6 w-6 items-center justify-center rounded-full bg-violet-500/20 text-violet-300 ring-1 ring-violet-400/30 transition group-hover:bg-violet-500/35">
                <Sparkles className="h-3.5 w-3.5" aria-hidden />
              </span>
              {t("landingFeatures")}
            </a>
            <Link
              href="/login"
              className={cn(
                "group inline-flex h-10 items-center gap-2 rounded-full border border-white/10",
                "bg-gradient-to-b from-white/[0.08] to-white/[0.02] px-4 text-sm font-medium text-zinc-100",
                "transition",
                "hover:border-violet-300/50 hover:from-violet-500/25 hover:to-violet-600/10 hover:text-white",
                "hover:shadow-[0_0_22px_rgba(139,92,246,0.3)]",
              )}
            >
              <span className="inline-flex h-6 w-6 items-center justify-center rounded-full bg-zinc-800 text-violet-200 ring-1 ring-white/10 transition group-hover:bg-violet-600 group-hover:text-white">
                <LogIn className="h-3.5 w-3.5" aria-hidden />
              </span>
              {t("login")}
            </Link>
          </nav>

          <div className="flex items-center gap-2">
            <Link
              href="/login"
              className="inline-flex h-10 items-center rounded-full border border-white/12 bg-white/5 px-3.5 text-sm font-medium text-zinc-200 md:hidden"
            >
              {t("login")}
            </Link>
            <Link
              href="/register"
              className="inline-flex h-10 shrink-0 items-center rounded-full bg-violet-600 px-5 text-sm font-semibold text-white shadow-[0_0_32px_rgba(124,58,237,0.55)] ring-1 ring-violet-400/40 transition hover:bg-violet-500 hover:shadow-[0_0_40px_rgba(139,92,246,0.65)]"
            >
              {t("landingCtaStart")}
            </Link>
          </div>
        </div>
      </header>

      {/* —— Hero —— */}
      <section className="relative z-10 mx-auto grid w-full max-w-6xl items-center gap-12 px-5 pb-16 pt-14 md:grid-cols-2 md:gap-6 md:pb-24 md:pt-16 lg:gap-10">
        <div className="space-y-8 md:pr-2">
          <h1 className="text-4xl font-bold leading-[1.1] tracking-tight sm:text-5xl lg:text-[3.5rem]">
            <span className="bg-gradient-to-b from-white via-[#e9d5ff] to-[#a78bfa] bg-clip-text text-transparent">
              {t("landingHeroTitle")}
            </span>
          </h1>
          <p className="max-w-md text-base leading-relaxed text-zinc-400 sm:text-lg">
            {t("landingHeroBody")}
          </p>
          <div className="flex flex-wrap items-center gap-3">
            <Link
              href="/register"
              className="inline-flex h-12 items-center rounded-full bg-violet-600 px-8 text-sm font-semibold text-white shadow-[0_0_36px_rgba(124,58,237,0.55)] ring-1 ring-violet-400/30 transition hover:bg-violet-500"
            >
              {t("landingCtaStart")}
            </Link>
          </div>
        </div>

        {/* CSS phone mockup (previous version — no PNG crop) */}
        <div className="relative mx-auto flex w-full max-w-[340px] justify-center md:max-w-[380px]">
          <div
            className="absolute -inset-12 rounded-full opacity-90 blur-3xl"
            aria-hidden
            style={{
              background:
                "radial-gradient(circle, rgba(139,92,246,0.55) 0%, rgba(88,28,135,0.2) 45%, transparent 68%)",
            }}
          />
          <div
            className={cn(
              "relative w-[272px] rotate-[11deg] sm:w-[300px]",
              "rounded-[2.6rem] p-[10px]",
              "bg-gradient-to-b from-zinc-600 via-zinc-800 to-zinc-950",
              "shadow-[0_40px_100px_rgba(0,0,0,0.75),0_0_80px_rgba(124,58,237,0.35)]",
            )}
          >
            <div className="relative overflow-hidden rounded-[2.15rem] bg-[#0c0d12]">
              <div className="flex items-center justify-between px-5 pb-1 pt-3 text-[10px] font-medium text-zinc-300">
                <span>9:41</span>
                <div className="absolute left-1/2 top-2.5 h-[22px] w-[90px] -translate-x-1/2 rounded-full bg-black" />
                <span className="flex items-center gap-1">
                  <Wifi className="h-3 w-3" aria-hidden />
                  <span className="inline-block h-2.5 w-5 rounded-sm border border-zinc-400">
                    <span className="ml-px mt-px block h-1.5 w-3 rounded-[1px] bg-zinc-300" />
                  </span>
                </span>
              </div>

              <div className="flex items-center justify-between px-4 pb-2 pt-1">
                <span className="flex items-center gap-1.5 text-[13px] font-bold text-white">
                  <span className="inline-flex h-6 w-6 items-center justify-center rounded-lg bg-violet-600 text-[11px] shadow-[0_0_12px_rgba(139,92,246,0.6)]">
                    S
                  </span>
                  Subify
                </span>
                <span className="flex h-7 w-7 items-center justify-center rounded-full bg-zinc-800/90 ring-1 ring-white/10">
                  <Bell className="h-3.5 w-3.5 text-zinc-300" aria-hidden />
                </span>
              </div>

              <div className="mx-3 rounded-2xl bg-gradient-to-r from-violet-600 via-violet-500 to-fuchsia-500 px-3.5 py-2.5 shadow-[0_8px_24px_rgba(124,58,237,0.4)]">
                <div className="flex items-center justify-between gap-2">
                  <p className="text-[11px] font-semibold leading-snug text-white">
                    {t("landingPhoneBanner")}
                  </p>
                  <span className="shrink-0 rounded-full bg-white/20 px-2 py-0.5 text-[10px] font-bold text-white">
                    %15
                  </span>
                </div>
              </div>

              <div className="px-4 pt-4">
                <p className="text-[10px] font-medium uppercase tracking-wide text-zinc-500">
                  {t("landingPhoneTotal")}
                </p>
                <p className="mt-0.5 text-[1.75rem] font-extrabold tracking-tight text-white">
                  388.730 ₺
                </p>
              </div>

              <div className="mt-3 space-y-1.5 px-3">
                <p className="px-1 text-[10px] font-semibold text-zinc-500">
                  {t("landingPhoneActive")}
                </p>
                {phoneSubs.map((s) => (
                  <div
                    key={s.name}
                    className="flex items-center justify-between rounded-2xl border border-white/[0.06] bg-[#15161e] px-2.5 py-2.5"
                  >
                    <div className="flex items-center gap-2.5">
                      <span
                        className="flex h-8 w-8 items-center justify-center rounded-xl text-xs font-bold text-white shadow-md"
                        style={{ background: s.color }}
                      >
                        {s.letter}
                      </span>
                      <span className="text-[12px] font-semibold text-zinc-100">
                        {s.name}
                      </span>
                    </div>
                    <span className="text-[12px] font-bold tabular-nums text-zinc-200">
                      {s.price}
                    </span>
                  </div>
                ))}
              </div>

              <div className="mx-3 mb-2 mt-3 rounded-2xl border border-white/[0.06] bg-[#15161e] px-3 py-2.5">
                <p className="mb-1 text-[9px] font-medium text-zinc-500">
                  {t("landingPhoneChart")}
                </p>
                <svg
                  viewBox="0 0 140 48"
                  className="h-12 w-full"
                  preserveAspectRatio="none"
                  aria-hidden
                >
                  <defs>
                    <linearGradient id="phFill" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="0%" stopColor="#8b5cf6" stopOpacity="0.55" />
                      <stop offset="100%" stopColor="#8b5cf6" stopOpacity="0" />
                    </linearGradient>
                  </defs>
                  <path
                    d="M0,36 C12,34 22,28 35,26 C48,24 58,30 72,18 C86,6 100,10 120,8 L140,6 L140,48 L0,48 Z"
                    fill="url(#phFill)"
                  />
                  <path
                    d="M0,36 C12,34 22,28 35,26 C48,24 58,30 72,18 C86,6 100,10 120,8 L140,6"
                    fill="none"
                    stroke="#c4b5fd"
                    strokeWidth="2"
                    strokeLinecap="round"
                  />
                  <circle cx="140" cy="6" r="3" fill="#ede9fe" />
                </svg>
              </div>

              <div className="flex items-center justify-around border-t border-white/5 bg-[#0c0d12]/95 px-4 py-2.5">
                {[
                  "bg-violet-500",
                  "bg-zinc-600",
                  "bg-zinc-600",
                  "bg-zinc-600",
                ].map((c, i) => (
                  <span
                    key={i}
                    className={cn("h-1.5 w-1.5 rounded-full", c)}
                    aria-hidden
                  />
                ))}
              </div>
              <div className="mx-auto mb-1.5 mt-0.5 h-1 w-[72px] rounded-full bg-zinc-600" />
            </div>
          </div>
        </div>
      </section>

      {/* —— Features —— */}
      <section
        id="features"
        className="relative z-10 border-t border-white/5 bg-[#0a0a12]/85 px-5 py-16 backdrop-blur-sm md:py-20"
      >
        <div className="mx-auto max-w-6xl">
          <div className="mb-12 text-center">
            <h2 className="text-2xl font-bold tracking-tight text-white md:text-3xl">
              {t("landingFeatures")}
            </h2>
            <div className="mx-auto mt-2 h-1 w-16 rounded-full bg-violet-500" />
          </div>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4 lg:gap-5">
            {featureKeys.map(({ key, desc, icon: Icon }) => (
              <div
                key={key}
                className="rounded-2xl border border-white/[0.08] bg-[#14141f]/95 p-6 shadow-[0_8px_32px_rgba(0,0,0,0.35)] transition hover:border-violet-500/35 hover:shadow-[0_0_40px_rgba(124,58,237,0.15)]"
              >
                <div className="mb-5 inline-flex h-12 w-12 items-center justify-center rounded-xl bg-violet-600/20 text-violet-300 ring-1 ring-violet-500/30">
                  <Icon className="h-6 w-6" strokeWidth={1.75} />
                </div>
                <h3 className="mb-2 text-base font-semibold text-white">
                  {t(key)}
                </h3>
                <p className="text-sm leading-relaxed text-zinc-400">
                  {t(desc)}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <footer className="relative z-10 border-t border-white/5 px-5 py-8">
        <div className="mx-auto flex max-w-6xl flex-col items-center justify-between gap-4 text-xs text-zinc-500 sm:flex-row">
          <div className="flex flex-wrap justify-center gap-5">
            <span>{t("landingFooterAbout")}</span>
            <span>{t("landingFooterPrivacy")}</span>
            <span>{t("landingFooterTerms")}</span>
          </div>
          <p className="text-zinc-600">Subify · Self-hosted</p>
        </div>
      </footer>
    </div>
  );
}
