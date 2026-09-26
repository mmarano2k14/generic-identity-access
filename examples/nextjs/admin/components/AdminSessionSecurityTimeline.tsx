import Link from "next/link";
import type { IdentitySecurityAuditRecord } from "@identity-access/client";
import { IdentityAccessAdminSessionPresentation } from "../server/IdentityAccessAdminSessionPresentation";
import { AdminEmptyState } from "./AdminEmptyState";
import { AdminIcon } from "./AdminIcon";

export interface AdminSessionSecurityTimelineProps {
  readonly records: readonly IdentitySecurityAuditRecord[];
}

/** Server-renderable session-security evidence timeline over secret-safe audit metadata. */
export function AdminSessionSecurityTimeline({ records }: AdminSessionSecurityTimelineProps) {
  if (records.length === 0) {
    return <AdminEmptyState title="No session security activity in this window" description="Adjust the filters or broaden the bounded result window. This view does not infer an active session inventory from missing audit evidence." />;
  }

  return (
    <section className="ia-audit-timeline ia-session-activity" aria-label="Session security activity">
      {records.map((record) => (
        <article className="ia-audit-event" key={record.eventId}>
          <div className="ia-audit-event-marker" aria-hidden="true" />
          <div className="ia-audit-event-body">
            <div className="ia-audit-event-heading">
              <div>
                <div className="ia-audit-event-meta">
                  <span className="ia-audit-category">{IdentityAccessAdminSessionPresentation.category(record.eventType)}</span>
                  <span className={IdentityAccessAdminSessionPresentation.outcomeClassName(record.outcome)}>{record.outcome}</span>
                </div>
                <h2>{IdentityAccessAdminSessionPresentation.eventLabel(record.eventType)}</h2>
              </div>
              <time dateTime={record.occurredAt}>{new Date(record.occurredAt).toLocaleString()}</time>
            </div>

            <dl className="ia-audit-event-facts">
              {record.targetId ? <div><dt>{IdentityAccessAdminSessionPresentation.targetLabel(record.eventType)}</dt><dd><code>{record.targetId}</code></dd></div> : null}
              {record.userId ? <div><dt>User</dt><dd><Link href={`/identity/users?userId=${encodeURIComponent(record.userId)}`}>{record.userId}</Link></dd></div> : null}
              {record.clientId ? <div><dt>Client</dt><dd><code>{record.clientId}</code></dd></div> : null}
              {record.reasonCode ? <div><dt>Reason</dt><dd>{record.reasonCode.replace(/([a-z0-9])([A-Z])/gu, "$1 $2")}</dd></div> : null}
              {record.correlationId ? <div><dt>Correlation</dt><dd><Link href={`/identity/security-audit?correlationId=${encodeURIComponent(record.correlationId)}`}><code>{record.correlationId}</code></Link></dd></div> : null}
            </dl>

            <div className="ia-session-event-actions">
              {record.userId ? <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/mfa?userId=${encodeURIComponent(record.userId)}`}><AdminIcon name="mfa" />MFA</Link> : null}
              {record.userId ? <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/security-audit?userId=${encodeURIComponent(record.userId)}`}><AdminIcon name="audit" />User audit</Link> : null}
              {record.correlationId ? <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/security-audit?correlationId=${encodeURIComponent(record.correlationId)}`}><AdminIcon name="search" />Correlation</Link> : null}
            </div>
          </div>
        </article>
      ))}
    </section>
  );
}
