import type { ReactNode } from "react";
import type { IdentitySecurityAuditRecord } from "@generic-identity/contracts/security-operations";
import {
  IdentityEmptyState,
  IdentityPageFrame,
  IdentityPanel,
  IdentityStatus,
} from "../components/index";

export interface SecurityAuditSummary {
  readonly total: number;
  readonly succeeded: number;
  readonly denied: number;
  readonly failed: number;
}

export interface SecurityAuditPageProps {
  readonly events: readonly IdentitySecurityAuditRecord[];
  readonly summary?: SecurityAuditSummary;
  readonly filters?: ReactNode;
  readonly actions?: ReactNode;
  readonly userHref?: (userId: string) => string;
  readonly tenantHref?: (tenantId: string) => string;
  readonly correlationHref?: (correlationId: string) => string;
}

function auditTone(outcome: IdentitySecurityAuditRecord["outcome"]): "positive" | "warning" | "negative" {
  if (outcome === "Succeeded") return "positive";
  if (outcome === "Denied") return "warning";
  return "negative";
}

function category(eventType: string): string {
  if (eventType.startsWith("Oidc")) return "OIDC";
  if (
    eventType.includes("Authenticator")
    || eventType.includes("AuthenticationFactor")
    || eventType.includes("Mfa")
  ) return "MFA";
  if (eventType.includes("Password") || eventType.includes("Login")) return "Authentication";
  if (eventType.includes("Session")) return "Sessions";
  if (
    eventType.includes("Policy")
    || eventType.includes("Group")
    || eventType.includes("Scope")
    || eventType.includes("Tenant")
    || eventType.includes("User")
  ) return "Administration";
  return "Security";
}

function label(value: string): string {
  return value.replace(/([a-z0-9])([A-Z])/gu, "$1 $2");
}

function summarize(events: readonly IdentitySecurityAuditRecord[]): SecurityAuditSummary {
  return {
    total: events.length,
    succeeded: events.filter((event) => event.outcome === "Succeeded").length,
    denied: events.filter((event) => event.outcome === "Denied").length,
    failed: events.filter((event) => event.outcome === "Failed").length,
  };
}

/** Read-only GOLDEN-style view over bounded server-produced Security Audit evidence. */
export function SecurityAuditPage({
  events,
  summary = summarize(events),
  filters,
  actions,
  userHref,
  tenantHref,
  correlationHref,
}: SecurityAuditPageProps) {
  return (
    <IdentityPageFrame
      title="Security audit"
      description="Inspect bounded, newest-first security events recorded by the current Identity application without exposing credentials, tokens, secrets, or arbitrary payloads."
      actions={actions}
    >
      <IdentityPanel
        title="Read-only application-scoped evidence"
        description="The trusted Identity backend resolves storage server-side and returns only categorical security metadata already persisted for the current identity scope and application."
      >
        <p className="gi-field-hint">
          Audit filters never grant access to another tenant, application, database route, credential, or secret.
        </p>
      </IdentityPanel>

      {filters}

      <section className="gi-audit-summary" aria-label="Security audit summary">
        <AuditMetric label="Visible events" value={summary.total} />
        <AuditMetric label="Succeeded" value={summary.succeeded} tone="positive" />
        <AuditMetric label="Denied" value={summary.denied} tone="warning" />
        <AuditMetric label="Failed" value={summary.failed} tone="negative" />
      </section>

      {events.length === 0 ? (
        <IdentityEmptyState
          title="No audit events in this window"
          description="Adjust the filters or increase the bounded result window. Audit reads never fall back to another application or database destination."
        />
      ) : (
        <section className="gi-audit-timeline" aria-label="Security audit events">
          {events.map((event) => (
            <article className="gi-audit-event" key={event.eventId} data-gi-record-id={event.eventId}>
              <header className="gi-audit-event-heading">
                <div>
                  <div className="gi-audit-event-meta">
                    <span className="gi-audit-category">{category(event.eventType)}</span>
                    <IdentityStatus label={event.outcome} tone={auditTone(event.outcome)} />
                  </div>
                  <h2 className="gi-audit-event-title">{label(event.eventType)}</h2>
                </div>
                <time dateTime={event.occurredAt}>{new Date(event.occurredAt).toLocaleString()}</time>
              </header>

              <dl className="gi-audit-event-facts">
                <AuditFact label="Event ID"><code>{event.eventId}</code></AuditFact>
                {event.userId ? (
                  <AuditFact label="User">
                    {userHref ? <a href={userHref(event.userId)}>{event.userId}</a> : <code>{event.userId}</code>}
                  </AuditFact>
                ) : null}
                {event.tenantId ? (
                  <AuditFact label="Tenant">
                    {tenantHref ? <a href={tenantHref(event.tenantId)}>{event.tenantId}</a> : <code>{event.tenantId}</code>}
                  </AuditFact>
                ) : null}
                {event.clientId ? <AuditFact label="Client"><code>{event.clientId}</code></AuditFact> : null}
                {event.targetId ? <AuditFact label="Target"><code>{event.targetId}</code></AuditFact> : null}
                {event.reasonCode ? <AuditFact label="Reason">{label(event.reasonCode)}</AuditFact> : null}
                {event.correlationId ? (
                  <AuditFact label="Correlation">
                    {correlationHref
                      ? <a href={correlationHref(event.correlationId)}><code>{event.correlationId}</code></a>
                      : <code>{event.correlationId}</code>}
                  </AuditFact>
                ) : null}
              </dl>
            </article>
          ))}
        </section>
      )}
    </IdentityPageFrame>
  );
}

function AuditMetric({
  label: metricLabel,
  value,
  tone = "neutral",
}: {
  readonly label: string;
  readonly value: number;
  readonly tone?: "positive" | "warning" | "negative" | "neutral";
}) {
  return (
    <div className={`gi-audit-metric gi-audit-metric-${tone}`}>
      <strong>{value}</strong>
      <span>{metricLabel}</span>
    </div>
  );
}

function AuditFact({ label: factLabel, children }: { readonly label: string; readonly children: ReactNode }) {
  return <div><dt>{factLabel}</dt><dd>{children}</dd></div>;
}
