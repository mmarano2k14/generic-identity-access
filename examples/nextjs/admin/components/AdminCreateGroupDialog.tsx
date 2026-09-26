import type { IdentityEffectiveAdministrationContext } from "@identity-access/client";
import { createGroupAction } from "../app/identity/actions";
import { AdminField, AdminStatusField } from "./AdminField";
import { AdminMutationDialog } from "./AdminMutationDialog";
import { AdminTenantTargetField } from "./AdminTenantTargetField";

type Props = {
  readonly effectiveContext: IdentityEffectiveAdministrationContext;
  readonly selectedTenantId?: string;
};

/** Creates one tenant-owned authorization group while keeping tenant selection explicit. */
export function AdminCreateGroupDialog({ effectiveContext, selectedTenantId }: Props) {
  const canTargetTenant = effectiveContext.tenantVisibility === "scope-wide" || effectiveContext.activeTenantMemberships.length > 0;
  if (!canTargetTenant) return null;

  return (
    <AdminMutationDialog
      title="Create group"
      description="Create a tenant-scoped group for explicit authorization assignments. Choose the tenant that will own the group."
      triggerLabel="Create group"
      submitLabel="Create group"
      action={createGroupAction}
    >
      <AdminTenantTargetField effectiveContext={effectiveContext} selectedTenantId={selectedTenantId} />
      <AdminField label="Display name" name="displayName" autoComplete="off" required maxLength={200} />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );
}
