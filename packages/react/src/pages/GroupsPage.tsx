import type { ReactNode } from "react";
import type { IdentityGroupRecord } from "@generic-identity/contracts";
import { IdentityEmptyState, IdentityPageFrame, IdentityStatus, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface GroupsPageProps {
  readonly groups: readonly IdentityGroupRecord[];
  readonly title?: string;
  readonly description?: string;
  readonly actions?: ReactNode;
  readonly renderGroupActions?: (group: IdentityGroupRecord) => ReactNode;
}

export function GroupsPage({
  groups,
  title = "Groups",
  description = "Tenant groups and reusable group templates.",
  actions,
  renderGroupActions,
}: GroupsPageProps) {
  return (
    <IdentityPageFrame title={title} description={description} actions={actions}>
      {groups.length === 0 ? <IdentityEmptyState title="No groups" description="No group is visible in the current tenant." /> : (
        <IdentityTable caption="Groups">
          <thead><tr><th scope="col">Name</th><th scope="col">Group ID</th><th scope="col">Status</th><th scope="col">Kind</th><th scope="col">Version</th>{renderGroupActions ? <th scope="col">Actions</th> : null}</tr></thead>
          <tbody>{groups.map((group) => <tr key={group.groupId} data-gi-record-id={group.groupId}><td>{group.displayName}</td><td><code>{group.groupId}</code></td><td>{activeStatus(group.status)}</td><td>{group.isTemplate ? <IdentityStatus label="Template" tone="neutral" /> : "Group"}</td><td>{group.version}</td>{renderGroupActions ? <td>{renderGroupActions(group)}</td> : null}</tr>)}</tbody>
        </IdentityTable>
      )}
    </IdentityPageFrame>
  );
}
