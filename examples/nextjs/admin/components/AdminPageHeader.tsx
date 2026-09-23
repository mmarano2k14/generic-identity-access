import type { ReactNode } from "react";
import { AdminIcon } from "./AdminIcon";

/** Server Component providing consistent premium page hierarchy and an optional action area. */
export function AdminPageHeader({
  title,
  description,
  actions,
  eyebrow = "Identity Access",
  badge,
}: {
  readonly title: string;
  readonly description: string;
  readonly actions?: ReactNode;
  readonly eyebrow?: string;
  readonly badge?: string;
}) {
  return (
    <header className="ia-page-header">
      <div className="ia-page-header-copy">
        <div className="ia-page-header-meta">
          <p className="ia-eyebrow">{eyebrow}</p>
          {badge ? <span className="ia-header-badge"><AdminIcon name="spark" />{badge}</span> : null}
        </div>
        <h1>{title}</h1>
        <p className="ia-page-description">{description}</p>
      </div>
      {actions ? <div className="ia-page-actions">{actions}</div> : null}
    </header>
  );
}
