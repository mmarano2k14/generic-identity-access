import { createManagedPolicyAction } from "../app/identity/actions";
import { AdminField, AdminStatusField } from "./AdminField";
import { AdminMutationDialog } from "./AdminMutationDialog";

/** Creates one reusable managed-policy definition in the shared application catalog. */
export function AdminCreateManagedPolicyDialog() {
  return (
    <AdminMutationDialog
      title="Create managed policy"
      description="Create one reusable policy definition for this identity scope and application. Tenants consume published versions through separate group bindings; no tenant owns this definition."
      triggerLabel="Create managed policy"
      submitLabel="Create policy"
      action={createManagedPolicyAction}
    >
      <AdminField label="Policy key" name="policyKey" autoComplete="off" required maxLength={128} pattern="[a-z][a-z0-9-]{0,127}" hint="Stable lowercase key, for example storage-read-only or iam-user-administrator." />
      <AdminField label="Display name" name="displayName" autoComplete="off" required maxLength={200} />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );
}
