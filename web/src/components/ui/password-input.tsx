"use client";

import { cn } from "@/lib/utils";
import { Eye, EyeOff } from "lucide-react";
import {
  InputHTMLAttributes,
  forwardRef,
  useState,
} from "react";

type Props = Omit<InputHTMLAttributes<HTMLInputElement>, "type"> & {
  /** Accessible label when password is hidden (click to show) */
  showLabel?: string;
  /** Accessible label when password is visible (click to hide) */
  hideLabel?: string;
};

/**
 * Password field with show/hide (eye) toggle.
 */
export const PasswordInput = forwardRef<HTMLInputElement, Props>(
  (
    {
      className,
      showLabel = "Show password",
      hideLabel = "Hide password",
      ...props
    },
    ref,
  ) => {
    const [visible, setVisible] = useState(false);

    return (
      <div className="relative">
        <input
          ref={ref}
          type={visible ? "text" : "password"}
          className={cn(
            "flex h-10 w-full rounded-xl border border-border bg-surface px-3 py-2 pr-10 text-sm text-foreground placeholder:text-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-1 focus-visible:ring-offset-background disabled:cursor-not-allowed disabled:opacity-50",
            className,
          )}
          {...props}
        />
        <button
          type="button"
          tabIndex={-1}
          className="absolute right-2 top-1/2 -translate-y-1/2 rounded-md p-1 text-muted hover:bg-muted/30 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
          aria-label={visible ? hideLabel : showLabel}
          aria-pressed={visible}
          onClick={() => setVisible((v) => !v)}
        >
          {visible ? (
            <EyeOff className="h-4 w-4" aria-hidden />
          ) : (
            <Eye className="h-4 w-4" aria-hidden />
          )}
        </button>
      </div>
    );
  },
);
PasswordInput.displayName = "PasswordInput";
