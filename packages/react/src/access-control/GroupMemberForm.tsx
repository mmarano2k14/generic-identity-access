import type { ReactNode } from "react";
import { IdentityButton, IdentityInput } from "../components/index";
export interface GroupMemberFormProps { readonly formAction?: string; readonly error?: ReactNode; }
export function GroupMemberForm({ formAction, error }: GroupMemberFormProps) {
  return <form className="gi-form" method="post" action={formAction} data-gi-component="group-member-form">
    <label className="gi-field"><span className="gi-field-label">Tenant membership ID</span><IdentityInput name="tenantMembershipId" required /></label>
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">Add member</IdentityButton>
  </form>;
}
