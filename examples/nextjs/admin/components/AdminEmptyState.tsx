export interface AdminEmptyStateProps {
  readonly title: string;
  readonly description: string;
}

/** Shared server-renderable empty state for administration collections. */
export function AdminEmptyState({ title, description }: AdminEmptyStateProps) {
  return (
    <div className="ia-empty-state">
      <span aria-hidden="true" className="ia-empty-state-mark">◇</span>
      <h3>{title}</h3>
      <p>{description}</p>
    </div>
  );
}
