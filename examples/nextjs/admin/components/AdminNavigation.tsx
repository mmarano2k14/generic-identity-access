import Link from "next/link";
import type { IdentityAccessAdminUiEntry } from "@identity-access/client";

/** Server Component rendering permission-filtered administration navigation. */
export function AdminNavigation({ entries }: { readonly entries: readonly IdentityAccessAdminUiEntry[] }) {
  return (
    <nav className="ia-navigation" aria-label="Identity administration">
      <div className="ia-navigation-brand">
        <span className="ia-navigation-mark" aria-hidden="true">IA</span>
        <div><strong>Identity Access</strong><small>Administration</small></div>
      </div>
      <ul className="ia-navigation-list">
        {entries.map((entry) => (
          <li key={entry.section}>
            <Link className="ia-navigation-link" href={entry.href}>
              <span>{entry.label}</span>
              <small>{entry.description}</small>
            </Link>
          </li>
        ))}
      </ul>
    </nav>
  );
}
