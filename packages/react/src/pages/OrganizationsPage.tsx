import type { ReactNode } from "react";
import type { IdentityOrganizationRecord } from "@generic-identity/contracts/organizations";
import { IdentityEmptyState, IdentityPageFrame, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface OrganizationsPageProps {
  readonly organizations: readonly IdentityOrganizationRecord[];
  readonly tenantId?: string;
  readonly actions?: ReactNode;
  readonly renderOrganizationActions?: (organization: IdentityOrganizationRecord) => ReactNode;
}

export function OrganizationsPage({ organizations, tenantId, actions, renderOrganizationActions }: OrganizationsPageProps) {
  return (
    <IdentityPageFrame title="Organizations" description={tenantId ? `Organizations for tenant ${tenantId}.` : "Organizations visible in the current tenant boundary."} actions={actions}>
      {organizations.length === 0 ? <IdentityEmptyState title="No organizations" /> : (
        <IdentityTable caption="Organizations">
          <thead><tr><th scope="col">Display name</th><th scope="col">Key</th><th scope="col">Type</th><th scope="col">Parent</th><th scope="col">Status</th><th scope="col">Version</th>{renderOrganizationActions ? <th scope="col">Actions</th> : null}</tr></thead>
          <tbody>{organizations.map((organization) => <tr key={organization.organizationId} data-gi-record-id={organization.organizationId}><td>{organization.displayName}</td><td><code>{organization.organizationKey}</code></td><td>{organization.organizationType}</td><td>{organization.parentOrganizationId ? <code>{organization.parentOrganizationId}</code> : "—"}</td><td>{activeStatus(organization.status)}</td><td>{organization.rowVersion}</td>{renderOrganizationActions ? <td>{renderOrganizationActions(organization)}</td> : null}</tr>)}</tbody>
        </IdentityTable>
      )}
    </IdentityPageFrame>
  );
}
