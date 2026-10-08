import type { ReactNode } from "react";
import type { IdentitySecurityAuditRecord } from "@generic-identity/contracts/security-operations";
import {
  IdentityEmptyState,
  IdentityPageFrame,
  IdentityPanel,
  IdentityStatus,
} from "../components/index";

export type SessionSecurityEvidenceState = "available" | "forbidden" | "unavailable";

export interface SessionSecuritySummary {
  readonly total: number;
  readonly issued: number;
  readonly revocationEvents: number;
  readonly assuranceEvents: number;
  readonly continuityAlerts: number;
}

export interface SessionSecurityPageProps {
  readonly records: readonly IdentitySecurityAuditRecord[];
  readonly evidenceState: SessionSecurityEvidenceState;
  readonly summary: SessionSecuritySummary;
  readonly filters?: ReactNode;
  readonly operations?: ReactNode;
  readonly contextLinks?: ReactNode;
  readonly evidenceFailureTitle?: string;
  readonly evidenceFailureMessage?: string;
  readonly userHref?: (userId: string) => string;
  readonly mfaHref?: (userId: string) => string;
  readonly auditUserHref?: (userId: string) => string;
  readonly auditCorrelationHref?: (correlationId: string) => string;
}

function eventLabel(eventType: string): string {
  switch (eventType) {
    case "PasswordLoginSucceeded": return "Session issued";
    case "SessionRevoked": return "Session revoked";
    case "SessionRevocationFailed": return "Session revocation rejected";
    case "UserSessionsRevoked": return "User sessions contained";
    case "ClientSessionsRevoked": return "Client sessions contained";
    case "SessionAssuranceUpgraded": return "Session assurance upgraded";
    case "SessionAssuranceUpgradeFailed": return "Session assurance upgrade rejected";
    case "OidcRefreshTokenReuseDetected": return "Refresh-token reuse detected";
    case "OidcRefreshTokenFamilyRevoked": return "Refresh-token family revoked";
    default: return eventType.replace(/([a-z0-9])([A-Z])/gu, "$1 $2");
  }
}

function category(eventType: string): string {
  if (eventType === "PasswordLoginSucceeded") return "Issuance";
  if (eventType.includes("Assurance")) return "Assurance";
  if (eventType.startsWith("OidcRefreshToken")) return "Token continuity";
  return "Revocation";
}

function targetLabel(eventType: string): string {
  if (eventType === "UserSessionsRevoked") return "User target";
  if (eventType === "ClientSessionsRevoked") return "Client target";
  if (eventType.startsWith("OidcRefreshToken")) return "Token reference";
  return "Session reference";
}

function auditTone(outcome: IdentitySecurityAuditRecord["outcome"]): "positive" | "warning" | "negative" {
  if (outcome === "Succeeded") return "positive";
  if (outcome === "Denied") return "warning";
  return "negative";
}

/**
 * GOLDEN-style session-security workspace.
 *
 * This page intentionally does not accept an active-session collection because
 * the administration backend exposes containment operations and audit evidence,
 * not a session-list contract.
 */
export function SessionSecurityPage({
  records,
  evidenceState,
  summary,
  filters,
  operations,
  contextLinks,
  evidenceFailureTitle,
  evidenceFailureMessage,
  userHref,
  mfaHref,
  auditUserHref,
  auditCorrelationHref,
}: SessionSecurityPageProps) {
  return (
    <IdentityPageFrame
      title="Sessions"
      description="Investigate bounded session-security evidence and contain active sessions through server-authorized revocation contracts."
    >
      <IdentityPanel
        title="This is not an inferred active-session inventory"
        description="The administration API does not expose a session-list contract. This workspace shows secret-safe security evidence and real containment operations without guessing whether an unseen session is active, expired, or revoked."
      >
        {null}
      </IdentityPanel>

      {filters}
      {contextLinks}

      {evidenceState === "forbidden" ? (
        <IdentityPanel
          title="Session audit evidence is not available to this administrator"
          description="Containment may remain separately authorized, but security-audit metadata requires its own read capability and is not inferred or bypassed here."
        >
          {null}
        </IdentityPanel>
      ) : evidenceState === "unavailable" ? (
        <IdentityPanel
          title={evidenceFailureTitle ?? "Session audit evidence is temporarily unavailable"}
          description={evidenceFailureMessage ?? "The bounded evidence read failed. No session state is inferred from missing evidence."}
        >
          {null}
        </IdentityPanel>
      ) : (
        <>
          <section className="gi-audit-summary" aria-label="Session security summary">
            <SessionMetric label="Visible events" value={summary.total} />
            <SessionMetric label="Session issuance" value={summary.issued} tone="positive" />
            <SessionMetric label="Revocation activity" value={summary.revocationEvents} />
            <SessionMetric label="Continuity alerts" value={summary.continuityAlerts} tone="warning" />
          </section>

          {records.length === 0 ? (
            <IdentityEmptyState
              title="No session security activity in this window"
              description="Adjust the filters or broaden the bounded result window. This view does not infer an active-session inventory from missing audit evidence."
            />
          ) : (
            <section className="gi-audit-timeline" aria-label="Session security activity">
              {records.map((record) => (
                <article className="gi-audit-event" key={record.eventId} data-gi-record-id={record.eventId}>
                  <header className="gi-audit-event-heading">
                    <div>
                      <div className="gi-audit-event-meta">
                        <span className="gi-audit-category">{category(record.eventType)}</span>
                        <IdentityStatus label={record.outcome} tone={auditTone(record.outcome)} />
                      </div>
                      <h2 className="gi-audit-event-title">{eventLabel(record.eventType)}</h2>
                    </div>
                    <time dateTime={record.occurredAt}>{new Date(record.occurredAt).toLocaleString()}</time>
                  </header>

                  <dl className="gi-audit-event-facts">
                    {record.targetId ? <SessionFact label={targetLabel(record.eventType)}><code>{record.targetId}</code></SessionFact> : null}
                    {record.userId ? (
                      <SessionFact label="User">
                        {userHref ? <a href={userHref(record.userId)}>{record.userId}</a> : <code>{record.userId}</code>}
                      </SessionFact>
                    ) : null}
                    {record.clientId ? <SessionFact label="Client"><code>{record.clientId}</code></SessionFact> : null}
                    {record.reasonCode ? <SessionFact label="Reason">{record.reasonCode.replace(/([a-z0-9])([A-Z])/gu, "$1 $2")}</SessionFact> : null}
                    {record.correlationId ? (
                      <SessionFact label="Correlation">
                        {auditCorrelationHref
                          ? <a href={auditCorrelationHref(record.correlationId)}><code>{record.correlationId}</code></a>
                          : <code>{record.correlationId}</code>}
                      </SessionFact>
                    ) : null}
                  </dl>

                  {record.userId && (mfaHref || auditUserHref) ? (
                    <div className="gi-audit-filter-actions">
                      {mfaHref ? <a className="gi-button gi-button-secondary" href={mfaHref(record.userId)}>MFA</a> : null}
                      {auditUserHref ? <a className="gi-button gi-button-secondary" href={auditUserHref(record.userId)}>User audit</a> : null}
                    </div>
                  ) : null}
                </article>
              ))}
            </section>
          )}
        </>
      )}

      {operations ? (
        <section className="gi-session-containment">
          <IdentityPanel
            title="Server-confirmed revocation operations"
            description="These actions use only the revocation contracts exposed by the API. No session-delete or refresh-token administration endpoint is synthesized."
          >
            {operations}
          </IdentityPanel>
        </section>
      ) : null}
    </IdentityPageFrame>
  );
}

function SessionMetric({
  label,
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
      <span>{label}</span>
    </div>
  );
}

function SessionFact({ label, children }: { readonly label: string; readonly children: ReactNode }) {
  return <div><dt>{label}</dt><dd>{children}</dd></div>;
}
