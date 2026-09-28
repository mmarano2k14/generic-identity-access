import type { IdentityEffectiveAdministrationContext } from "@identity-access/client";
import { createGroupAction } from "../app/identity/actions";
import { AdminField, AdminStatusField } from "./AdminField";
import { AdminMutationDialog } from "./AdminMutationDialog";
import { AdminTenantTargetField } from "./AdminTenantTargetField";

type Props = {
  readonly effectiveContext: IdentityEffectiveAdministrationContext;
  readonly selectedTenantId?: string;
};

/** Creates one tenant-owned custom group while keeping tenant selection explicit. */
export function AdminCreateGroupDialog({ effectiveContext, selectedTenantId }: Props) {
  const canTargetTenant = effectiveContext.tenantVisibility === "scope-wide" || effectiveContext.activeTenantMemberships.length > 0;
  if (!canTargetTenant) return null;

  return (
    <AdminMutationDialog
      title="Create tenant group"
      description="Create a tenant-owned custom group. Reusable application templates are administered separately and remain read-only from tenant context."
      triggerLabel="Create tenant group"
      submitLabel="Create tenant group"
      action={createGroupAction}
    >
      <AdminTenantTargetField effectiveContext={effectiveContext} selectedTenantId={selectedTenantId} />
      <AdminField label="Display name" name="displayName" autoComplete="off" required maxLength={200} />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );
}
