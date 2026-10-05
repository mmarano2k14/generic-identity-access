import type { ReactNode } from "react";
import type { IdentityTenantRecord } from "@generic-identity/contracts";
import { IdentityEmptyState, IdentityPageFrame, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface TenantsPageProps {
  readonly tenants: readonly IdentityTenantRecord[];
  readonly actions?: ReactNode;
  readonly renderTenantActions?: (tenant: IdentityTenantRecord) => ReactNode;
}

export function TenantsPage({ tenants, actions, renderTenantActions }: TenantsPageProps) {
  return (
    <IdentityPageFrame title="Tenants" description="Tenant identities visible in the current administration boundary." actions={actions}>
      {tenants.length === 0 ? <IdentityEmptyState title="No tenants" /> : (
        <IdentityTable caption="Tenants">
          <thead><tr><th scope="col">Display name</th><th scope="col">Tenant ID</th><th scope="col">Status</th><th scope="col">Version</th>{renderTenantActions ? <th scope="col">Actions</th> : null}</tr></thead>
          <tbody>{tenants.map((tenant) => <tr key={tenant.tenantId} data-gi-record-id={tenant.tenantId}><td>{tenant.displayName}</td><td><code>{tenant.tenantId}</code></td><td>{activeStatus(tenant.status)}</td><td>{tenant.version}</td>{renderTenantActions ? <td>{renderTenantActions(tenant)}</td> : null}</tr>)}</tbody>
        </IdentityTable>
      )}
    </IdentityPageFrame>
  );
}
