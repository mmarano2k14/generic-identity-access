import type { ReactNode } from "react";
import type { IdentityTenantMembershipRecord } from "@generic-identity/contracts/directory";
import { IdentityButton, IdentityInput } from "../components/index";

export interface MembershipFormProps {
  readonly membership?: IdentityTenantMembershipRecord | null;
  readonly formAction?: string;
  readonly error?: ReactNode;
}

export function MembershipForm({ membership = null, formAction, error }: MembershipFormProps) {
  return (
    <form className="gi-form" method="post" action={formAction} data-gi-component="membership-form">
      {membership ? <input type="hidden" name="expectedVersion" value={membership.version} /> : <label className="gi-field"><span className="gi-field-label">User ID</span><IdentityInput name="userId" required /></label>}
      <label className="gi-field"><span className="gi-field-label">Status</span><select className="gi-input" name="status" defaultValue={String(membership?.status ?? 1)}><option value="1">Active</option><option value="2">Inactive</option></select></label>
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">{membership ? "Save membership" : "Create membership"}</IdentityButton>
    </form>
  );
}
