import type { ReactNode } from "react";
import type {
  IdentityGroupMemberRecord,
  IdentityGroupRecord,
  IdentityManagedGroupPolicyBindingRecord,
} from "@generic-identity/contracts/access-control";
import { IdentityEmptyState, IdentityPageFrame, IdentityPanel, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface GroupAccessPageProps {
  readonly group: IdentityGroupRecord;
  readonly members?: readonly IdentityGroupMemberRecord[];
  readonly managedPolicyBindings?: readonly IdentityManagedGroupPolicyBindingRecord[];
  readonly memberDisplayNames?: Readonly<Record<string, string>>;
  readonly managedPolicyDisplayNames?: Readonly<Record<string, string>>;
  readonly resourceScopeDisplayNames?: Readonly<Record<string, string>>;
  readonly actions?: ReactNode;
  readonly memberActions?: ReactNode;
  readonly policyBindingActions?: ReactNode;
  readonly renderMemberActions?: (member: IdentityGroupMemberRecord) => ReactNode;
  readonly renderPolicyBindingActions?: (binding: IdentityManagedGroupPolicyBindingRecord) => ReactNode;
}

export function GroupAccessPage({
  group,
  members = [],
  managedPolicyBindings = [],
  memberDisplayNames = {},
  managedPolicyDisplayNames = {},
  resourceScopeDisplayNames = {},
  actions,
  memberActions,
  policyBindingActions,
  renderMemberActions,
  renderPolicyBindingActions,
}: GroupAccessPageProps) {
  return <IdentityPageFrame title={group.displayName} description={`Access control group ${group.groupId}`} actions={actions}>
    <IdentityPanel title="Group"><dl className="gi-definition-list"><div><dt>Tenant</dt><dd><code>{group.tenantId}</code></dd></div><div><dt>Status</dt><dd>{activeStatus(group.status)}</dd></div><div><dt>Template</dt><dd>{group.isTemplate ? "Yes" : "No"}</dd></div><div><dt>Version</dt><dd>{group.version}</dd></div></dl></IdentityPanel>
    <IdentityPanel title="Members">
      {memberActions ? <div className="gi-panel-actions">{memberActions}</div> : null}
      {members.length === 0 ? <IdentityEmptyState title="No members" /> : <IdentityTable caption="Group members"><thead><tr><th scope="col">Member</th><th scope="col">User ID</th><th scope="col">Tenant membership ID</th>{renderMemberActions ? <th scope="col">Actions</th> : null}</tr></thead><tbody>{members.map((member) => <tr key={member.tenantMembershipId}><td>{memberDisplayNames[member.tenantMembershipId] ?? "Tenant member"}</td><td><code>{member.userId}</code></td><td><code>{member.tenantMembershipId}</code></td>{renderMemberActions ? <td>{renderMemberActions(member)}</td> : null}</tr>)}</tbody></IdentityTable>}
    </IdentityPanel>
    <IdentityPanel title="Managed policy bindings">
      {policyBindingActions ? <div className="gi-panel-actions">{policyBindingActions}</div> : null}
      {managedPolicyBindings.length === 0 ? <IdentityEmptyState title="No policy bindings" /> : <IdentityTable caption="Group managed policy bindings"><thead><tr><th scope="col">Policy</th><th scope="col">Version</th><th scope="col">Resource scope</th><th scope="col">Descendants</th>{renderPolicyBindingActions ? <th scope="col">Actions</th> : null}</tr></thead><tbody>{managedPolicyBindings.map((binding) => <tr key={`managed:${binding.policyId}:${binding.policyVersion}:${binding.resourceScopeId ?? ""}`}><td>{managedPolicyDisplayNames[binding.policyId] ?? <code>{binding.policyId}</code>}</td><td>{binding.policyVersion}</td><td>{binding.resourceScopeId ? (resourceScopeDisplayNames[binding.resourceScopeId] ?? <code>{binding.resourceScopeId}</code>) : "Unscoped"}</td><td>{binding.includeDescendants ? "Yes" : "No"}</td>{renderPolicyBindingActions ? <td>{renderPolicyBindingActions(binding)}</td> : null}</tr>)}</tbody></IdentityTable>}
    </IdentityPanel>
  </IdentityPageFrame>;
}
