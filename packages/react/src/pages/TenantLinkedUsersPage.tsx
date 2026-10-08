import type { ReactNode } from "react";
import type { IdentityTenantLinkedUserRow } from "@generic-identity/contracts";
import { IdentityEmptyState, IdentityPageFrame, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface TenantLinkedUsersPageProps {
  readonly rows: readonly IdentityTenantLinkedUserRow[];
  readonly actions?: ReactNode;
  readonly renderUserActions?: (row: IdentityTenantLinkedUserRow) => ReactNode;
}

/** Every row is a concrete tenant membership; user IDs may legitimately repeat. */
export function TenantLinkedUsersPage({ rows, actions, renderUserActions }: TenantLinkedUsersPageProps) {
  return (
    <IdentityPageFrame
      title="Users — all authorized tenants"
      description="Membership-backed collection. No cross-tenant identity or permission is inferred."
      actions={actions}
    >
      {rows.length === 0 ? (
        <IdentityEmptyState title="No tenant-linked users" description="No authorized tenant-user row was returned." />
      ) : (
        <IdentityTable caption="Tenant-linked users">
          <thead><tr>
            <th scope="col">Display name</th>
            <th scope="col">Tenant</th>
            <th scope="col">User ID</th>
            <th scope="col">Membership ID</th>
            <th scope="col">Status</th>
            <th scope="col">User version</th>
            {renderUserActions ? <th scope="col">Actions</th> : null}
          </tr></thead>
          <tbody>
            {rows.map((row) => (
              <tr key={`${row.tenantId}:${row.membershipId}`} data-gi-record-id={row.userId}>
                <td>{row.displayName}</td>
                <td>{row.tenantDisplayName}<br /><code>{row.tenantId}</code></td>
                <td><code>{row.userId}</code></td>
                <td><code>{row.membershipId}</code></td>
                <td>{activeStatus(row.status)}</td>
                <td>{row.version}</td>
                {renderUserActions ? <td>{renderUserActions(row)}</td> : null}
              </tr>
            ))}
          </tbody>
        </IdentityTable>
      )}
    </IdentityPageFrame>
  );
}
