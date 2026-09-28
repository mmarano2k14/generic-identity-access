import Link from "next/link";
import type { ReactNode } from "react";
import { AdminEmptyState } from "./AdminEmptyState";
import { AdminIcon } from "./AdminIcon";

export interface AdminMembershipTenantRow {
  readonly tenantId: string;
  readonly displayName: string;
  readonly status: string;
  readonly memberCountLabel: string;
  readonly selected?: boolean;
}

export interface AdminMembershipGroupBadge {
  readonly groupId: string;
  readonly displayName: string;
  readonly isTemplate: boolean;
}

export interface AdminMembershipMemberRow {
  readonly membershipId: string;
  readonly userId: string;
  readonly displayName: string;
  readonly userStatus: string;
  readonly membershipStatus: string;
  readonly version: number;
  readonly groups: readonly AdminMembershipGroupBadge[];
  readonly actions?: ReactNode;
}

export function AdminMembershipTenantTable({ rows }: { readonly rows: readonly AdminMembershipTenantRow[] }) {
  return (
    <section className="ia-table-card">
      <div className="ia-table-heading">
        <div>
          <p className="ia-card-kicker">Tenant memberships</p>
          <h2>Tenants</h2>
          <p>Select a tenant to inspect the memberships visible to the current administration scope.</p>
        </div>
        <span className="ia-count-badge">{rows.length} tenants</span>
      </div>
      {rows.length === 0 ? (
        <AdminEmptyState title="No tenant membership context" description="No tenant is currently visible to this authenticated administration subject." />
      ) : (
        <div className="ia-table-scroll">
          <table className="ia-table">
            <thead>
              <tr>
                <th scope="col">Tenant</th>
                <th scope="col">Identifier</th>
                <th scope="col">Members</th>
                <th scope="col">Status</th>
                <th scope="col" className="ia-actions-heading">Actions</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.tenantId} className={row.selected ? "ia-table-row-selected" : undefined}>
                  <td data-label="Tenant" className="ia-table-cell-name">
                    <div className="ia-entity-name">
                      <span className="ia-entity-avatar" aria-hidden="true">{row.displayName.trim().slice(0, 1).toUpperCase() || "T"}</span>
                      <strong>{row.displayName}</strong>
                      {row.selected ? <span className="ia-selected-badge">Selected</span> : null}
                    </div>
                  </td>
                  <td data-label="Identifier"><code className="ia-id-chip" title={row.tenantId}>{row.tenantId}</code></td>
                  <td data-label="Members"><span className="ia-membership-count">{row.memberCountLabel}</span></td>
                  <td data-label="Status"><span className={`ia-status ${row.status === "Active" ? "ia-status-active" : row.status === "Inactive" ? "ia-status-inactive" : "ia-status-neutral"}`}><span className="ia-status-dot" aria-hidden="true" />{row.status}</span></td>
                  <td data-label="Actions" className="ia-table-cell-actions">
                    <div className="ia-row-actions">
                      <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/memberships?tenantId=${encodeURIComponent(row.tenantId)}`}><AdminIcon name="manage" />Manage</Link>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

export function AdminMembershipMemberTable({ rows, selfOnly }: { readonly rows: readonly AdminMembershipMemberRow[]; readonly selfOnly?: boolean }) {
  return (
    <section className="ia-table-card">
      <div className="ia-table-heading">
        <div>
          <p className="ia-card-kicker">{selfOnly ? "Current membership" : "Tenant directory"}</p>
          <h2>{selfOnly ? "Your membership" : "Members"}</h2>
          <p>{selfOnly ? "Membership-limited subjects see only their own trusted tenant relationship." : "Users are loaded through the tenant membership boundary before presentation."}</p>
        </div>
        <span className="ia-count-badge">{rows.length} {rows.length === 1 ? "member" : "members"}</span>
      </div>
      {rows.length === 0 ? (
        <AdminEmptyState title="No members" description="This tenant currently contains no visible memberships." />
      ) : (
        <div className="ia-table-scroll">
          <table className="ia-table">
            <thead>
              <tr>
                <th scope="col">User</th>
                <th scope="col">User ID</th>
                <th scope="col">Membership</th>
                <th scope="col">Membership status</th>
                <th scope="col">User status</th>
                <th scope="col">Groups</th>
                <th scope="col" className="ia-actions-heading">Actions</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.membershipId}>
                  <td data-label="User" className="ia-table-cell-name">
                    <div className="ia-entity-name">
                      <span className="ia-entity-avatar" aria-hidden="true">{row.displayName.trim().slice(0, 1).toUpperCase() || "U"}</span>
                      <strong>{row.displayName}</strong>
                    </div>
                  </td>
                  <td data-label="User ID"><code className="ia-id-chip" title={row.userId}>{row.userId}</code></td>
                  <td data-label="Membership"><code className="ia-id-chip" title={row.membershipId}>{row.membershipId}</code></td>
                  <td data-label="Membership status"><span className={`ia-status ${row.membershipStatus === "Active" ? "ia-status-active" : "ia-status-inactive"}`}><span className="ia-status-dot" aria-hidden="true" />{row.membershipStatus}</span></td>
                  <td data-label="User status"><span className={`ia-status ${row.userStatus === "Active" ? "ia-status-active" : "ia-status-inactive"}`}><span className="ia-status-dot" aria-hidden="true" />{row.userStatus}</span></td>
                  <td data-label="Groups">
                    {row.groups.length === 0 ? <span className="ia-muted">No groups</span> : (
                      <div className="ia-member-group-badges">
                        {row.groups.map((group) => (
                          <span className="ia-member-group-chip" key={group.groupId}>
                            <span className={`ia-origin-badge ${group.isTemplate ? "ia-origin-template" : "ia-origin-tenant"}`}>{group.isTemplate ? "TEMPLATE" : "GROUP"}</span>
                            <span>{group.displayName}</span>
                          </span>
                        ))}
                      </div>
                    )}
                  </td>
                  <td data-label="Actions" className="ia-table-cell-actions"><div className="ia-row-actions">{row.actions}</div></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
