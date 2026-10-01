import type { ReactNode } from "react";
import type { IdentityTenantMembershipRecord, IdentityUserRecord } from "@generic-identity/contracts";
import { IdentityEmptyState, IdentityPageFrame, IdentityPanel, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface UserDetailsPageProps {
  readonly user: IdentityUserRecord;
  readonly memberships?: readonly IdentityTenantMembershipRecord[];
  readonly actions?: ReactNode;
}

export function UserDetailsPage({ user, memberships = [], actions }: UserDetailsPageProps) {
  return (
    <IdentityPageFrame title={user.displayName} description={`User ${user.userId}`} actions={actions}>
      <IdentityPanel title="Account">
        <dl className="gi-definition-list">
          <div><dt>User ID</dt><dd><code>{user.userId}</code></dd></div>
          <div><dt>Status</dt><dd>{activeStatus(user.status)}</dd></div>
          <div><dt>Version</dt><dd>{user.version}</dd></div>
        </dl>
      </IdentityPanel>
      <IdentityPanel title="Tenant memberships">
        {memberships.length === 0 ? <IdentityEmptyState title="No memberships" /> : (
          <IdentityTable caption="Tenant memberships">
            <thead><tr><th scope="col">Tenant</th><th scope="col">Membership ID</th><th scope="col">Status</th><th scope="col">Version</th></tr></thead>
            <tbody>{memberships.map((membership) => <tr key={membership.membershipId}><td><code>{membership.tenantId}</code></td><td><code>{membership.membershipId}</code></td><td>{activeStatus(membership.status)}</td><td>{membership.version}</td></tr>)}</tbody>
          </IdentityTable>
        )}
      </IdentityPanel>
    </IdentityPageFrame>
  );
}
