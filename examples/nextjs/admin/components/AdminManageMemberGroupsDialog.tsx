import { replaceTenantMemberGroupsAction } from "../app/identity/actions";
import { AdminMutationDialog } from "./AdminMutationDialog";

export interface AdminMemberGroupOption {
  readonly key: string;
  readonly displayName: string;
  readonly isTemplate: boolean;
  readonly checked: boolean;
}

type Props = {
  readonly tenantId: string;
  readonly tenantMembershipId: string;
  readonly userDisplayName: string;
  readonly options: readonly AdminMemberGroupOption[];
  readonly enabled: boolean;
};

/** Reconciles membership edges only against real groups already present in this tenant. */
export function AdminManageMemberGroupsDialog({ tenantId, tenantMembershipId, userDisplayName, options, enabled }: Props) {
  if (!enabled) return null;
  return (
    <AdminMutationDialog title={`Manage groups for ${userDisplayName}`} description="Assignments remain inside this tenant. Reusable groups are still real groups; selecting one here never clones another group." triggerLabel="Manage groups" submitLabel="Save groups" action={replaceTenantMemberGroupsAction} triggerVariant="secondary" triggerIcon="manage" compact>
      <input type="hidden" name="tenantId" value={tenantId} />
      <input type="hidden" name="tenantMembershipId" value={tenantMembershipId} />
      <div className="ia-group-assignment-list">
        {options.length === 0 ? <p className="ia-muted">No groups are available in this tenant.</p> : options.map((option) => (
          <label className="ia-group-assignment-option" key={option.key}>
            <input type="checkbox" name="groupSelection" value={option.key} defaultChecked={option.checked} />
            <span><strong>{option.displayName}</strong><small><span className={`ia-origin-badge ${option.isTemplate ? "ia-origin-template" : "ia-origin-tenant"}`}>{option.isTemplate ? "TEMPLATE" : "GROUP"}</span>Tenant-local group assignment.</small></span>
          </label>
        ))}
      </div>
    </AdminMutationDialog>
  );
}
