import type { ReactNode } from "react";
import { IdentityButton, IdentityInput } from "../components/index";
export interface ManagedPolicyVersionFormProps { readonly formAction?: string; readonly error?: ReactNode; }
export function ManagedPolicyVersionForm({ formAction, error }: ManagedPolicyVersionFormProps) {
  return <form className="gi-form" method="post" action={formAction} data-gi-component="managed-policy-version-form">
    <label className="gi-field"><span className="gi-field-label">Policy version</span><IdentityInput name="policyVersion" type="number" min={1} required /></label>
    <label className="gi-field"><span className="gi-field-label">Security model version</span><IdentityInput name="modelVersion" type="number" min={1} required /></label>
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">Create version</IdentityButton>
  </form>;
}
