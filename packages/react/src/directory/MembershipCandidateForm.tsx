import type { ReactNode } from "react";
import { IdentityButton, IdentityInput } from "../components/index";

export interface MembershipCandidateFormProps {
  readonly formAction?: string;
  readonly error?: ReactNode;
}

export function MembershipCandidateForm({ formAction, error }: MembershipCandidateFormProps) {
  return (
    <form className="gi-form" method="post" action={formAction} data-gi-component="membership-candidate-form">
      <label className="gi-field"><span className="gi-field-label">Login identifier</span><IdentityInput name="loginIdentifier" autoComplete="username" required /></label>
      <label className="gi-field"><span className="gi-field-label">Membership status</span><select className="gi-input" name="status" defaultValue="1"><option value="1">Active</option><option value="2">Inactive</option></select></label>
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">Find / add member</IdentityButton>
    </form>
  );
}
