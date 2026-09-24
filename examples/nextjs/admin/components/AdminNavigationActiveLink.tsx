"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";

export interface AdminNavigationActiveLinkProps {
  readonly href: string;
  readonly className: string;
  readonly children: ReactNode;
}

/** Client-only route-state decoration. Authorization and navigation visibility remain server-owned. */
export function AdminNavigationActiveLink({ href, className, children }: AdminNavigationActiveLinkProps) {
  const pathname = usePathname();
  const active = href === "/identity"
    ? pathname === href
    : pathname === href || pathname.startsWith(`${href}/`);

  return (
    <Link
      className={`${className}${active ? " ia-navigation-link-active" : ""}`}
      href={href}
      aria-current={active ? "page" : undefined}
    >
      {children}
    </Link>
  );
}
