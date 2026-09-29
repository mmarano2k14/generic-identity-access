import type { IdentityOrganizationRecord } from "@identity-access/client";
import { createOrganizationAction } from "../app/identity/actions";
import { AdminField, AdminSelectField } from "./AdminField";
import { AdminMutationDialog } from "./AdminMutationDialog";

type Props = {
  readonly tenantId: string;
  readonly organizations: readonly IdentityOrganizationRecord[];
  readonly enabled: boolean;
};

/** Owns only the Organization-create form. */
export function AdminOrganizationCreateDialog({
  tenantId,
  organizations,
  enabled,
}: Props) {
  if (!enabled) return null;

  const parents = organizations
    .slice()
    .sort((left, right) =>
      left.displayName.localeCompare(right.displayName),
    );

  return (
    <AdminMutationDialog
      title="Create organization"
      description="Create a generic organizational unit inside this tenant. Business-specific profiles remain outside Identity Access."
      triggerLabel="Create organization"
      submitLabel="Create organization"
      action={createOrganizationAction}
    >
      <input type="hidden" name="tenantId" value={tenantId} />
      <AdminField
        label="Organization key"
        name="organizationKey"
        required
        maxLength={64}
        pattern="[a-z][a-z0-9-]{0,63}"
        hint="Stable tenant-local lowercase key, for example urban-flower."
      />
      <AdminField
        label="Display name"
        name="displayName"
        required
        maxLength={200}
      />
      <AdminField
        label="Organization type"
        name="organizationType"
        required
        maxLength={64}
        defaultValue="organization"
        pattern="[a-z][a-z0-9-]{0,63}"
        hint="Generic classification such as organization, business, division, department, site, or branch."
      />
      <AdminSelectField
        label="Parent organization"
        name="parentOrganizationId"
        defaultValue=""
      >
        <option value="">No parent</option>
        {parents.map((organization) => (
          <option
            value={organization.organizationId}
            key={organization.organizationId}
          >
            {organization.displayName}
          </option>
        ))}
      </AdminSelectField>
    </AdminMutationDialog>
  );
}
