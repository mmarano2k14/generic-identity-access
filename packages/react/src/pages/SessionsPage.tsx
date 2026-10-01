import type { ReactNode } from "react";
import type { IdentitySessionValidationResult } from "@generic-identity/contracts";
import { IdentityEmptyState, IdentityPageFrame, IdentityStatus, IdentityTable } from "../components/index";

export interface SessionsPageProps {
  readonly sessions: readonly IdentitySessionValidationResult[];
  readonly currentSessionId?: string;
  readonly actions?: ReactNode;
  readonly renderSessionActions?: (session: IdentitySessionValidationResult) => ReactNode;
}

export function SessionsPage({ sessions, currentSessionId, actions, renderSessionActions }: SessionsPageProps) {
  return (
    <IdentityPageFrame title="Sessions" description="Validated session metadata. Session credentials are never rendered." actions={actions}>
      {sessions.length === 0 ? <IdentityEmptyState title="No sessions" /> : (
        <IdentityTable caption="Sessions">
          <thead><tr><th scope="col">Session ID</th><th scope="col">User</th><th scope="col">Assurance</th><th scope="col">Expires</th><th scope="col">Current</th>{renderSessionActions ? <th scope="col">Actions</th> : null}</tr></thead>
          <tbody>{sessions.map((session) => <tr key={session.sessionId} data-gi-record-id={session.sessionId}><td><code>{session.sessionId}</code></td><td><code>{session.userId}</code></td><td>{session.assurance.level}</td><td>{session.expiresAt}</td><td>{session.sessionId === currentSessionId ? <IdentityStatus label="Current" tone="positive" /> : "—"}</td>{renderSessionActions ? <td>{renderSessionActions(session)}</td> : null}</tr>)}</tbody>
        </IdentityTable>
      )}
    </IdentityPageFrame>
  );
}
