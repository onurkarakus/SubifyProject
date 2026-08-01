/** Maps API ThemeColors presets to CSS primary hex (light mode). */
export const THEME_ACCENTS: Record<string, { light: string; dark: string }> = {
  "Royal Purple": { light: "#7c3aed", dark: "#8b5cf6" },
  "Ocean Blue": { light: "#2563eb", dark: "#3b82f6" },
  "Forest Green": { light: "#059669", dark: "#34d399" },
  "Sunset Orange": { light: "#ea580c", dark: "#fb923c" },
  "Cherry Red": { light: "#dc2626", dark: "#f87171" },
  "Golden Yellow": { light: "#ca8a04", dark: "#fbbf24" },
};

export const THEME_COLOR_PRESETS = Object.keys(THEME_ACCENTS);

export function applyAccentColor(themeColor: string | null | undefined, isDark: boolean) {
  if (typeof document === "undefined") return;
  const key = themeColor && THEME_ACCENTS[themeColor] ? themeColor : "Royal Purple";
  const pair = THEME_ACCENTS[key];
  const hex = isDark ? pair.dark : pair.light;
  const root = document.documentElement;
  root.style.setProperty("--primary", hex);
  root.style.setProperty("--ring", hex);
  // Soft fill + hover for mockup-style pills / active nav
  root.style.setProperty(
    "--primary-soft",
    `color-mix(in srgb, ${hex} ${isDark ? "18%" : "14%"}, transparent)`,
  );
  root.style.setProperty(
    "--primary-hover",
    isDark ? pair.light : pair.dark,
  );
  root.dataset.accent = key;
}
