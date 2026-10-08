import type { ComponentProps, ReactNode } from "react";
import { IdentityButton, IdentityEntityAutocomplete } from "../components/index";

export interface OrganizationMembershipFormProps {
  readonly tenantId: string;
  readonly organizationId: string;
  readonly formAction?: ComponentProps<"form">["action"];
  readonly error?: ReactNode;
}

/** Adds an existing active tenant membership to an Organization using protected autocomplete. */
export function OrganizationMembershipForm({ tenantId, organizationId, formAction, error }: OrganizationMembershipFormProps) {
  return (
    <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="organization-membership-form">
      <input type="hidden" name="tenantId" value={tenantId} />
      <input type="hidden" name="organizationId" value={organizationId} />
      <IdentityEntityAutocomplete
        label="Tenant member"
        name="tenantMembershipId"
        kind="tenant-membership"
        tenantId={tenantId}
        required
        hint="Search active tenant memberships. Only the selected stable membership ID is submitted."
      />
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">Add member</IdentityButton>
    </form>
  );
}
