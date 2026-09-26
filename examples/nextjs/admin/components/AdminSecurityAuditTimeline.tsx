import Link from "next/link";
import type { IdentitySecurityAuditRecord } from "@identity-access/client";
import { IdentityAccessAdminSecurityAuditPresentation } from "../server/IdentityAccessAdminSecurityAuditPresentation";
import { AdminEmptyState } from "./AdminEmptyState";

export interface AdminSecurityAuditTimelineProps {
  readonly records: readonly IdentitySecurityAuditRecord[];
}

/** Server-renderable timeline for secret-safe audit metadata returned by the authorized API. */
export function AdminSecurityAuditTimeline({ records }: AdminSecurityAuditTimelineProps) {
  if (records.length === 0) {
    return <AdminEmptyState title="No audit events in this window" description="Adjust the filters or increase the bounded result window. Audit reads never fall back to another application or database destination." />;
  }

  return (
    <section className="ia-audit-timeline" aria-label="Security audit events">
      {records.map((record) => (
        <article className="ia-audit-event" key={record.eventId}>
          <div className="ia-audit-event-marker" aria-hidden="true" />
          <div className="ia-audit-event-body">
            <div className="ia-audit-event-heading">
              <div>
                <div className="ia-audit-event-meta">
                  <span className="ia-audit-category">{IdentityAccessAdminSecurityAuditPresentation.category(record.eventType)}</span>
                  <span className={IdentityAccessAdminSecurityAuditPresentation.outcomeClassName(record.outcome)}>{IdentityAccessAdminSecurityAuditPresentation.outcomeLabel(record.outcome)}</span>
                </div>
                <h2>{IdentityAccessAdminSecurityAuditPresentation.eventLabel(record.eventType)}</h2>
              </div>
              <time dateTime={record.occurredAt}>{new Date(record.occurredAt).toLocaleString()}</time>
            </div>

            <dl className="ia-audit-event-facts">
              <div><dt>Event ID</dt><dd><code>{record.eventId}</code></dd></div>
              {record.userId ? <div><dt>User</dt><dd><Link href={`/identity/users?userId=${encodeURIComponent(record.userId)}`}>{record.userId}</Link></dd></div> : null}
              {record.tenantId ? <div><dt>Tenant</dt><dd><Link href={`/identity/tenants?tenantId=${encodeURIComponent(record.tenantId)}`}>{record.tenantId}</Link></dd></div> : null}
              {record.clientId ? <div><dt>Client</dt><dd><code>{record.clientId}</code></dd></div> : null}
              {record.targetId ? <div><dt>Target</dt><dd><code>{record.targetId}</code></dd></div> : null}
              {record.reasonCode ? <div><dt>Reason</dt><dd>{IdentityAccessAdminSecurityAuditPresentation.eventLabel(record.reasonCode)}</dd></div> : null}
              {record.correlationId ? <div><dt>Correlation</dt><dd><Link href={`/identity/security-audit?correlationId=${encodeURIComponent(record.correlationId)}`}><code>{record.correlationId}</code></Link></dd></div> : null}
            </dl>
          </div>
        </article>
      ))}
    </section>
  );
}
