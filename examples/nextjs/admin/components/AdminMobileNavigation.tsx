"use client";

import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import { usePathname } from "next/navigation";
import { AdminIcon } from "./AdminIcon";

export interface AdminMobileNavigationProps {
  readonly children: ReactNode;
}

/** Client-only disclosure state for the mobile navigation shell; route visibility remains server-owned. */
export function AdminMobileNavigation({ children }: AdminMobileNavigationProps) {
  const pathname = usePathname();
  const details = useRef<HTMLDetailsElement>(null);
  const [open, setOpen] = useState(false);
  const popoverId = useId();

  useEffect(() => {
    if (details.current?.open) details.current.open = false;
    setOpen(false);
  }, [pathname]);

  return (
    <details
      className="ia-mobile-navigation"
      ref={details}
      onToggle={(event) => setOpen(event.currentTarget.open)}
    >
      <summary
        className="ia-icon-button ia-mobile-navigation-trigger"
        aria-label={open ? "Close administration navigation" : "Open administration navigation"}
        aria-expanded={open}
        aria-controls={popoverId}
      >
        <AdminIcon name="menu" />
      </summary>
      <div className="ia-mobile-navigation-popover" id={popoverId}>
        {children}
      </div>
    </details>
  );
}
