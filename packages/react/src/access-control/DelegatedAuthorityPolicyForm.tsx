import type { ReactNode } from "react";
import type { IdentityScopeAuthorityPolicyRecord } from "@generic-identity/contracts/access-control";
import { IdentityButton, IdentityInput } from "../components/index";

export interface DelegatedAuthorityPolicyFormProps {
  readonly policy?: IdentityScopeAuthorityPolicyRecord | null;
  readonly formAction?: string;
  readonly error?: ReactNode;
}

export function DelegatedAuthorityPolicyForm({ policy = null, formAction, error }: DelegatedAuthorityPolicyFormProps) {
  return <form className="gi-form" method="post" action={formAction} data-gi-component="delegated-authority-policy-form">
    <label className="gi-field"><span className="gi-field-label">Display name</span><IdentityInput name="displayName" defaultValue={policy?.displayName ?? ""} required /></label>
    <label className="gi-field"><span className="gi-field-label">Status</span><select className="gi-input" name="status" defaultValue={String(policy?.status ?? 1)}><option value="1">Active</option><option value="2">Inactive</option></select></label>
    {policy ? <input type="hidden" name="expectedVersion" value={policy.version} /> : null}
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">{policy ? "Save authority policy" : "Create authority policy"}</IdentityButton>
  </form>;
}
