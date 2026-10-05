import type { ReactNode } from "react";
import { IdentityButton, IdentityInput } from "../components/index";
export interface DelegatedAuthorityMemberFormProps { readonly formAction?: string; readonly error?: ReactNode; }
export function DelegatedAuthorityMemberForm({ formAction, error }: DelegatedAuthorityMemberFormProps) {
  return <form className="gi-form" method="post" action={formAction} data-gi-component="delegated-authority-member-form">
    <label className="gi-field"><span className="gi-field-label">User ID</span><IdentityInput name="userId" required /></label>
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">Add authority member</IdentityButton>
  </form>;
}
