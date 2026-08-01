"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { applyAccentColor } from "@/lib/theme/accents";

type Theme = "light" | "dark" | "system";

type ThemeContextValue = {
  theme: Theme;
  resolved: "light" | "dark";
  accent: string;
  setTheme: (theme: Theme) => void;
  setAccent: (accent: string) => void;
  toggle: () => void;
};

const ThemeContext = createContext<ThemeContextValue | null>(null);
const STORAGE_KEY = "subify.theme";
const ACCENT_KEY = "subify.accent";

function resolve(theme: Theme): "light" | "dark" {
  if (theme === "system") {
    if (typeof window === "undefined") return "light";
    return window.matchMedia("(prefers-color-scheme: dark)").matches
      ? "dark"
      : "light";
  }
  return theme;
}

function applyClass(resolved: "light" | "dark") {
  const root = document.documentElement;
  root.classList.toggle("dark", resolved === "dark");
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<Theme>("system");
  const [resolved, setResolved] = useState<"light" | "dark">("light");
  const [accent, setAccentState] = useState("Royal Purple");

  useEffect(() => {
    const stored = localStorage.getItem(STORAGE_KEY) as Theme | null;
    const initial =
      stored === "light" || stored === "dark" || stored === "system"
        ? stored
        : "system";
    setThemeState(initial);
    const r = resolve(initial);
    setResolved(r);
    applyClass(r);

    const storedAccent = localStorage.getItem(ACCENT_KEY);
    if (storedAccent) {
      setAccentState(storedAccent);
      applyAccentColor(storedAccent, r === "dark");
    } else {
      applyAccentColor("Royal Purple", r === "dark");
    }
  }, []);

  useEffect(() => {
    if (theme !== "system") return;
    const mq = window.matchMedia("(prefers-color-scheme: dark)");
    const onChange = () => {
      const r = resolve("system");
      setResolved(r);
      applyClass(r);
      applyAccentColor(accent, r === "dark");
    };
    mq.addEventListener("change", onChange);
    return () => mq.removeEventListener("change", onChange);
  }, [theme, accent]);

  const setTheme = useCallback(
    (next: Theme) => {
      setThemeState(next);
      localStorage.setItem(STORAGE_KEY, next);
      const r = resolve(next);
      setResolved(r);
      applyClass(r);
      applyAccentColor(accent, r === "dark");
    },
    [accent],
  );

  const setAccent = useCallback(
    (next: string) => {
      setAccentState(next);
      localStorage.setItem(ACCENT_KEY, next);
      applyAccentColor(next, resolved === "dark");
    },
    [resolved],
  );

  const toggle = useCallback(() => {
    setTheme(resolved === "dark" ? "light" : "dark");
  }, [resolved, setTheme]);

  const value = useMemo(
    () => ({ theme, resolved, accent, setTheme, setAccent, toggle }),
    [theme, resolved, accent, setTheme, setAccent, toggle],
  );

  return (
    <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
  );
}

export function useTheme() {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error("useTheme must be used within ThemeProvider");
  return ctx;
}
