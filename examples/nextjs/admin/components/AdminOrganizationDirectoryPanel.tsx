import type {
  IdentityOrganizationRecord,
  IdentityOrganizationResourceScopeLinkRecord,
} from "@identity-access/client";
import { AdminEmptyState } from "./AdminEmptyState";
import { AdminOrganizationCreateDialog } from "./AdminOrganizationCreateDialog";
import { AdminOrganizationScopeLinkPanel } from "./AdminOrganizationScopeLinkPanel";
import { AdminOrganizationTable } from "./AdminOrganizationTable";

export interface AdminOrganizationDirectoryPermissions {
  readonly canManageOrganizations: boolean;
  readonly canReadScopeLinks: boolean;
  readonly canManageScopeLinks: boolean;
  readonly canReadResourceScopes: boolean;
}

type Props = {
  readonly tenantId: string;
  readonly organizations: readonly IdentityOrganizationRecord[];
  readonly selectedOrganization: IdentityOrganizationRecord | null;
  readonly selectedScopeLink: IdentityOrganizationResourceScopeLinkRecord | null;
  readonly permissions: AdminOrganizationDirectoryPermissions;
};

/** Composes focused Organization Directory UI sections inside Identity Membership. */
export function AdminOrganizationDirectoryPanel({
  tenantId,
  organizations,
  selectedOrganization,
  selectedScopeLink,
  permissions,
}: Props) {
  return (
    <section className="ia-table-card">
      <div className="ia-table-heading">
        <div>
          <p className="ia-card-kicker">
            Identity membership · Organizations
          </p>
          <h2>Organization directory</h2>
          <p>
            Organizations stay inside this existing tenant membership workspace.
            They describe belonging and authorization scope boundaries, not
            business profiles.
          </p>
        </div>
        <div className="ia-action-row">
          <span className="ia-count-badge">
            {organizations.length}{" "}
            {organizations.length === 1 ? "organization" : "organizations"}
          </span>
          <AdminOrganizationCreateDialog
            tenantId={tenantId}
            organizations={organizations}
            enabled={permissions.canManageOrganizations}
          />
        </div>
      </div>

      {organizations.length === 0 ? (
        <AdminEmptyState
          title="No Organizations"
          description="Create the tenant's organizational hierarchy here. Organization membership remains separate from permission."
        />
      ) : (
        <AdminOrganizationTable
          tenantId={tenantId}
          organizations={organizations}
          selectedOrganizationId={selectedOrganization?.organizationId}
          canReadScopeLinks={permissions.canReadScopeLinks}
          canManageOrganizations={permissions.canManageOrganizations}
        />
      )}

      {selectedOrganization ? (
        <AdminOrganizationScopeLinkPanel
          tenantId={tenantId}
          organization={selectedOrganization}
          scopeLink={selectedScopeLink}
          canManageScopeLinks={permissions.canManageScopeLinks}
          canReadResourceScopes={permissions.canReadResourceScopes}
        />
      ) : null}
    </section>
  );
}
