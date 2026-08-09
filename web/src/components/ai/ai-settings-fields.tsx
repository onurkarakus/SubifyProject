"use client";

import { InfoTip, LabelWithInfo } from "@/components/ui/info-tip";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PasswordInput } from "@/components/ui/password-input";
import {
  AI_PRESETS,
  getAiPreset,
  normalizeAiPresetId,
  type AiPresetId,
} from "@/lib/ai/presets";
import { useI18n } from "@/lib/i18n/context";
import { cn } from "@/lib/utils";

export type AiSettingsFieldsValue = {
  provider: string;
  baseUrl: string;
  model: string;
  apiKey: string;
};

type Props = {
  value: AiSettingsFieldsValue;
  onChange: (next: AiSettingsFieldsValue) => void;
  /** When true, empty key means “keep existing”. */
  keyOptionalKeep?: boolean;
  hasExistingKey?: boolean;
  apiKeyMasked?: string | null;
  className?: string;
};

/**
 * Shared BYOK AI fields: preset select → base URL + free-text model + key.
 * No model catalog chips — lists go stale and confuse providers (e.g. Groq vs Grok).
 */
export function AiSettingsFields({
  value,
  onChange,
  keyOptionalKeep = false,
  hasExistingKey = false,
  apiKeyMasked,
  className,
}: Props) {
  const { t } = useI18n();
  const presetId = normalizeAiPresetId(value.provider);
  const preset = getAiPreset(presetId);

  function patch(partial: Partial<AiSettingsFieldsValue>) {
    onChange({ ...value, ...partial });
  }

  function onPresetChange(id: AiPresetId) {
    const next = getAiPreset(id);
    patch({
      provider: id,
      baseUrl: next.baseUrl,
      model:
        id === "custom" && value.model
          ? value.model
          : next.defaultModel || value.model,
    });
  }

  const providerLabel = (id: AiPresetId) => {
    switch (id) {
      case "openai":
        return t("aiPresetOpenai");
      case "gemini":
        return t("aiPresetGemini");
      case "xai":
        return t("aiPresetXai");
      case "groq":
        return t("aiPresetGroq");
      case "openrouter":
        return t("aiPresetOpenrouter");
      case "deepseek":
        return t("aiPresetDeepseek");
      case "ollama":
        return t("aiPresetOllama");
      case "custom":
        return t("aiPresetCustom");
    }
  };

  return (
    <div className={cn("space-y-4", className)}>
      <div className="space-y-2">
        <LabelWithInfo info={t("aiProviderHint")} infoLabel={t("moreInfo")}>
          {t("provider")}
        </LabelWithInfo>
        <select
          className="flex h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm"
          value={presetId}
          onChange={(e) => onPresetChange(e.target.value as AiPresetId)}
        >
          {AI_PRESETS.map((p) => (
            <option key={p.id} value={p.id}>
              {providerLabel(p.id)}
            </option>
          ))}
        </select>
      </div>

      <div className="space-y-2">
        <LabelWithInfo info={t("aiBaseUrlHint")} infoLabel={t("moreInfo")}>
          {t("aiBaseUrl")}
        </LabelWithInfo>
        <Input
          value={value.baseUrl}
          onChange={(e) => patch({ baseUrl: e.target.value })}
          readOnly={!preset.baseUrlEditable}
          className={cn(!preset.baseUrlEditable && "bg-muted/20 text-muted")}
          placeholder={
            presetId === "custom"
              ? "https://your-proxy.example/v1"
              : preset.baseUrl
          }
          autoComplete="off"
        />        
        {presetId === "ollama" ? (
          <p className="text-xs text-muted">{t("aiOllamaHint")}</p>
        ) : null}
        {!preset.baseUrlEditable && presetId !== "gemini" ? (
          <p className="text-xs text-muted">{t("aiBaseUrlLockedHint")}</p>
        ) : null}
      </div>

      <div className="space-y-2">
        <LabelWithInfo info={t("aiModelHint")} infoLabel={t("moreInfo")}>
          {t("aiModel")}
        </LabelWithInfo>
        <Input
          value={value.model}
          onChange={(e) => patch({ model: e.target.value })}
          placeholder={preset.defaultModel || "model-id"}
          autoComplete="off"
        />
      </div>

      <div className="space-y-2">
        <div className="flex items-center gap-1.5">
          <Label>
            {t("apiKey")}
            {hasExistingKey
              ? ` (${apiKeyMasked ?? t("secretSet")})`
              : ` (${t("secretNotSet")})`}
          </Label>
          {preset.keyOptional ? (
            <InfoTip label={t("moreInfo")}>{t("aiKeyOptionalHint")}</InfoTip>
          ) : null}
        </div>
        {keyOptionalKeep && hasExistingKey ? (
          <p className="text-xs text-muted">{t("leaveBlankToKeep")}</p>
        ) : null}
        <PasswordInput
          value={value.apiKey}
          onChange={(e) => patch({ apiKey: e.target.value })}
          placeholder={
            keyOptionalKeep && hasExistingKey
              ? t("leaveBlankToKeep")
              : preset.keyOptional
                ? "ollama"
                : "sk-..."
          }
          autoComplete="off"
          showLabel={t("showPassword")}
          hideLabel={t("hidePassword")}
        />
      </div>
    </div>
  );
}
