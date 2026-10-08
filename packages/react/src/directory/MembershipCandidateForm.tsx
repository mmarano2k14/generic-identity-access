import type { ReactNode } from "react";
import { IdentityButton, IdentityInput } from "../components/index";

export interface MembershipCandidateFormProps {
  readonly tenantId?: string;
  readonly formAction?: string | ((formData: FormData) => void | Promise<void>);
  readonly error?: ReactNode;
}

/** Login-based enrollment for delegated administrators (no global user ID lookup). */
export function MembershipCandidateForm({ tenantId, formAction, error }: MembershipCandidateFormProps) {
  return (
    <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="membership-candidate-form">
      {tenantId ? <input type="hidden" name="tenantId" value={tenantId} /> : null}
      <label className="gi-field">
        <span className="gi-field-label">Exact login identifier</span>
        <IdentityInput name="loginIdentifier" autoComplete="username" maxLength={320} required />
      </label>
      <label className="gi-field">
        <span className="gi-field-label">Membership status</span>
        <select className="gi-input" name="status" defaultValue="1">
          <option value="1">Active</option>
          <option value="2">Inactive</option>
        </select>
      </label>
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">Find and add member</IdentityButton>
    </form>
  );
}
