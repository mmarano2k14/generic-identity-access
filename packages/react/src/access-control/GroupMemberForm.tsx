import type { ReactNode } from "react";
import { IdentityButton, IdentityEntityAutocomplete } from "../components/index";

type GroupMemberFormAction = string | ((formData: FormData) => void | Promise<void>);

export interface GroupMemberFormProps {
  readonly tenantId: string;
  readonly groupId: string;
  readonly excludeMembershipIds?: readonly string[];
  readonly formAction?: GroupMemberFormAction;
  readonly error?: ReactNode;
}

export function GroupMemberForm({ tenantId, groupId, excludeMembershipIds = [], formAction, error }: GroupMemberFormProps) {
  return <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="group-member-form">
    <input type="hidden" name="tenantId" value={tenantId} />
    <input type="hidden" name="groupId" value={groupId} />
    <IdentityEntityAutocomplete
      label="Tenant member"
      name="tenantMembershipId"
      kind="tenant-membership"
      tenantId={tenantId}
      excludeIds={excludeMembershipIds}
      required
      hint="Type at least 3 characters of the user display name, or enter a full user/membership ID."
    />
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">Add member</IdentityButton>
  </form>;
}
