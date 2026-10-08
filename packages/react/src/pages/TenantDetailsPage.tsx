import type { ReactNode } from "react";
import type { IdentityTenantMembershipRecord, IdentityTenantRecord } from "@generic-identity/contracts";
import { IdentityEmptyState, IdentityPageFrame, IdentityPanel, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface TenantDetailsPageProps {
  readonly tenant: IdentityTenantRecord;
  readonly memberships?: readonly IdentityTenantMembershipRecord[];
  readonly showMemberships?: boolean;
  readonly actions?: ReactNode;
}

export function TenantDetailsPage({
  tenant,
  memberships = [],
  showMemberships = true,
  actions,
}: TenantDetailsPageProps) {
  return (
    <IdentityPageFrame title={tenant.displayName} description={`Tenant ${tenant.tenantId}`} actions={actions}>
      <IdentityPanel title="Tenant"><dl className="gi-definition-list"><div><dt>Tenant ID</dt><dd><code>{tenant.tenantId}</code></dd></div><div><dt>Status</dt><dd>{activeStatus(tenant.status)}</dd></div><div><dt>Version</dt><dd>{tenant.version}</dd></div></dl></IdentityPanel>
      {showMemberships ? (
        <IdentityPanel title="Memberships">
          {memberships.length === 0 ? <IdentityEmptyState title="No memberships" /> : <IdentityTable caption="Tenant memberships"><thead><tr><th scope="col">User ID</th><th scope="col">Membership ID</th><th scope="col">Status</th><th scope="col">Version</th></tr></thead><tbody>{memberships.map((membership) => <tr key={membership.membershipId}><td><code>{membership.userId}</code></td><td><code>{membership.membershipId}</code></td><td>{activeStatus(membership.status)}</td><td>{membership.version}</td></tr>)}</tbody></IdentityTable>}
        </IdentityPanel>
      ) : null}
    </IdentityPageFrame>
  );
}
