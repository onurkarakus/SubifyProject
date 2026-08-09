"use client";

import { cn } from "@/lib/utils";
import { Info } from "lucide-react";
import {
  useCallback,
  useEffect,
  useId,
  useRef,
  useState,
  type ReactNode,
} from "react";

type InfoTipProps = {
  /** Tooltip body — short, 1–2 sentences */
  children: ReactNode;
  /** Accessible name for the icon button */
  label?: string;
  className?: string;
  /** Preferred placement relative to the icon */
  side?: "top" | "bottom";
};

/**
 * Industry-style field help: small (i) next to a label.
 * Opens on hover, keyboard focus, or click (touch); Escape / outside click closes.
 * Icon is intentionally a separate control so it does not toggle parent checkboxes.
 */
export function InfoTip({
  children,
  label = "More information",
  className,
  side = "bottom",
}: InfoTipProps) {
  const tipId = useId();
  const rootRef = useRef<HTMLSpanElement>(null);
  const [sticky, setSticky] = useState(false);
  const [hovered, setHovered] = useState(false);
  const [focused, setFocused] = useState(false);

  const open = sticky || hovered || focused;

  const closeAll = useCallback(() => {
    setSticky(false);
    setHovered(false);
    setFocused(false);
  }, []);

  useEffect(() => {
    if (!open) return;

    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") closeAll();
    }
    function onPointerDown(e: MouseEvent | TouchEvent) {
      if (rootRef.current && !rootRef.current.contains(e.target as Node)) {
        closeAll();
      }
    }

    document.addEventListener("keydown", onKey);
    document.addEventListener("mousedown", onPointerDown);
    document.addEventListener("touchstart", onPointerDown);
    return () => {
      document.removeEventListener("keydown", onKey);
      document.removeEventListener("mousedown", onPointerDown);
      document.removeEventListener("touchstart", onPointerDown);
    };
  }, [open, closeAll]);

  return (
    <span
      ref={rootRef}
      className={cn("relative inline-flex shrink-0 items-center", className)}
    >
      <button
        type="button"
        className={cn(
          "inline-flex h-5 w-5 items-center justify-center rounded-full text-muted",
          "hover:bg-muted/30 hover:text-foreground",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary",
          open && "bg-muted/30 text-foreground",
        )}
        aria-label={label}
        aria-expanded={open}
        aria-controls={tipId}
        onClick={(e) => {
          e.preventDefault();
          e.stopPropagation();
          setSticky((v) => !v);
        }}
        onMouseEnter={() => setHovered(true)}
        onMouseLeave={() => setHovered(false)}
        onFocus={() => setFocused(true)}
        onBlur={() => setFocused(false)}
      >
        <Info className="h-3.5 w-3.5" aria-hidden strokeWidth={2} />
      </button>
      {open ? (
        <span
          id={tipId}
          role="tooltip"
          className={cn(
            "absolute z-50 w-max max-w-[17rem] rounded-md border border-border bg-surface px-2.5 py-1.5",
            "text-left text-xs font-normal leading-snug text-foreground shadow-md",
            // Prefer left alignment so long tips near form edges stay readable
            "left-0",
            side === "top" ? "bottom-full mb-1.5" : "top-full mt-1.5",
          )}
        >
          {children}
        </span>
      ) : null}
    </span>
  );
}

type LabelWithInfoProps = {
  children: ReactNode;
  info?: ReactNode;
  infoLabel?: string;
  htmlFor?: string;
  className?: string;
};

/**
 * Form label row: text + optional info icon (GitHub / AWS settings pattern).
 */
export function LabelWithInfo({
  children,
  info,
  infoLabel,
  htmlFor,
  className,
}: LabelWithInfoProps) {
  return (
    <div
      className={cn(
        "flex items-center gap-1.5 text-sm font-medium text-foreground",
        className,
      )}
    >
      <label htmlFor={htmlFor} className="cursor-default">
        {children}
      </label>
      {info ? <InfoTip label={infoLabel}>{info}</InfoTip> : null}
    </div>
  );
}
