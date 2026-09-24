"use client";

import { usePathname } from "next/navigation";
import type { IdentityAccessAdminUiEntry } from "@identity-access/client";

export function AdminCurrentSection({ entries }: { readonly entries: readonly IdentityAccessAdminUiEntry[] }) {
  const pathname = usePathname();
  const current = pathname === "/identity"
    ? "Overview"
    : entries.find((entry) => pathname === entry.href || pathname.startsWith(`${entry.href}/`))?.label ?? "Administration";

  return (
    <div className="ia-topbar-context-copy">
      <strong>{current}</strong>
      <small>Protected Identity Access workspace</small>
    </div>
  );
}
