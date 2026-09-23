import type { ReactNode } from "react";

/** Server Component providing consistent page hierarchy and an optional action area. */
export function AdminPageHeader({
  title,
  description,
  actions,
}: {
  readonly title: string;
  readonly description: string;
  readonly actions?: ReactNode;
}) {
  return (
    <header className="ia-page-header">
      <div>
        <p className="ia-eyebrow">Identity Access</p>
        <h1>{title}</h1>
        <p className="ia-page-description">{description}</p>
      </div>
      {actions ? <div className="ia-page-actions">{actions}</div> : null}
    </header>
  );
}
