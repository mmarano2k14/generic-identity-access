import type { ReactNode } from "react";
import type { IdentityTenantRecord } from "@generic-identity/contracts/directory";
import { IdentityButton, IdentityInput } from "../components/index";

export interface TenantFormProps {
  readonly tenant?: IdentityTenantRecord | null;
  readonly formAction?: string;
  readonly error?: ReactNode;
}

export function TenantForm({ tenant = null, formAction, error }: TenantFormProps) {
  return (
    <form className="gi-form" method="post" action={formAction} data-gi-component="tenant-form">
      <label className="gi-field"><span className="gi-field-label">Display name</span><IdentityInput name="displayName" defaultValue={tenant?.displayName ?? ""} required /></label>
      <label className="gi-field"><span className="gi-field-label">Status</span><select className="gi-input" name="status" defaultValue={String(tenant?.status ?? 1)}><option value="1">Active</option><option value="2">Inactive</option></select></label>
      {tenant ? <input type="hidden" name="expectedVersion" value={tenant.version} /> : null}
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">{tenant ? "Save tenant" : "Create tenant"}</IdentityButton>
    </form>
  );
}
