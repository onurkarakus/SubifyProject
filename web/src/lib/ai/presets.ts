/**
 * OpenAI-compatible BYOK presets.
 * Only base URL + a default model placeholder are filled on select.
 * Model catalog is NOT maintained here — users type the id their provider accepts.
 */

export type AiPresetId =
  | "openai"
  | "gemini"
  | "xai"
  | "groq"
  | "openrouter"
  | "deepseek"
  | "ollama"
  | "custom";

export type AiPreset = {
  id: AiPresetId;
  /** Default API root (…/v1). Editable in UI. */
  baseUrl: string;
  /** Placeholder / initial model when switching to this preset. */
  defaultModel: string;
  /** Whether base URL is typically edited (local / proxy). */
  baseUrlEditable: boolean;
  /** Hint that key may be optional (e.g. local Ollama). */
  keyOptional?: boolean;
};

export const AI_PRESETS: AiPreset[] = [
  {
    id: "openai",
    baseUrl: "https://api.openai.com/v1",
    defaultModel: "gpt-4o-mini",
    baseUrlEditable: false,
  },
  {
    // Google AI Studio key via OpenAI-compatible endpoint
    id: "gemini",
    baseUrl: "https://generativelanguage.googleapis.com/v1beta/openai",
    // Placeholder only — Google renames models often; override if needed.
    // gemini-flash-latest tracks Google's current flash model alias.
    defaultModel: "gemini-flash-latest",
    baseUrlEditable: false,
  },
  {
    // xAI Grok — not to be confused with Groq
    id: "xai",
    baseUrl: "https://api.x.ai/v1",
    defaultModel: "grok-3-mini",
    baseUrlEditable: false,
  },
  {
    id: "groq",
    baseUrl: "https://api.groq.com/openai/v1",
    defaultModel: "llama-3.3-70b-versatile",
    baseUrlEditable: false,
  },
  {
    id: "openrouter",
    baseUrl: "https://openrouter.ai/api/v1",
    defaultModel: "openai/gpt-4o-mini",
    baseUrlEditable: false,
  },
  {
    id: "deepseek",
    baseUrl: "https://api.deepseek.com/v1",
    defaultModel: "deepseek-chat",
    baseUrlEditable: false,
  },
  {
    id: "ollama",
    baseUrl: "http://localhost:11434/v1",
    defaultModel: "llama3.2",
    baseUrlEditable: true,
    keyOptional: true,
  },
  {
    id: "custom",
    baseUrl: "",
    defaultModel: "",
    baseUrlEditable: true,
  },
];

export function getAiPreset(id: string | null | undefined): AiPreset {
  const found = AI_PRESETS.find(
    (p) => p.id === (id ?? "").toLowerCase(),
  );
  return found ?? AI_PRESETS.find((p) => p.id === "custom")!;
}

export function normalizeAiPresetId(
  provider: string | null | undefined,
): AiPresetId {
  const id = (provider ?? "openai").toLowerCase().trim();
  // Common aliases
  if (id === "grok") return "xai";
  if (id === "google" || id === "google-ai") return "gemini";
  if (AI_PRESETS.some((p) => p.id === id)) {
    return id as AiPresetId;
  }
  return "custom";
}
