import type { IdentityAccessAdminUiEntry, IdentityAccessAdminUiSection } from "@identity-access/client";
import type { AdminIconName } from "./AdminIcon";
import { AdminIcon } from "./AdminIcon";
import { AdminNavigationActiveLink } from "./AdminNavigationActiveLink";

const ICONS: Readonly<Record<IdentityAccessAdminUiSection, AdminIconName>> = {
  users: "users",
  tenants: "tenants",
  memberships: "memberships",
  groups: "groups",
  policies: "policies",
  "resource-scopes": "resource-scopes",
  mfa: "mfa",
  sessions: "sessions",
  "scope-authority": "scope-authority",
};

function NavigationGroup({ label, entries }: { readonly label: string; readonly entries: readonly IdentityAccessAdminUiEntry[] }) {
  if (entries.length === 0) return null;
  return (
    <div className="ia-navigation-group">
      <p className="ia-navigation-group-label">{label}</p>
      <ul className="ia-navigation-list">
        {entries.map((entry) => (
          <li key={entry.section}>
            <AdminNavigationActiveLink className="ia-navigation-link" href={entry.href}>
              <span className="ia-navigation-link-icon"><AdminIcon name={ICONS[entry.section]} /></span>
              <span className="ia-navigation-link-copy">
                <span>{entry.label}</span>
                <small>{entry.description}</small>
              </span>
              <span className="ia-navigation-link-arrow"><AdminIcon name="arrow" /></span>
            </AdminNavigationActiveLink>
          </li>
        ))}
      </ul>
    </div>
  );
}

/** Server-rendered permission-filtered navigation with client-only active-route decoration. */
export function AdminNavigation({ entries, mobile = false }: { readonly entries: readonly IdentityAccessAdminUiEntry[]; readonly mobile?: boolean }) {
  const scopeEntries = entries.filter((entry) => !entry.tenantScoped && entry.section !== "mfa" && entry.section !== "sessions" && entry.section !== "scope-authority");
  const tenantEntries = entries.filter((entry) => entry.tenantScoped);
  const securityEntries = entries.filter((entry) => entry.section === "mfa" || entry.section === "sessions" || entry.section === "scope-authority");

  return (
    <nav className={`ia-navigation${mobile ? " ia-navigation-mobile-drawer" : ""}`} aria-label={mobile ? "Mobile identity administration" : "Identity administration"}>
      <div className="ia-navigation-top">
        <AdminNavigationActiveLink className="ia-navigation-brand" href="/identity">
          <span className="ia-navigation-mark" aria-hidden="true"><AdminIcon name="shield" /></span>
          <span className="ia-navigation-brand-copy"><strong>Identity Access</strong><small>Security control center</small></span>
        </AdminNavigationActiveLink>
        <div className="ia-navigation-status"><span className="ia-live-dot" aria-hidden="true" />Protected server context</div>
      </div>

      <AdminNavigationActiveLink className="ia-navigation-overview" href="/identity">
        <span className="ia-navigation-link-icon"><AdminIcon name="overview" /></span>
        <span>Overview</span>
        <span className="ia-navigation-link-arrow"><AdminIcon name="arrow" /></span>
      </AdminNavigationActiveLink>

      <div className="ia-navigation-groups">
        <NavigationGroup label="Directory" entries={scopeEntries} />
        <NavigationGroup label="Tenant access" entries={tenantEntries} />
        <NavigationGroup label="Security" entries={securityEntries} />
      </div>

      <div className="ia-navigation-footer">
        <span className="ia-navigation-footer-icon"><AdminIcon name="lock" /></span>
        <div><strong>Authorization enforced server-side</strong><small>Navigation visibility is presentation only.</small></div>
      </div>
    </nav>
  );
}
