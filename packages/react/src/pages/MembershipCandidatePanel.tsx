import type { ReactNode } from "react";
import type { IdentityTenantMembershipCandidateRecord } from "@generic-identity/contracts";
import { IdentityPanel, IdentityStatus } from "../components/index";

export interface MembershipCandidatePanelProps {
  readonly candidate: IdentityTenantMembershipCandidateRecord | null;
  readonly actions?: ReactNode;
}

export function MembershipCandidatePanel({ candidate, actions }: MembershipCandidatePanelProps) {
  if (!candidate) return <IdentityPanel title="Membership candidate"><p className="gi-muted">No matching identity candidate.</p></IdentityPanel>;
  return (
    <IdentityPanel title="Membership candidate">
      <dl className="gi-definition-list"><div><dt>Display name</dt><dd>{candidate.displayName}</dd></div><div><dt>User ID</dt><dd><code>{candidate.userId}</code></dd></div><div><dt>User status</dt><dd><IdentityStatus label={candidate.userStatus === 1 ? "ACTIVE" : "INACTIVE"} tone={candidate.userStatus === 1 ? "positive" : "neutral"} /></dd></div><div><dt>Existing membership</dt><dd>{candidate.existingMembershipId ? <code>{candidate.existingMembershipId}</code> : "None"}</dd></div></dl>
      {actions ? <div className="gi-inline-actions">{actions}</div> : null}
    </IdentityPanel>
  );
}
