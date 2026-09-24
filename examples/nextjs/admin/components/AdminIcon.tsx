export type AdminIconName =
  | "overview"
  | "users"
  | "tenants"
  | "memberships"
  | "groups"
  | "policies"
  | "resource-scopes"
  | "mfa"
  | "sessions"
  | "scope-authority"
  | "search"
  | "plus"
  | "shield"
  | "lock"
  | "spark"
  | "arrow"
  | "menu"
  | "eye"
  | "eye-off"
  | "key"
  | "back"
  | "check";

/** Dependency-free icon set so the copyable administration module has no UI runtime dependency. */
export function AdminIcon({ name, className = "ia-icon" }: { readonly name: AdminIconName; readonly className?: string }) {
  const common = {
    className,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.8,
    strokeLinecap: "round" as const,
    strokeLinejoin: "round" as const,
    "aria-hidden": true,
  };

  switch (name) {
    case "overview":
      return <svg {...common}><rect x="3" y="3" width="7" height="7" rx="2"/><rect x="14" y="3" width="7" height="7" rx="2"/><rect x="3" y="14" width="7" height="7" rx="2"/><rect x="14" y="14" width="7" height="7" rx="2"/></svg>;
    case "users":
      return <svg {...common}><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>;
    case "tenants":
      return <svg {...common}><path d="M3 21h18"/><path d="M5 21V7l7-4 7 4v14"/><path d="M9 9h1"/><path d="M14 9h1"/><path d="M9 13h1"/><path d="M14 13h1"/><path d="M10 21v-4h4v4"/></svg>;
    case "memberships":
      return <svg {...common}><circle cx="8" cy="8" r="3"/><path d="M3 21v-2a5 5 0 0 1 10 0v2"/><path d="M16 11h5"/><path d="M18.5 8.5v5"/></svg>;
    case "groups":
      return <svg {...common}><circle cx="7" cy="8" r="3"/><circle cx="17" cy="8" r="3"/><path d="M2.5 21v-1.5A5.5 5.5 0 0 1 8 14h0"/><path d="M21.5 21v-1.5A5.5 5.5 0 0 0 16 14h0"/><path d="M9.5 21v-2a5 5 0 0 1 5-5"/></svg>;
    case "policies":
      return <svg {...common}><path d="M6 3h9l3 3v15H6z"/><path d="M14 3v4h4"/><path d="M9 12h6"/><path d="M9 16h6"/><path d="M9 8h1"/></svg>;
    case "resource-scopes":
      return <svg {...common}><circle cx="12" cy="5" r="2"/><circle cx="6" cy="18" r="2"/><circle cx="18" cy="18" r="2"/><path d="M12 7v4"/><path d="M6 16v-2h12v2"/></svg>;
    case "mfa":
      return <svg {...common}><path d="M12 3l7 3v5c0 4.9-3 8.2-7 10-4-1.8-7-5.1-7-10V6z"/><circle cx="12" cy="11" r="2"/><path d="M12 13v3"/></svg>;
    case "sessions":
      return <svg {...common}><rect x="3" y="4" width="18" height="13" rx="2"/><path d="M8 21h8"/><path d="M12 17v4"/><path d="M8 9h8"/><path d="M8 12h5"/></svg>;
    case "scope-authority":
      return <svg {...common}><path d="M12 3l7 3v5c0 4.9-3 8.2-7 10-4-1.8-7-5.1-7-10V6z"/><path d="M9 12l2 2 4-4"/></svg>;
    case "search":
      return <svg {...common}><circle cx="11" cy="11" r="7"/><path d="M20 20l-4-4"/></svg>;
    case "plus":
      return <svg {...common}><path d="M12 5v14"/><path d="M5 12h14"/></svg>;
    case "shield":
      return <svg {...common}><path d="M12 3l7 3v5c0 4.9-3 8.2-7 10-4-1.8-7-5.1-7-10V6z"/></svg>;
    case "lock":
      return <svg {...common}><rect x="5" y="10" width="14" height="11" rx="2"/><path d="M8 10V7a4 4 0 0 1 8 0v3"/></svg>;
    case "spark":
      return <svg {...common}><path d="M12 3l1.3 4.2L17 9l-3.7 1.8L12 15l-1.3-4.2L7 9l3.7-1.8z"/><path d="M18.5 15l.7 2.3 2.3.7-2.3.7-.7 2.3-.7-2.3-2.3-.7 2.3-.7z"/></svg>;
    case "arrow":
      return <svg {...common}><path d="M5 12h14"/><path d="M13 6l6 6-6 6"/></svg>;
    case "menu":
      return <svg {...common}><path d="M4 7h16"/><path d="M4 12h16"/><path d="M4 17h16"/></svg>;
    case "eye":
      return <svg {...common}><path d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6z"/><circle cx="12" cy="12" r="2.5"/></svg>;
    case "eye-off":
      return <svg {...common}><path d="M3 3l18 18"/><path d="M10.6 6.2A10.4 10.4 0 0 1 12 6c6 0 9.5 6 9.5 6a17 17 0 0 1-2.3 3"/><path d="M6.2 6.2A17.5 17.5 0 0 0 2.5 12s3.5 6 9.5 6a10 10 0 0 0 3.2-.5"/><path d="M10.4 10.4a2.3 2.3 0 0 0 3.2 3.2"/></svg>;
    case "key":
      return <svg {...common}><circle cx="7.5" cy="15.5" r="3.5"/><path d="M10 13l9-9"/><path d="M15 8l3 3"/><path d="M17 6l2 2"/></svg>;
    case "back":
      return <svg {...common}><path d="M19 12H5"/><path d="M11 18l-6-6 6-6"/></svg>;
    case "check":
      return <svg {...common}><path d="M5 12l4 4 10-10"/></svg>;
  }
}
