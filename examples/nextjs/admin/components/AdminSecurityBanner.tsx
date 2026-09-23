import { AdminIcon } from "./AdminIcon";

/** Server-renderable reminder that UI visibility never replaces server-side authorization. */
export function AdminSecurityBanner({ title, description }: { readonly title: string; readonly description: string }) {
  return (
    <aside className="ia-security-banner">
      <span className="ia-security-banner-icon"><AdminIcon name="shield" /></span>
      <div>
        <strong>{title}</strong>
        <p>{description}</p>
      </div>
    </aside>
  );
}
