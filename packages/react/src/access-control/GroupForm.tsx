import type { ReactNode } from "react";
import type { IdentityGroupRecord } from "@generic-identity/contracts/access-control";
import { IdentityButton, IdentityInput } from "../components/index";

type GroupFormAction = string | ((formData: FormData) => void | Promise<void>);

export interface GroupFormProps {
  readonly tenantId?: string;
  readonly group?: IdentityGroupRecord | null;
  readonly allowTemplate?: boolean;
  readonly formAction?: GroupFormAction;
  readonly error?: ReactNode;
}

export function GroupForm({ tenantId, group = null, allowTemplate = false, formAction, error }: GroupFormProps) {
  return <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="group-form">
    {tenantId ? <input type="hidden" name="tenantId" value={tenantId} /> : null}
    {group ? <input type="hidden" name="groupId" value={group.groupId} /> : null}
    <label className="gi-field"><span className="gi-field-label">Display name</span><IdentityInput name="displayName" defaultValue={group?.displayName ?? ""} required maxLength={200} /></label>
    <label className="gi-field"><span className="gi-field-label">Status</span><select className="gi-input" name="status" defaultValue={String(group?.status ?? 1)}><option value="1">Active</option><option value="2">Inactive</option></select></label>
    {allowTemplate && group ? <label className="gi-field gi-checkbox-field"><input type="checkbox" name="isTemplate" value="true" defaultChecked={group.isTemplate} /><span>Make available as reusable template</span></label> : null}
    {group ? <input type="hidden" name="expectedVersion" value={group.version} /> : null}
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">{group ? "Save group" : "Create group"}</IdentityButton>
  </form>;
}
