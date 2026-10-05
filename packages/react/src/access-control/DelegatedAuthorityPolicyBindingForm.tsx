import type { ReactNode } from "react";
import type { IdentityScopeAuthorityPolicyRecord } from "@generic-identity/contracts/access-control";
import { IdentityButton } from "../components/index";

export interface DelegatedAuthorityPolicyBindingFormProps {
  readonly policies: readonly IdentityScopeAuthorityPolicyRecord[];
  readonly formAction?: string;
  readonly error?: ReactNode;
}

export function DelegatedAuthorityPolicyBindingForm({ policies, formAction, error }: DelegatedAuthorityPolicyBindingFormProps) {
  return <form className="gi-form" method="post" action={formAction} data-gi-component="delegated-authority-policy-binding-form">
    <label className="gi-field"><span className="gi-field-label">Authority policy</span><select className="gi-input" name="policyId" required><option value="">Select a policy</option>{policies.map((policy) => <option key={policy.policyId} value={policy.policyId}>{policy.displayName}</option>)}</select></label>
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">Bind authority policy</IdentityButton>
  </form>;
}
