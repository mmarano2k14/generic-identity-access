import { createTenantMembershipAction } from "../app/identity/actions";
import { AdminEntityAutocomplete } from "./AdminEntityAutocomplete";
import { AdminExactMemberLookup } from "./AdminExactMemberLookup";
import { AdminStatusField } from "./AdminField";
import { AdminMutationDialog } from "./AdminMutationDialog";

type Props = {
  readonly tenantId: string;
  readonly scopeWide: boolean;
  readonly enabled: boolean;
};

/** Adds one membership while keeping global-user browsing restricted to scope-wide administrators. */
export function AdminAddTenantMemberDialog({ tenantId, scopeWide, enabled }: Props) {
  if (!enabled) return null;

  return (
    <AdminMutationDialog
      title="Add member"
      description={scopeWide
        ? "Attach an existing identity-scope user to this tenant. The membership itself grants no capability."
        : "Resolve one exact login and attach that account to the current tenant. No global user directory is exposed."}
      triggerLabel="Add member"
      submitLabel="Add member"
      action={createTenantMembershipAction}
    >
      <input type="hidden" name="tenantId" value={tenantId} />
      {scopeWide ? (
        <AdminEntityAutocomplete
          label="Existing user"
          name="userId"
          kind="user"
          required
          hint="Identity-scope administrators may search the global user directory."
        />
      ) : (
        <AdminExactMemberLookup tenantId={tenantId} />
      )}
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );
}
