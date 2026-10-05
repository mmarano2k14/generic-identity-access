import type { ReactNode } from "react";
import type { IdentityManagedPolicyRecord } from "@generic-identity/contracts/access-control";
import { IdentityButton, IdentityInput } from "../components/index";
export interface ManagedPolicyFormProps { readonly policy?: IdentityManagedPolicyRecord | null; readonly formAction?: string; readonly error?: ReactNode; }
export function ManagedPolicyForm({ policy = null, formAction, error }: ManagedPolicyFormProps) {
  return <form className="gi-form" method="post" action={formAction} data-gi-component="managed-policy-form">
    <label className="gi-field"><span className="gi-field-label">Policy key</span><IdentityInput name="policyKey" defaultValue={policy?.policyKey ?? ""} required /></label>
    <label className="gi-field"><span className="gi-field-label">Display name</span><IdentityInput name="displayName" defaultValue={policy?.displayName ?? ""} required /></label>
    <label className="gi-field"><span className="gi-field-label">Status</span><select className="gi-input" name="status" defaultValue={String(policy?.status ?? 1)}><option value="1">Active</option><option value="2">Inactive</option></select></label>
    {policy ? <input type="hidden" name="expectedVersion" value={policy.version} /> : null}
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">{policy ? "Save managed policy" : "Create managed policy"}</IdentityButton>
  </form>;
}
