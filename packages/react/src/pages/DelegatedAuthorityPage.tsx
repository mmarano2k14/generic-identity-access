import type { ReactNode } from "react";
import type {
  IdentityScopeAuthorityGroupRecord,
  IdentityScopeAuthorityMemberRecord,
  IdentityScopeAuthorityPolicyBindingRecord,
  IdentityScopeAuthorityPolicyRecord,
  IdentityScopeAuthorityPolicyStatementRecord,
} from "@generic-identity/contracts/access-control";
import { IdentityEmptyState, IdentityPageFrame, IdentityPanel, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface DelegatedAuthorityPageProps {
  readonly groups: readonly IdentityScopeAuthorityGroupRecord[];
  readonly policies: readonly IdentityScopeAuthorityPolicyRecord[];
  readonly selectedGroup?: IdentityScopeAuthorityGroupRecord | null;
  readonly selectedPolicy?: IdentityScopeAuthorityPolicyRecord | null;
  readonly members?: readonly IdentityScopeAuthorityMemberRecord[];
  readonly statements?: readonly IdentityScopeAuthorityPolicyStatementRecord[];
  readonly bindings?: readonly IdentityScopeAuthorityPolicyBindingRecord[];
  readonly memberDisplayNames?: Readonly<Record<string, string>>;
  readonly policyDisplayNames?: Readonly<Record<string, string>>;
  readonly actions?: ReactNode;
  readonly lookup?: ReactNode;
  readonly groupActions?: ReactNode;
  readonly policyActions?: ReactNode;
  readonly memberActions?: ReactNode;
  readonly statementActions?: ReactNode;
  readonly bindingActions?: ReactNode;
  readonly renderGroupActions?: (group: IdentityScopeAuthorityGroupRecord) => ReactNode;
  readonly renderPolicyActions?: (policy: IdentityScopeAuthorityPolicyRecord) => ReactNode;
  readonly renderMemberActions?: (member: IdentityScopeAuthorityMemberRecord) => ReactNode;
  readonly renderStatementActions?: (statement: IdentityScopeAuthorityPolicyStatementRecord) => ReactNode;
  readonly renderBindingActions?: (binding: IdentityScopeAuthorityPolicyBindingRecord) => ReactNode;
}

export function DelegatedAuthorityPage({
  groups,
  policies,
  selectedGroup = null,
  selectedPolicy = null,
  members = [],
  statements = [],
  bindings = [],
  memberDisplayNames = {},
  policyDisplayNames = {},
  actions,
  lookup,
  groupActions,
  policyActions,
  memberActions,
  statementActions,
  bindingActions,
  renderGroupActions,
  renderPolicyActions,
  renderMemberActions,
  renderStatementActions,
  renderBindingActions,
}: DelegatedAuthorityPageProps) {
  return <IdentityPageFrame
    title="Delegated authority"
    description="Identity-scope administration groups, policies, memberships, statements and bindings. These objects are tenant-free and remain separate from tenant Groups and Managed Policies."
    actions={actions}
  >
    {lookup ? <IdentityPanel title="Authority lookup">{lookup}</IdentityPanel> : null}

    <IdentityPanel title="Authority groups">
      {groups.length === 0
        ? <IdentityEmptyState title="No authority groups" />
        : <IdentityTable caption="Authority groups">
            <thead><tr><th scope="col">Name</th><th scope="col">Group ID</th><th scope="col">Status</th><th scope="col">Version</th>{renderGroupActions ? <th scope="col">Actions</th> : null}</tr></thead>
            <tbody>{groups.map((group) => <tr key={group.groupId} aria-selected={selectedGroup?.groupId === group.groupId}>
              <td>{group.displayName}</td><td><code>{group.groupId}</code></td><td>{activeStatus(group.status)}</td><td>{group.version}</td>{renderGroupActions ? <td>{renderGroupActions(group)}</td> : null}
            </tr>)}</tbody>
          </IdentityTable>}
    </IdentityPanel>

    <IdentityPanel title="Authority policies">
      {policies.length === 0
        ? <IdentityEmptyState title="No authority policies" />
        : <IdentityTable caption="Authority policies">
            <thead><tr><th scope="col">Name</th><th scope="col">Policy ID</th><th scope="col">Status</th><th scope="col">Version</th>{renderPolicyActions ? <th scope="col">Actions</th> : null}</tr></thead>
            <tbody>{policies.map((policy) => <tr key={policy.policyId} aria-selected={selectedPolicy?.policyId === policy.policyId}>
              <td>{policy.displayName}</td><td><code>{policy.policyId}</code></td><td>{activeStatus(policy.status)}</td><td>{policy.version}</td>{renderPolicyActions ? <td>{renderPolicyActions(policy)}</td> : null}
            </tr>)}</tbody>
          </IdentityTable>}
    </IdentityPanel>

    {selectedGroup ? <IdentityPanel title={`Authority group · ${selectedGroup.displayName}`}>
      {groupActions ? <div className="gi-panel-actions">{groupActions}</div> : null}
      <dl className="gi-definition-list">
        <div><dt>Group ID</dt><dd><code>{selectedGroup.groupId}</code></dd></div>
        <div><dt>Status</dt><dd>{activeStatus(selectedGroup.status)}</dd></div>
        <div><dt>Version</dt><dd>{selectedGroup.version}</dd></div>
        <div><dt>Tenant ownership</dt><dd>None — identity scope</dd></div>
      </dl>
    </IdentityPanel> : null}

    {selectedGroup ? <IdentityPanel title="Authority group members">
      {memberActions ? <div className="gi-panel-actions">{memberActions}</div> : null}
      {members.length === 0 ? <IdentityEmptyState title="No authority members" /> : <IdentityTable caption="Authority group members">
        <thead><tr><th scope="col">User</th><th scope="col">User ID</th>{renderMemberActions ? <th scope="col">Actions</th> : null}</tr></thead>
        <tbody>{members.map((member) => <tr key={`${member.groupId}:${member.userId}`}>
          <td>{memberDisplayNames[member.userId] ?? "Identity user"}</td><td><code>{member.userId}</code></td>{renderMemberActions ? <td>{renderMemberActions(member)}</td> : null}
        </tr>)}</tbody>
      </IdentityTable>}
    </IdentityPanel> : null}

    {selectedGroup ? <IdentityPanel title="Authority policy bindings">
      {bindingActions ? <div className="gi-panel-actions">{bindingActions}</div> : null}
      {bindings.length === 0 ? <IdentityEmptyState title="No authority policy bindings" /> : <IdentityTable caption="Authority policy bindings">
        <thead><tr><th scope="col">Policy</th><th scope="col">Policy ID</th>{renderBindingActions ? <th scope="col">Actions</th> : null}</tr></thead>
        <tbody>{bindings.map((binding) => <tr key={`${binding.groupId}:${binding.policyId}`}>
          <td>{policyDisplayNames[binding.policyId] ?? "Authority policy"}</td><td><code>{binding.policyId}</code></td>{renderBindingActions ? <td>{renderBindingActions(binding)}</td> : null}
        </tr>)}</tbody>
      </IdentityTable>}
    </IdentityPanel> : null}

    {selectedPolicy ? <IdentityPanel title={`Authority policy · ${selectedPolicy.displayName}`}>
      {policyActions ? <div className="gi-panel-actions">{policyActions}</div> : null}
      <dl className="gi-definition-list">
        <div><dt>Policy ID</dt><dd><code>{selectedPolicy.policyId}</code></dd></div>
        <div><dt>Status</dt><dd>{activeStatus(selectedPolicy.status)}</dd></div>
        <div><dt>Version</dt><dd>{selectedPolicy.version}</dd></div>
        <div><dt>Tenant ownership</dt><dd>None — identity scope</dd></div>
      </dl>
    </IdentityPanel> : null}

    {selectedPolicy ? <IdentityPanel title="Authority policy statements">
      {statementActions ? <div className="gi-panel-actions">{statementActions}</div> : null}
      {statements.length === 0 ? <IdentityEmptyState title="No authority policy statements" /> : <IdentityTable caption="Authority policy statements">
        <thead><tr><th scope="col">Capability pattern</th><th scope="col">Model</th><th scope="col">Statement ID</th>{renderStatementActions ? <th scope="col">Actions</th> : null}</tr></thead>
        <tbody>{statements.map((statement) => <tr key={statement.statementId}>
          <td><code>{statement.resource}:{statement.feature}:{statement.action}</code></td><td>v{statement.modelVersion}</td><td><code>{statement.statementId}</code></td>{renderStatementActions ? <td>{renderStatementActions(statement)}</td> : null}
        </tr>)}</tbody>
      </IdentityTable>}
    </IdentityPanel> : null}
  </IdentityPageFrame>;
}
