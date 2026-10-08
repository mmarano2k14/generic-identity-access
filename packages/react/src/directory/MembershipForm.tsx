import type { ReactNode } from "react";
import type { IdentityTenantMembershipRecord } from "@generic-identity/contracts/directory";
import { IdentityButton, IdentityEntityAutocomplete } from "../components/index";

export interface MembershipFormProps {
  readonly membership?: IdentityTenantMembershipRecord | null;
  readonly tenantId?: string;
  readonly formAction?: string | ((formData: FormData) => void | Promise<void>);
  readonly error?: ReactNode;
}

/** Tenant membership editor. User IDs are only offered for a new membership. */
export function MembershipForm({ membership = null, tenantId, formAction, error }: MembershipFormProps) {
  return (
    <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="membership-form">
      {tenantId ? <input type="hidden" name="tenantId" value={tenantId} /> : null}
      {membership ? (
        <>
          <input type="hidden" name="membershipId" value={membership.membershipId} />
          <input type="hidden" name="expectedVersion" value={membership.version} />
        </>
      ) : (
        <IdentityEntityAutocomplete
          label="Existing user"
          name="userId"
          kind="user"
          required
          hint="Identity-scope administrators may search the global user directory. Select a listed user; only the stable user ID is submitted."
        />
      )}
      <label className="gi-field">
        <span className="gi-field-label">Membership status</span>
        <select className="gi-input" name="status" defaultValue={String(membership?.status ?? 1)}>
          <option value="1">Active</option>
          <option value="2">Inactive</option>
        </select>
      </label>
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">{membership ? "Save membership" : "Add existing user"}</IdentityButton>
    </form>
  );
}
