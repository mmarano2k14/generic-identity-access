import type { ReactNode } from "react";
import { IdentityButton, IdentityInput } from "../components/index";

export interface OrganizationMembershipFormProps {
  readonly organizationId: string;
  readonly formAction?: string;
  readonly error?: ReactNode;
}

export function OrganizationMembershipForm({ organizationId, formAction, error }: OrganizationMembershipFormProps) {
  return (
    <form className="gi-form" method="post" action={formAction} data-gi-component="organization-membership-form">
      <input type="hidden" name="organizationId" value={organizationId} />
      <label className="gi-field"><span className="gi-field-label">Tenant membership ID</span><IdentityInput name="tenantMembershipId" required /></label>
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">Add member</IdentityButton>
    </form>
  );
}
