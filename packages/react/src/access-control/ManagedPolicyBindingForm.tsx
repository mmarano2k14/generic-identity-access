import type { ReactNode } from "react";
import type { IdentityManagedPolicyRecord, IdentityResourceScopeRecord } from "@generic-identity/contracts/access-control";
import { IdentityButton, IdentityInput } from "../components/index";
export interface ManagedPolicyBindingFormProps { readonly policies: readonly IdentityManagedPolicyRecord[]; readonly resourceScopes?: readonly IdentityResourceScopeRecord[]; readonly formAction?: string; readonly error?: ReactNode; }
export function ManagedPolicyBindingForm({ policies, resourceScopes = [], formAction, error }: ManagedPolicyBindingFormProps) {
  return <form className="gi-form" method="post" action={formAction} data-gi-component="managed-policy-binding-form">
    <label className="gi-field"><span className="gi-field-label">Managed policy</span><select className="gi-input" name="policyId" required><option value="">Select a policy</option>{policies.map((policy) => <option key={policy.policyId} value={policy.policyId}>{policy.displayName}</option>)}</select></label>
    <label className="gi-field"><span className="gi-field-label">Policy version</span><IdentityInput name="policyVersion" type="number" min={1} /></label>
    <label className="gi-field"><span className="gi-field-label">Resource scope</span><select className="gi-input" name="resourceScopeId"><option value="">Unscoped</option>{resourceScopes.map((scope) => <option key={scope.resourceScopeId} value={scope.resourceScopeId}>{scope.displayName}</option>)}</select></label>
    <label className="gi-field gi-checkbox-field"><input type="checkbox" name="includeDescendants" value="true" /><span>Include descendant scopes</span></label>
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">Bind managed policy</IdentityButton>
  </form>;
}
