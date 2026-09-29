import { replaceTenantMemberOrganizationsAction } from "../app/identity/actions";
import { AdminMutationDialog } from "./AdminMutationDialog";

export interface AdminMemberOrganizationOption {
  readonly key: string;
  readonly displayName: string;
  readonly organizationType: string;
  readonly checked: boolean;
  readonly locked: boolean;
}

type Props = {
  readonly tenantId: string;
  readonly tenantMembershipId: string;
  readonly userDisplayName: string;
  readonly options: readonly AdminMemberOrganizationOption[];
  readonly enabled: boolean;
};

/** Reconciles explicit OrganizationMembership edges only; authorization grants are untouched. */
export function AdminManageMemberOrganizationsDialog({
  tenantId,
  tenantMembershipId,
  userDisplayName,
  options,
  enabled,
}: Props) {
  if (!enabled) return null;

  return (
    <AdminMutationDialog
      title={`Manage organizations for ${userDisplayName}`}
      description="Organization membership records where this tenant member belongs. It never grants groups, policies, capabilities, or RBAC authority."
      triggerLabel="Manage organizations"
      submitLabel="Save organizations"
      action={replaceTenantMemberOrganizationsAction}
      triggerVariant="secondary"
      triggerIcon="manage"
      compact
    >
      <input type="hidden" name="tenantId" value={tenantId} />
      <input type="hidden" name="tenantMembershipId" value={tenantMembershipId} />
      <div className="ia-group-assignment-list">
        {options.length === 0 ? (
          <p className="ia-muted">No Organizations are available in this tenant.</p>
        ) : options.map((option) => (
          <label className="ia-group-assignment-option" key={option.key}>
            {option.locked && option.checked ? (
              <input type="hidden" name="organizationSelection" value={option.key} />
            ) : null}
            <input
              type="checkbox"
              name="organizationSelection"
              value={option.key}
              defaultChecked={option.checked}
              disabled={option.locked}
            />
            <span>
              <strong>{option.displayName}</strong>
              <small>
                <span className="ia-origin-badge ia-origin-tenant">{option.organizationType.toUpperCase()}</span>
                {option.locked
                  ? "Inactive Organization; existing membership is preserved until the Organization is re-enabled."
                  : "Belonging only. Permissions remain managed through groups and policies."}
              </small>
            </span>
          </label>
        ))}
      </div>
    </AdminMutationDialog>
  );
}
