import type { ReactNode } from "react";
import type { IdentityOrganizationRecord } from "@generic-identity/contracts/organizations";
import { IdentityEmptyState, IdentityPageFrame, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface OrganizationsPageProps {
  readonly organizations: readonly IdentityOrganizationRecord[];
  readonly tenantId?: string;
  readonly selectedOrganizationId?: string;
  readonly actions?: ReactNode;
  readonly renderOrganizationActions?: (organization: IdentityOrganizationRecord) => ReactNode;
}

function hierarchyLabel(organization: IdentityOrganizationRecord, byId: ReadonlyMap<string, IdentityOrganizationRecord>): string {
  const names = [organization.displayName];
  const visited = new Set([organization.organizationId]);
  let parentId = organization.parentOrganizationId;
  while (parentId) {
    if (visited.has(parentId)) return `Cycle / ${names.reverse().join(" / ")}`;
    visited.add(parentId);
    const parent = byId.get(parentId);
    if (!parent) return `Missing parent / ${names.reverse().join(" / ")}`;
    names.push(parent.displayName);
    parentId = parent.parentOrganizationId;
  }
  return names.reverse().join(" / ");
}

export function OrganizationsPage({ organizations, tenantId, selectedOrganizationId, actions, renderOrganizationActions }: OrganizationsPageProps) {
  const byId = new Map(organizations.map((organization) => [organization.organizationId, organization]));
  const sorted = organizations.slice().sort((a, b) => hierarchyLabel(a, byId).localeCompare(hierarchyLabel(b, byId)));
  return (
    <IdentityPageFrame title="Organizations" description={tenantId ? `Organizations for tenant ${tenantId}.` : "Organizations visible in the current tenant boundary."} actions={actions}>
      {sorted.length === 0 ? <IdentityEmptyState title="No organizations" description="Create the tenant's organizational hierarchy here. Organization belonging remains separate from authorization." /> : (
        <IdentityTable caption="Organizations">
          <thead><tr><th scope="col">Display name</th><th scope="col">Key</th><th scope="col">Type</th><th scope="col">Hierarchy</th><th scope="col">Status</th><th scope="col">Version</th>{renderOrganizationActions ? <th scope="col">Actions</th> : null}</tr></thead>
          <tbody>{sorted.map((organization) => <tr key={organization.organizationId} data-gi-record-id={organization.organizationId} data-selected={selectedOrganizationId === organization.organizationId ? "true" : undefined}><td>{organization.displayName}</td><td><code>{organization.organizationKey}</code></td><td>{organization.organizationType}</td><td>{hierarchyLabel(organization, byId)}</td><td>{activeStatus(organization.status)}</td><td>{organization.rowVersion}</td>{renderOrganizationActions ? <td>{renderOrganizationActions(organization)}</td> : null}</tr>)}</tbody>
        </IdentityTable>
      )}
    </IdentityPageFrame>
  );
}
