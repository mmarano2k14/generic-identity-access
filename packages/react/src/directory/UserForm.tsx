import type { ComponentProps, ReactNode } from "react";
import type { IdentityUserRecord } from "@generic-identity/contracts/directory";
import { IdentityButton, IdentityInput } from "../components/index";

export interface UserFormProps {
  readonly user?: IdentityUserRecord | null;
  readonly tenantId?: string;
  readonly formAction?: ComponentProps<"form">["action"];
  readonly error?: ReactNode;
}

/** Account metadata only; tenant membership and credentials are separate. */
export function UserForm({ user = null, tenantId, formAction, error }: UserFormProps) {
  return (
    <form
      className="gi-form"
      method={typeof formAction === "function" ? undefined : "post"}
      action={formAction}
      data-gi-component="user-form"
    >
      {user ? <input type="hidden" name="userId" value={user.userId} /> : null}
      {tenantId ? <input type="hidden" name="tenantId" value={tenantId} /> : null}
      <label className="gi-field">
        <span className="gi-field-label">Display name</span>
        <IdentityInput name="displayName" defaultValue={user?.displayName ?? ""} required maxLength={200} />
      </label>
      <label className="gi-field">
        <span className="gi-field-label">Status</span>
        <select className="gi-input" name="status" defaultValue={String(user?.status ?? 1)}>
          <option value="1">Active</option>
          <option value="2">Inactive</option>
        </select>
      </label>
      {user ? <input type="hidden" name="expectedVersion" value={user.version} /> : null}
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">{user ? "Save user" : "Create user"}</IdentityButton>
    </form>
  );
}
