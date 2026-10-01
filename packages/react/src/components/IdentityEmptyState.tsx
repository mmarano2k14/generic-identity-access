import type { ReactNode } from "react";

export interface IdentityEmptyStateProps {
  readonly title: string;
  readonly description?: string;
  readonly action?: ReactNode;
}

export function IdentityEmptyState({ title, description, action }: IdentityEmptyStateProps) {
  return (
    <div className="gi-empty-state" data-gi-component="empty-state">
      <strong className="gi-empty-state-title">{title}</strong>
      {description ? <p className="gi-empty-state-description">{description}</p> : null}
      {action ? <div className="gi-empty-state-action">{action}</div> : null}
    </div>
  );
}
