import type { ReactNode } from "react";
import { IdentityButton, IdentityInput } from "../components/index";

type DelegatedAuthorityPolicyStatementFormAction = string | ((formData: FormData) => void | Promise<void>);

export interface DelegatedAuthorityPolicyStatementFormProps {
  readonly policyId: string;
  readonly modelVersion?: number;
  readonly formAction?: DelegatedAuthorityPolicyStatementFormAction;
  readonly error?: ReactNode;
}

export function DelegatedAuthorityPolicyStatementForm({ policyId, modelVersion = 1, formAction, error }: DelegatedAuthorityPolicyStatementFormProps) {
  return <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="delegated-authority-policy-statement-form">
    <input type="hidden" name="policyId" value={policyId} />
    <label className="gi-field"><span className="gi-field-label">Security model version</span><IdentityInput name="modelVersion" type="number" min={1} step={1} defaultValue={modelVersion} required /></label>
    <label className="gi-field"><span className="gi-field-label">Resource</span><IdentityInput name="resource" required maxLength={128} /></label>
    <label className="gi-field"><span className="gi-field-label">Feature</span><IdentityInput name="feature" required maxLength={128} /></label>
    <label className="gi-field"><span className="gi-field-label">Action</span><IdentityInput name="action" required maxLength={128} /></label>
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">Add authority statement</IdentityButton>
  </form>;
}
