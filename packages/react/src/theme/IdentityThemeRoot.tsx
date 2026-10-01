import type { HTMLAttributes, ReactNode } from "react";

export interface IdentityThemeRootProps extends HTMLAttributes<HTMLDivElement> {
  readonly children: ReactNode;
}

function joinClasses(...classes: Array<string | undefined | false>): string {
  return classes.filter(Boolean).join(" ");
}

/**
 * Optional theme boundary. Consumers may attach their own class and override
 * documented --gi-* CSS variables without forking shared page behavior.
 */
export function IdentityThemeRoot({ className, children, ...props }: IdentityThemeRootProps) {
  return (
    <div
      {...props}
      className={joinClasses("gi-root", className)}
      data-gi-component="root"
    >
      {children}
    </div>
  );
}
