import type { ReactNode } from "react";
import type { IdentityGroupMemberRecord, IdentityGroupRecord } from "@generic-identity/contracts";
import { IdentityEmptyState, IdentityPageFrame, IdentityPanel, IdentityStatus, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface GroupDetailsPageProps {
  readonly group: IdentityGroupRecord;
  readonly members?: readonly IdentityGroupMemberRecord[];
  readonly actions?: ReactNode;
}

export function GroupDetailsPage({ group, members = [], actions }: GroupDetailsPageProps) {
  return (
    <IdentityPageFrame title={group.displayName} description={`Group ${group.groupId}`} actions={actions}>
      <IdentityPanel title="Group">
        <dl className="gi-definition-list">
          <div><dt>Tenant</dt><dd><code>{group.tenantId}</code></dd></div>
          <div><dt>Status</dt><dd>{activeStatus(group.status)}</dd></div>
          <div><dt>Kind</dt><dd>{group.isTemplate ? <IdentityStatus label="Template" /> : "Group"}</dd></div>
          <div><dt>Version</dt><dd>{group.version}</dd></div>
        </dl>
      </IdentityPanel>
      <IdentityPanel title="Members">
        {members.length === 0 ? <IdentityEmptyState title="No members" /> : (
          <IdentityTable caption="Group members">
            <thead><tr><th scope="col">User ID</th><th scope="col">Tenant membership ID</th></tr></thead>
            <tbody>{members.map((member) => <tr key={member.tenantMembershipId}><td><code>{member.userId}</code></td><td><code>{member.tenantMembershipId}</code></td></tr>)}</tbody>
          </IdentityTable>
        )}
      </IdentityPanel>
    </IdentityPageFrame>
  );
}
