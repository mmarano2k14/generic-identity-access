import type { ReactNode } from "react";
import type { IdentitySessionValidationResult, IdentityUserRecord } from "@generic-identity/contracts";
import { IdentityPageFrame, IdentityPanel, IdentityStatus } from "../components/index";
import { activeStatus } from "./internal";

export interface AccountPageProps {
  readonly user: IdentityUserRecord;
  readonly session?: IdentitySessionValidationResult | null;
  readonly actions?: ReactNode;
  readonly security?: ReactNode;
}

export function AccountPage({ user, session = null, actions, security }: AccountPageProps) {
  return (
    <IdentityPageFrame title="Account" description="Current account and session metadata." actions={actions}>
      <IdentityPanel title="Identity"><dl className="gi-definition-list"><div><dt>Display name</dt><dd>{user.displayName}</dd></div><div><dt>User ID</dt><dd><code>{user.userId}</code></dd></div><div><dt>Status</dt><dd>{activeStatus(user.status)}</dd></div></dl></IdentityPanel>
      <IdentityPanel title="Session">{session ? <dl className="gi-definition-list"><div><dt>Session ID</dt><dd><code>{session.sessionId}</code></dd></div><div><dt>Assurance</dt><dd><IdentityStatus label={session.assurance.level.toUpperCase()} tone={session.assurance.level === "mfa" ? "positive" : "neutral"} /></dd></div><div><dt>Expires</dt><dd>{session.expiresAt}</dd></div></dl> : <p className="gi-muted">No validated session metadata supplied.</p>}</IdentityPanel>
      {security ? <IdentityPanel title="Security">{security}</IdentityPanel> : null}
    </IdentityPageFrame>
  );
}
