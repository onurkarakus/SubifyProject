import { cn } from "@/lib/utils";
import { HTMLAttributes } from "react";

export function Badge({
  className,
  variant = "default",
  ...props
}: HTMLAttributes<HTMLSpanElement> & {
  variant?: "default" | "warning" | "danger" | "success" | "muted";
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium",
        variant === "default" && "bg-primary/15 text-primary",
        variant === "warning" && "bg-warning/15 text-warning",
        variant === "danger" && "bg-danger/15 text-danger",
        variant === "success" && "bg-success/15 text-success",
        // muted token is a mid-gray; use it as text on a light/dark surface tint (not bg-muted + text-muted)
        variant === "muted" &&
          "border border-border bg-background text-foreground",
        className,
      )}
      {...props}
    />
  );
}
