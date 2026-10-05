import type { ReactNode } from "react";
import type { IdentityScopeAuthorityGroupRecord } from "@generic-identity/contracts/access-control";
import { IdentityButton, IdentityInput } from "../components/index";
export interface DelegatedAuthorityGroupFormProps { readonly group?: IdentityScopeAuthorityGroupRecord | null; readonly formAction?: string; readonly error?: ReactNode; }
export function DelegatedAuthorityGroupForm({ group = null, formAction, error }: DelegatedAuthorityGroupFormProps) {
  return <form className="gi-form" method="post" action={formAction} data-gi-component="delegated-authority-group-form">
    <label className="gi-field"><span className="gi-field-label">Display name</span><IdentityInput name="displayName" defaultValue={group?.displayName ?? ""} required /></label>
    <label className="gi-field"><span className="gi-field-label">Status</span><select className="gi-input" name="status" defaultValue={String(group?.status ?? 1)}><option value="1">Active</option><option value="2">Inactive</option></select></label>
    {group ? <input type="hidden" name="expectedVersion" value={group.version} /> : null}
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">{group ? "Save authority group" : "Create authority group"}</IdentityButton>
  </form>;
}
