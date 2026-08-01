import { cn } from "@/lib/utils";

/** Subify mark — S monogram matching UI mockups */
export function SubifyLogo({
  className,
  markClassName,
  showWordmark = true,
  wordmark = "Subify",
}: {
  className?: string;
  markClassName?: string;
  showWordmark?: boolean;
  wordmark?: string;
}) {
  return (
    <span className={cn("inline-flex items-center gap-2.5", className)}>
      <span
        className={cn(
          "inline-flex h-9 w-9 items-center justify-center rounded-xl bg-primary text-sm font-bold text-white shadow-[var(--shadow-glow)]",
          markClassName,
        )}
        aria-hidden
      >
        S
      </span>
      {showWordmark ? (
        <span className="text-lg font-bold tracking-tight text-foreground">
          {wordmark}
        </span>
      ) : null}
    </span>
  );
}
