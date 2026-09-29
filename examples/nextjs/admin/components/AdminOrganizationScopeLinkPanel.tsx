import type {
  IdentityOrganizationRecord,
  IdentityOrganizationResourceScopeLinkRecord,
} from "@identity-access/client";
import {
  linkOrganizationResourceScopeAction,
  relinkOrganizationResourceScopeAction,
  unlinkOrganizationResourceScopeAction,
} from "../app/identity/actions";
import { AdminEntityAutocomplete } from "./AdminEntityAutocomplete";
import { AdminField } from "./AdminField";
import { AdminMutationDialog } from "./AdminMutationDialog";
import { AdminRecordContext } from "./AdminRecordContext";
import { AdminSecurityBanner } from "./AdminSecurityBanner";

type Props = {
  readonly tenantId: string;
  readonly organization: IdentityOrganizationRecord;
  readonly scopeLink: IdentityOrganizationResourceScopeLinkRecord | null;
  readonly canManageScopeLinks: boolean;
  readonly canReadResourceScopes: boolean;
};

/** Owns only the selected Organization's ResourceScope mapping UI. */
export function AdminOrganizationScopeLinkPanel({
  tenantId,
  organization,
  scopeLink,
  canManageScopeLinks,
  canReadResourceScopes,
}: Props) {
  const canChooseScope = canManageScopeLinks && canReadResourceScopes;

  const actions = canChooseScope
    ? scopeLink
      ? (
          <>
            <AdminMutationDialog
              title={`Relink ${organization.displayName}`}
              description="Replace the current application's ResourceScope mapping using an existing active Identity Access scope."
              triggerLabel="Change scope"
              submitLabel="Relink scope"
              action={relinkOrganizationResourceScopeAction}
              triggerVariant="secondary"
              triggerIcon="resource-scopes"
              compact
            >
              <input type="hidden" name="tenantId" value={tenantId} />
              <input
                type="hidden"
                name="organizationId"
                value={organization.organizationId}
              />
              <input
                type="hidden"
                name="expectedRowVersion"
                value={scopeLink.rowVersion}
              />
              <AdminEntityAutocomplete
                label="Resource scope"
                name="resourceScopeId"
                kind="resource-scope"
                tenantId={tenantId}
                defaultValue={scopeLink.resourceScopeId}
                required
                hint="Search the protected ResourceScope catalog by display name, external resource ID, or stable ID. The server revalidates active status before relinking."
              />
            </AdminMutationDialog>

            <AdminMutationDialog
              title={`Remove scope link from ${organization.displayName}`}
              description="Remove only the Organization-to-ResourceScope link. The Organization and ResourceScope remain intact."
              triggerLabel="Unlink"
              submitLabel="Remove link"
              action={unlinkOrganizationResourceScopeAction}
              dangerous
              compact
            >
              <input type="hidden" name="tenantId" value={tenantId} />
              <input
                type="hidden"
                name="organizationId"
                value={organization.organizationId}
              />
              <input
                type="hidden"
                name="expectedRowVersion"
                value={scopeLink.rowVersion}
              />
              <AdminField
                label="Type REMOVE to confirm"
                name="confirmation"
                autoComplete="off"
                required
              />
            </AdminMutationDialog>
          </>
        )
      : (
          <AdminMutationDialog
            title={`Link ${organization.displayName} to a ResourceScope`}
            description="Choose an existing active ResourceScope in this tenant and application. Scope type and model version remain owned by Identity Access."
            triggerLabel="Link scope"
            submitLabel="Create link"
            action={linkOrganizationResourceScopeAction}
            triggerVariant="secondary"
            triggerIcon="resource-scopes"
            compact
          >
            <input type="hidden" name="tenantId" value={tenantId} />
            <input
              type="hidden"
              name="organizationId"
              value={organization.organizationId}
            />
            <AdminEntityAutocomplete
              label="Resource scope"
              name="resourceScopeId"
              kind="resource-scope"
              tenantId={tenantId}
              required
              hint="Search the protected ResourceScope catalog by display name, external resource ID, or stable ID. The server accepts only a valid active scope in this tenant and application."
            />
          </AdminMutationDialog>
        )
    : undefined;

  return (
    <div className="ia-section-block">
      <AdminRecordContext
        kicker="Organization ResourceScope"
        title={organization.displayName}
        description="The ResourceScope link says where Identity Access policy can be scoped for this Organization. It does not grant a capability by itself."
        identifier={organization.organizationId}
        status={scopeLink ? "Linked" : "Not linked"}
        version={scopeLink?.rowVersion}
        facts={[
          {
            label: "Application",
            value: scopeLink?.applicationKey ?? "Current application",
          },
          { label: "Scope type", value: scopeLink?.scopeType ?? "—" },
          {
            label: "ResourceScope",
            value: scopeLink?.resourceScopeId ?? "—",
          },
        ]}
        actions={actions}
        closeHref={`/identity/memberships?tenantId=${encodeURIComponent(
          tenantId,
        )}`}
        closeLabel="Close scope mapping"
      />

      {canManageScopeLinks && !canReadResourceScopes ? (
        <AdminSecurityBanner
          title="ResourceScope catalog is not readable."
          description="The link can be inspected when authorized, but choosing a new target requires resource-scope/read in addition to organization-scope-link/write."
        />
      ) : null}
    </div>
  );
}
