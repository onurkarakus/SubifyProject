import { cn } from "@/lib/utils";
import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";

export function StatCard({
  label,
  value,
  hint,
  icon: Icon,
  className,
}: {
  label: string;
  value: ReactNode;
  hint?: ReactNode;
  icon: LucideIcon;
  className?: string;
}) {
  return (
    <div className={cn("stat-card p-4 md:p-5", className)}>
      <div className="relative z-[1] flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="text-sm text-muted">{label}</p>
          <div className="mt-2 text-2xl font-bold tracking-tight md:text-[1.75rem]">
            {value}
          </div>
          {hint ? <div className="mt-1.5 text-xs">{hint}</div> : null}
        </div>
        <span className="inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-primary-soft text-primary">
          <Icon className="h-5 w-5" />
        </span>
      </div>
    </div>
  );
}
