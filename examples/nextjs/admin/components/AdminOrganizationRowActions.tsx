import type { IdentityOrganizationRecord } from "@identity-access/client";
import {
  disableOrganizationAction,
  enableOrganizationAction,
  updateOrganizationAction,
} from "../app/identity/actions";
import { AdminField, AdminSelectField } from "./AdminField";
import { AdminMutationDialog } from "./AdminMutationDialog";

type Props = {
  readonly tenantId: string;
  readonly organization: IdentityOrganizationRecord;
  readonly organizations: readonly IdentityOrganizationRecord[];
};

/** Owns only Organization definition edit and lifecycle controls for one table row. */
export function AdminOrganizationRowActions({
  tenantId,
  organization,
  organizations,
}: Props) {
  const parentOptions = organizations
    .filter(
      (candidate) =>
        candidate.organizationId !== organization.organizationId,
    )
    .slice()
    .sort((left, right) =>
      left.displayName.localeCompare(right.displayName),
    );

  return (
    <>
      <AdminMutationDialog
        title={`Edit ${organization.displayName}`}
        description="Change generic Organization definition state. The stable organization key does not change."
        triggerLabel="Edit"
        submitLabel="Save organization"
        action={updateOrganizationAction}
        triggerVariant="secondary"
        triggerIcon="edit"
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
          value={organization.rowVersion}
        />
        <AdminField
          label="Display name"
          name="displayName"
          defaultValue={organization.displayName}
          required
          maxLength={200}
        />
        <AdminField
          label="Organization type"
          name="organizationType"
          defaultValue={organization.organizationType}
          required
          maxLength={64}
          pattern="[a-z][a-z0-9-]{0,63}"
        />
        <AdminSelectField
          label="Parent organization"
          name="parentOrganizationId"
          defaultValue={organization.parentOrganizationId ?? ""}
        >
          <option value="">No parent</option>
          {parentOptions.map((parent) => (
            <option
              value={parent.organizationId}
              key={parent.organizationId}
            >
              {parent.displayName}
            </option>
          ))}
        </AdminSelectField>
      </AdminMutationDialog>

      <AdminMutationDialog
        title={`${
          organization.status === 1 ? "Disable" : "Enable"
        } ${organization.displayName}`}
        description={
          organization.status === 1
            ? "Disable this Organization without deleting its stable identity or memberships."
            : "Re-enable this Organization for normal operations."
        }
        triggerLabel={
          organization.status === 1 ? "Disable" : "Enable"
        }
        submitLabel={
          organization.status === 1
            ? "Disable organization"
            : "Enable organization"
        }
        action={
          organization.status === 1
            ? disableOrganizationAction
            : enableOrganizationAction
        }
        triggerVariant="secondary"
        triggerIcon="manage"
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
          value={organization.rowVersion}
        />
      </AdminMutationDialog>
    </>
  );
}
