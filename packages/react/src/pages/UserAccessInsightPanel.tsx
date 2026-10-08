import type { IdentityUserAccessInsight } from "@generic-identity/contracts";
import { IdentityEmptyState, IdentityPanel, IdentityTable } from "../components/index";

export interface UserAccessInsightPanelProps {
  readonly insight: IdentityUserAccessInsight;
  readonly userName: string;
  readonly userId: string;
  readonly groupHref?: (groupId: string) => string;
  readonly policyHref?: (policyId: string) => string;
  readonly resourceScopeHref?: (resourceScopeId: string) => string;
}

/** Assignment provenance only; NEVER render these statements as effective RBAC grants. */
export function UserAccessInsightPanel({
  insight, userName, userId, groupHref, policyHref, resourceScopeHref,
}: UserAccessInsightPanelProps) {
  return (
    <IdentityPanel
      title={`Assigned access provenance — ${userName}`}
      description="Tenant membership → groups → policy bindings → capability patterns. Not an allow/deny verdict."
    >
      <dl className="gi-definition-list">
        <div><dt>Membership</dt><dd>{insight.membership === null ? "None" : insight.membership.status === 1 ? "Active" : "Inactive"}</dd></div>
        <div><dt>Assigned groups</dt><dd>{insight.groups.length}</dd></div>
        <div><dt>Policy bindings</dt><dd>{insight.assignmentCount}</dd></div>
        <div><dt>Capability patterns</dt><dd>{insight.statementCount}</dd></div>
        <div><dt>Lifecycle-ready bindings</dt><dd>{insight.lifecycleReadyAssignmentCount}</dd></div>
      </dl>
      {insight.groups.length === 0 ? (
        <IdentityEmptyState
          title={insight.membership === null ? "No tenant membership" : "No assigned groups in scan"}
          description="Absence of an assignment in this bounded diagnostic is not a security verdict."
        />
      ) : insight.groups.map((group) => (
        <section key={group.groupId} className="gi-panel" data-gi-component="user-access-group">
          <h3>{groupHref ? <a href={groupHref(group.groupId)}>{group.groupName}</a> : group.groupName}</h3>
          <p>Group: {group.groupStatus === 1 ? "Active" : "Inactive"} · <code>{group.groupId}</code></p>
          {group.bindings.length === 0 ? <p>No policy bindings in this group.</p> : (
            <IdentityTable caption={`Bindings for ${group.groupName}`}>
              <thead><tr><th scope="col">Policy</th><th scope="col">Resource</th><th scope="col">Lifecycle</th><th scope="col">Patterns</th></tr></thead>
              <tbody>{group.bindings.map((binding) => (
                <tr key={`${binding.policyId}:${binding.policyVersion}:${binding.resourceScopeId ?? "tenant"}`}>
                  <td>
                    {policyHref ? <a href={policyHref(binding.policyId)}>{binding.policyName}</a> : binding.policyName}
                    <br />v{binding.policyVersion} · {binding.policyStatus === 1 ? "Active" : "Inactive / unavailable"}
                  </td>
                  <td>{binding.resourceScopeId === undefined ? "Tenant-wide" : (
                    resourceScopeHref ? <a href={resourceScopeHref(binding.resourceScopeId)}>{binding.resourceScopeName ?? binding.resourceScopeId}</a> : (binding.resourceScopeName ?? binding.resourceScopeId)
                  )}{binding.includeDescendants && binding.resourceScopeId !== undefined ? " + descendants" : ""}</td>
                  <td>{binding.lifecycleReady ? "Lifecycle ready" : `Blocked: ${binding.blockers.join(", ")}`}</td>
                  <td>{binding.statements.map((statement) => (
                    <div key={statement.statementId}>
                      <code>{statement.resource}:{statement.feature}:{statement.action}</code> · model v{statement.modelVersion}
                    </div>
                  ))}</td>
                </tr>
              ))}</tbody>
            </IdentityTable>
          )}
        </section>
      ))}
      {insight.scanBoundReached ? (
        <p role="status">Bounded diagnostic: the group scan reached {insight.scannedGroupCount} records. Additional group assignments may exist.</p>
      ) : null}
      <p>
        <strong>Not an authorization decision.</strong> Exact resource checks, wildcard evaluation and effective RBAC state
        remain authoritative in the .NET security boundary; this view does not impersonate <code>{userId}</code>.
      </p>
    </IdentityPanel>
  );
}
