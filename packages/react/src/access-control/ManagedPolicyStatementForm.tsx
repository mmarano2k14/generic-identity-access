import type { ReactNode } from "react";
import { IdentityButton, IdentityInput } from "../components/index";
export interface ManagedPolicyStatementFormProps { readonly formAction?: string; readonly error?: ReactNode; }
export function ManagedPolicyStatementForm({ formAction, error }: ManagedPolicyStatementFormProps) {
  return <form className="gi-form" method="post" action={formAction} data-gi-component="managed-policy-statement-form">
    <label className="gi-field"><span className="gi-field-label">Resource pattern</span><IdentityInput name="resource" required /></label>
    <label className="gi-field"><span className="gi-field-label">Feature pattern</span><IdentityInput name="feature" required /></label>
    <label className="gi-field"><span className="gi-field-label">Action pattern</span><IdentityInput name="action" required /></label>
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">Add managed statement</IdentityButton>
  </form>;
}
