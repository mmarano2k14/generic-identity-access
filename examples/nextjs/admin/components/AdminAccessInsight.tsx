import Link from "next/link";
import { AdminIcon } from "./AdminIcon";
import type { IdentityAccessAdminAccessInsight } from "../server/IdentityAccessAdminAccessInsightService";

export interface AdminAccessInsightProps {
  readonly userId: string;
  readonly userName: string;
  readonly insight: IdentityAccessAdminAccessInsight;
}

function statusLabel(value: number | undefined): string {
  if (value === 1) return "Active";
  if (value === 2) return "Inactive";
  return "Unavailable";
}

function statusClass(value: number | undefined): string {
  return value === 1 ? "ia-status ia-status-active" : "ia-status ia-status-inactive";
}

/** Server-rendered assignment provenance. It never converts assignment structure into an RBAC verdict. */
export function AdminAccessInsight({ userId, userName, insight }: AdminAccessInsightProps) {
  const membership = insight.membership;

  return (
    <section className="ia-access-insight" aria-label={`Assigned access provenance for ${userName}`}>
      <div className="ia-access-insight-heading">
        <div className="ia-access-insight-heading-copy">
          <span className="ia-access-insight-icon" aria-hidden="true"><AdminIcon name="shield" /></span>
          <div>
            <p className="ia-card-kicker">Security insight</p>
            <h2>Assigned access provenance</h2>
            <p>Trace the selected identity through tenant membership, groups, policy bindings, resource scopes, and capability statements already returned by the server.</p>
          </div>
        </div>
        <span className="ia-insight-boundary-badge"><AdminIcon name="lock" />No allow/deny inference</span>
      </div>

      <div className="ia-access-insight-metrics">
        <div><span>Tenant membership</span><strong>{membership === null ? "None" : statusLabel(membership.status)}</strong></div>
        <div><span>Assigned groups</span><strong>{insight.groups.length}</strong></div>
        <div><span>Policy bindings</span><strong>{insight.assignmentCount}</strong></div>
        <div><span>Capability patterns</span><strong>{insight.statementCount}</strong></div>
        <div><span>Lifecycle-ready bindings</span><strong>{insight.lifecycleReadyAssignmentCount}</strong></div>
      </div>

      {membership === null ? (
        <div className="ia-access-insight-empty">
          <AdminIcon name="memberships" />
          <div><strong>No tenant membership</strong><span>This identity has no membership in the configured tenant, so no tenant-scoped group assignment path is present here.</span></div>
        </div>
      ) : insight.groups.length === 0 ? (
        <div className="ia-access-insight-empty">
          <AdminIcon name="groups" />
          <div><strong>No matching group assignment</strong><span>The tenant membership exists, but no group in this bounded administration scan contains that membership.</span></div>
        </div>
      ) : (
        <div className="ia-access-path-list">
          {insight.groups.map((group) => (
            <article className="ia-access-path-group" key={group.groupId}>
              <div className="ia-access-path-group-heading">
                <div>
                  <p className="ia-access-path-step">User <span>→</span> Membership <span>→</span> Group</p>
                  <h3>{group.groupName}</h3>
                  <code>{group.groupId}</code>
                </div>
                <div className="ia-access-path-actions">
                  <span className={statusClass(group.groupStatus)}><span className="ia-status-dot" aria-hidden="true" />{statusLabel(group.groupStatus)}</span>
                  <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/groups?groupId=${encodeURIComponent(group.groupId)}`}>Open group</Link>
                </div>
              </div>

              {group.bindings.length === 0 ? (
                <p className="ia-empty-inline">This group contains the user but has no policy binding.</p>
              ) : (
                <div className="ia-access-binding-list">
                  {group.bindings.map((binding) => {
                    const bindingKey = `${group.groupId}:${binding.policyId}:v${binding.policyVersion}:${binding.resourceScopeId ?? "tenant"}`;
                    return (
                      <section className="ia-access-binding" key={bindingKey}>
                        <div className="ia-access-binding-heading">
                          <div>
                            <p className="ia-access-path-step">Group <span>→</span> Policy binding <span>→</span> Statements</p>
                            <h4>{binding.policyName} · v{binding.policyVersion}</h4>
                            <div className="ia-access-binding-meta">
                              <span className={statusClass(binding.policyStatus)}><span className="ia-status-dot" aria-hidden="true" />Policy {statusLabel(binding.policyStatus)}</span>
                              {binding.resourceScopeId === undefined ? (
                                <span className="ia-scope-chip">Tenant-wide binding</span>
                              ) : (
                                <Link className="ia-scope-chip ia-scope-chip-link" href={`/identity/resource-scopes?resourceScopeId=${encodeURIComponent(binding.resourceScopeId)}`}>
                                  {binding.resourceScopeName ?? binding.resourceScopeId}{binding.includeDescendants ? " + descendants" : ""}
                                </Link>
                              )}
                              <span className={binding.lifecycleReady ? "ia-readiness-chip ia-readiness-ready" : "ia-readiness-chip ia-readiness-blocked"}>
                                {binding.lifecycleReady ? "Lifecycle ready" : "Lifecycle blocked"}
                              </span>
                            </div>
                          </div>
                          <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/policies?policyId=${encodeURIComponent(binding.policyId)}`}>Open policy</Link>
                        </div>

                        {!binding.lifecycleReady ? (
                          <p className="ia-access-blockers">Blocked by: {binding.blockers.join(", ")}.</p>
                        ) : null}

                        {binding.statements.length === 0 ? (
                          <p className="ia-empty-inline">No capability statement is currently present in this policy.</p>
                        ) : (
                          <div className="ia-capability-patterns" aria-label="Assigned capability patterns">
                            {binding.statements.map((statement) => (
                              <div className="ia-capability-pattern" key={statement.statementId}>
                                <code>{statement.resource}:{statement.feature}:{statement.action}</code>
                                <span>Model v{statement.modelVersion}</span>
                              </div>
                            ))}
                          </div>
                        )}
                      </section>
                    );
                  })}
                </div>
              )}
            </article>
          ))}
        </div>
      )}

      {insight.scanBoundReached ? (
        <div className="ia-access-insight-warning">
          <AdminIcon name="spark" />
          <p><strong>Bounded diagnostic view.</strong> The group scan reached its administration bound of {insight.scannedGroupCount} records, so additional group assignments may exist outside this view.</p>
        </div>
      ) : null}

      <div className="ia-access-insight-boundary">
        <AdminIcon name="lock" />
        <p><strong>Assignment structure is not an authorization verdict.</strong> Wildcard matching, exact resource-target applicability, current external RBAC state, and final allow/deny evaluation remain authoritative in the .NET authorization boundary. This view does not impersonate <code>{userId}</code>.</p>
      </div>
    </section>
  );
}
