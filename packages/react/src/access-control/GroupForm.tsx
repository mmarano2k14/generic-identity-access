import type { ReactNode } from "react";
import type { IdentityGroupRecord } from "@generic-identity/contracts/access-control";
import { IdentityButton, IdentityInput } from "../components/index";
export interface GroupFormProps { readonly group?: IdentityGroupRecord | null; readonly formAction?: string; readonly error?: ReactNode; }
export function GroupForm({ group = null, formAction, error }: GroupFormProps) {
  return <form className="gi-form" method="post" action={formAction} data-gi-component="group-form">
    <label className="gi-field"><span className="gi-field-label">Display name</span><IdentityInput name="displayName" defaultValue={group?.displayName ?? ""} required /></label>
    <label className="gi-field"><span className="gi-field-label">Status</span><select className="gi-input" name="status" defaultValue={String(group?.status ?? 1)}><option value="1">Active</option><option value="2">Inactive</option></select></label>
    {group ? <input type="hidden" name="expectedVersion" value={group.version} /> : null}
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">{group ? "Save group" : "Create group"}</IdentityButton>
  </form>;
}
