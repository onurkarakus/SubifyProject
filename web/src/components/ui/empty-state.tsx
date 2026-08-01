import { cn } from "@/lib/utils";
import { Inbox } from "lucide-react";
import { HTMLAttributes } from "react";

export function EmptyState({
  title,
  description,
  className,
  action,
  ...props
}: HTMLAttributes<HTMLDivElement> & {
  title: string;
  description?: string;
  action?: React.ReactNode;
}) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center gap-3 rounded-xl border border-dashed border-border bg-surface/50 px-6 py-12 text-center",
        className,
      )}
      {...props}
    >
      <Inbox className="h-10 w-10 text-muted" />
      <div>
        <p className="font-medium text-foreground">{title}</p>
        {description ? (
          <p className="mt-1 text-sm text-muted">{description}</p>
        ) : null}
      </div>
      {action}
    </div>
  );
}
