import type { ReactNode } from "react";

export interface AdminDetailField {
  readonly label: string;
  readonly value: ReactNode;
  readonly mono?: boolean;
}

export interface AdminDetailCardProps {
  readonly title: string;
  readonly description: string;
  readonly fields: readonly AdminDetailField[];
  readonly emptyMessage?: string;
}

/** Server-renderable structured details that avoid raw JSON presentation in the administration UI. */
export function AdminDetailCard({ title, description, fields, emptyMessage = "No record loaded." }: AdminDetailCardProps) {
  return (
    <article className="ia-detail-card">
      <header className="ia-detail-header">
        <div>
          <p className="ia-card-kicker">Record details</p>
          <h2>{title}</h2>
          <p>{description}</p>
        </div>
      </header>
      {fields.length === 0 ? (
        <div className="ia-detail-empty">{emptyMessage}</div>
      ) : (
        <dl className="ia-detail-grid">
          {fields.map((field) => (
            <div className="ia-detail-item" key={field.label}>
              <dt>{field.label}</dt>
              <dd className={field.mono ? "ia-mono" : undefined}>{field.value}</dd>
            </div>
          ))}
        </dl>
      )}
    </article>
  );
}
