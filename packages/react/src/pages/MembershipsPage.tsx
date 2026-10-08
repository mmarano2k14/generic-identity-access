import type { ReactNode } from "react";
import type { IdentityTenantMembershipRecord } from "@generic-identity/contracts";
import { IdentityEmptyState, IdentityPageFrame, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface MembershipsPageProps {
  readonly memberships: readonly IdentityTenantMembershipRecord[];
  readonly tenantId?: string;
  readonly memberNames?: Readonly<Record<string, string>>;
  readonly actions?: ReactNode;
  readonly renderMembershipActions?: (membership: IdentityTenantMembershipRecord) => ReactNode;
}

export function MembershipsPage({ memberships, tenantId, memberNames, actions, renderMembershipActions }: MembershipsPageProps) {
  return (
    <IdentityPageFrame title="Tenant memberships" description={tenantId ? `Memberships for tenant ${tenantId}.` : "Tenant memberships visible in the current boundary."} actions={actions}>
      {memberships.length === 0 ? <IdentityEmptyState title="No memberships" /> : <IdentityTable caption="Tenant memberships"><thead><tr>{memberNames ? <th scope="col">Member</th> : null}<th scope="col">User ID</th><th scope="col">Tenant ID</th><th scope="col">Membership ID</th><th scope="col">Status</th><th scope="col">Version</th>{renderMembershipActions ? <th scope="col">Actions</th> : null}</tr></thead><tbody>{memberships.map((membership) => <tr key={membership.membershipId} data-gi-record-id={membership.membershipId}>{memberNames ? <td>{memberNames[membership.userId] ?? "Unknown"}</td> : null}<td><code>{membership.userId}</code></td><td><code>{membership.tenantId}</code></td><td><code>{membership.membershipId}</code></td><td>{activeStatus(membership.status)}</td><td>{membership.version}</td>{renderMembershipActions ? <td>{renderMembershipActions(membership)}</td> : null}</tr>)}</tbody></IdentityTable>}
    </IdentityPageFrame>
  );
}
