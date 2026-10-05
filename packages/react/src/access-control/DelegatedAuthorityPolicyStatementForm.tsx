import type { ReactNode } from "react";
import { IdentityButton, IdentityInput } from "../components/index";

export interface DelegatedAuthorityPolicyStatementFormProps {
  readonly modelVersion?: number;
  readonly formAction?: string;
  readonly error?: ReactNode;
}

export function DelegatedAuthorityPolicyStatementForm({ modelVersion = 1, formAction, error }: DelegatedAuthorityPolicyStatementFormProps) {
  return <form className="gi-form" method="post" action={formAction} data-gi-component="delegated-authority-policy-statement-form">
    <label className="gi-field"><span className="gi-field-label">Model version</span><IdentityInput name="modelVersion" type="number" min={1} defaultValue={modelVersion} required /></label>
    <label className="gi-field"><span className="gi-field-label">Resource</span><IdentityInput name="resource" required /></label>
    <label className="gi-field"><span className="gi-field-label">Feature</span><IdentityInput name="feature" required /></label>
    <label className="gi-field"><span className="gi-field-label">Action</span><IdentityInput name="action" required /></label>
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">Add authority statement</IdentityButton>
  </form>;
}
