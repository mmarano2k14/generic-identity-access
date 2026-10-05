import type { ReactNode } from "react";
import type { IdentitySecurityAuditRecord } from "@generic-identity/contracts/security-operations";
import { IdentityEmptyState, IdentityPageFrame, IdentityStatus, IdentityTable } from "../components/index";

export interface SecurityAuditPageProps {
  readonly events: readonly IdentitySecurityAuditRecord[];
  readonly actions?: ReactNode;
}

function auditTone(outcome: IdentitySecurityAuditRecord["outcome"]): "positive" | "warning" | "negative" {
  if (outcome === "Succeeded") return "positive";
  if (outcome === "Denied") return "warning";
  return "negative";
}

/** Read-only view over server-produced security audit records. */
export function SecurityAuditPage({ events, actions }: SecurityAuditPageProps) {
  return (
    <IdentityPageFrame
      title="Security audit"
      description="Security-sensitive authentication and administration events emitted by the Identity backend."
      actions={actions}
    >
      {events.length === 0 ? <IdentityEmptyState title="No security events" /> : (
        <IdentityTable caption="Security audit events">
          <thead>
            <tr>
              <th scope="col">Time</th>
              <th scope="col">Event</th>
              <th scope="col">Outcome</th>
              <th scope="col">User</th>
              <th scope="col">Tenant</th>
              <th scope="col">Target</th>
              <th scope="col">Correlation</th>
            </tr>
          </thead>
          <tbody>
            {events.map((event) => (
              <tr key={event.eventId} data-gi-record-id={event.eventId}>
                <td>{event.occurredAt}</td>
                <td><code>{event.eventType}</code></td>
                <td><IdentityStatus label={event.outcome} tone={auditTone(event.outcome)} /></td>
                <td>{event.userId ? <code>{event.userId}</code> : "—"}</td>
                <td>{event.tenantId ? <code>{event.tenantId}</code> : "—"}</td>
                <td>{event.targetId ? <code>{event.targetId}</code> : "—"}</td>
                <td>{event.correlationId ? <code>{event.correlationId}</code> : "—"}</td>
              </tr>
            ))}
          </tbody>
        </IdentityTable>
      )}
    </IdentityPageFrame>
  );
}
