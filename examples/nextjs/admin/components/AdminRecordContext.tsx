import Link from "next/link";
import type { ReactNode } from "react";
import { AdminIcon } from "./AdminIcon";

export interface AdminRecordContextFact {
  readonly label: string;
  readonly value: ReactNode;
  readonly mono?: boolean;
}

export interface AdminRecordContextProps {
  readonly kicker: string;
  readonly title: string;
  readonly description: string;
  readonly identifier: string;
  readonly status?: string;
  readonly version?: number;
  readonly facts?: readonly AdminRecordContextFact[];
  readonly actions?: ReactNode;
  readonly closeHref?: string;
  readonly closeLabel?: string;
}

function statusClassName(status: string): string {
  return status === "Active"
    ? "ia-status ia-status-active"
    : status === "Inactive" || status === "Revoked"
      ? "ia-status ia-status-inactive"
      : "ia-status ia-status-neutral";
}

/** Server-renderable record context for collection -> detail -> relationship administration flows. */
export function AdminRecordContext({
  kicker,
  title,
  description,
  identifier,
  status,
  version,
  facts = [],
  actions,
  closeHref,
  closeLabel = "Back to collection",
}: AdminRecordContextProps) {
  return (
    <section className="ia-record-context" aria-label={`${title} record context`}>
      <div className="ia-record-context-main">
        <div className="ia-record-context-heading">
          <span className="ia-record-context-icon" aria-hidden="true"><AdminIcon name="manage" /></span>
          <div>
            <p className="ia-card-kicker">{kicker}</p>
            <h2>{title}</h2>
            <p>{description}</p>
          </div>
        </div>
        <div className="ia-record-context-meta">
          <code className="ia-id-chip" title={identifier}>{identifier}</code>
          {status ? <span className={statusClassName(status)}><span className="ia-status-dot" aria-hidden="true" />{status}</span> : null}
          {version !== undefined ? <span className="ia-version-chip">v{version}</span> : null}
        </div>
      </div>

      {facts.length > 0 ? (
        <dl className="ia-record-context-facts">
          {facts.map((fact) => (
            <div key={fact.label}>
              <dt>{fact.label}</dt>
              <dd className={fact.mono ? "ia-mono" : undefined}>{fact.value}</dd>
            </div>
          ))}
        </dl>
      ) : null}

      {(actions || closeHref) ? (
        <div className="ia-record-context-actions">
          {actions}
          {closeHref ? <Link className="ia-button ia-button-secondary ia-button-compact" href={closeHref}><AdminIcon name="back" />{closeLabel}</Link> : null}
        </div>
      ) : null}
    </section>
  );
}
