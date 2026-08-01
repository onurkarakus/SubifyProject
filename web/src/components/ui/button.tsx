import { cn } from "@/lib/utils";
import { cva, type VariantProps } from "class-variance-authority";
import { ButtonHTMLAttributes, forwardRef } from "react";

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 font-medium transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background disabled:pointer-events-none disabled:opacity-50",
  {
    variants: {
      variant: {
        default:
          "rounded-full bg-primary text-white shadow-sm hover:bg-primary-hover hover:shadow-[var(--shadow-glow)]",
        secondary:
          "rounded-full border border-border bg-surface text-foreground hover:bg-primary-soft",
        danger:
          "rounded-full bg-danger text-white hover:opacity-90 shadow-sm",
        ghost:
          "rounded-lg text-foreground hover:bg-primary-soft hover:text-primary",
        outline:
          "rounded-full border border-border bg-transparent text-foreground hover:border-primary/40 hover:bg-primary-soft",
      },
      size: {
        default: "h-10 px-5 py-2 text-sm",
        sm: "h-8 px-3.5 text-xs",
        lg: "h-11 px-7 text-base",
        icon: "h-10 w-10 rounded-full",
      },
    },
    defaultVariants: { variant: "default", size: "default" },
  },
);

export interface ButtonProps
  extends ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, ...props }, ref) => (
    <button
      ref={ref}
      className={cn(buttonVariants({ variant, size }), className)}
      {...props}
    />
  ),
);
Button.displayName = "Button";
