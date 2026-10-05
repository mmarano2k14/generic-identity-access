import type { ReactNode } from "react";
import type { IdentityOrganizationMembershipRecord } from "@generic-identity/contracts/organizations";
import { IdentityEmptyState, IdentityPageFrame, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface OrganizationMembershipsPageProps {
  readonly memberships: readonly IdentityOrganizationMembershipRecord[];
  readonly organizationId?: string;
  readonly tenantMembershipId?: string;
  readonly actions?: ReactNode;
  readonly renderMembershipActions?: (membership: IdentityOrganizationMembershipRecord) => ReactNode;
}

export function OrganizationMembershipsPage({ memberships, organizationId, tenantMembershipId, actions, renderMembershipActions }: OrganizationMembershipsPageProps) {
  const description = organizationId ? `Memberships for organization ${organizationId}.` : tenantMembershipId ? `Organizations assigned to tenant membership ${tenantMembershipId}.` : "Organization memberships visible in the current tenant boundary.";
  return (
    <IdentityPageFrame title="Organization memberships" description={description} actions={actions}>
      {memberships.length === 0 ? <IdentityEmptyState title="No organization memberships" /> : (
        <IdentityTable caption="Organization memberships">
          <thead><tr><th scope="col">Organization ID</th><th scope="col">Tenant membership ID</th><th scope="col">Status</th><th scope="col">Version</th>{renderMembershipActions ? <th scope="col">Actions</th> : null}</tr></thead>
          <tbody>{memberships.map((membership) => <tr key={`${membership.organizationId}:${membership.tenantMembershipId}`}><td><code>{membership.organizationId}</code></td><td><code>{membership.tenantMembershipId}</code></td><td>{activeStatus(membership.status)}</td><td>{membership.rowVersion}</td>{renderMembershipActions ? <td>{renderMembershipActions(membership)}</td> : null}</tr>)}</tbody>
        </IdentityTable>
      )}
    </IdentityPageFrame>
  );
}
