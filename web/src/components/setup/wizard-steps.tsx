"use client";

import { cn } from "@/lib/utils";

export type SetupStepId =
  | "welcome"
  | "admin"
  | "instance"
  | "users"
  | "smtp"
  | "ai"
  | "finish";

export const SETUP_STEPS: { id: SetupStepId; labelKey: string }[] = [
  { id: "welcome", labelKey: "setupStepWelcome" },
  { id: "admin", labelKey: "setupStepAdmin" },
  { id: "instance", labelKey: "setupStepInstance" },
  { id: "users", labelKey: "setupStepUsers" },
  { id: "smtp", labelKey: "setupStepSmtp" },
  { id: "ai", labelKey: "setupStepAi" },
  { id: "finish", labelKey: "setupStepFinish" },
];

export function WizardSteps({
  current,
  labels,
}: {
  current: SetupStepId;
  labels: Record<string, string>;
}) {
  const idx = SETUP_STEPS.findIndex((s) => s.id === current);

  return (
    <ol className="mb-8 flex flex-wrap gap-2">
      {SETUP_STEPS.map((step, i) => {
        const done = i < idx;
        const active = i === idx;
        return (
          <li
            key={step.id}
            className={cn(
              "flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium",
              active && "bg-primary text-white",
              done && "bg-primary/15 text-primary",
              !active && !done && "bg-muted/30 text-muted",
            )}
          >
            <span
              className={cn(
                "flex h-5 w-5 items-center justify-center rounded-full text-[10px]",
                active ? "bg-white/20" : "bg-background/60",
              )}
            >
              {done ? "✓" : i + 1}
            </span>
            <span className="hidden sm:inline">
              {labels[step.labelKey] ?? step.id}
            </span>
          </li>
        );
      })}
    </ol>
  );
}
