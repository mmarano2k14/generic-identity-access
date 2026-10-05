import type { ReactNode } from "react";
import { IdentityButton, IdentityInput } from "../components/index";

export interface ApplicationScopeTypeFormProps {
  readonly parentKeys?: readonly string[];
  readonly formAction?: string;
  readonly error?: ReactNode;
}

export function ApplicationScopeTypeForm({ parentKeys = [], formAction, error }: ApplicationScopeTypeFormProps) {
  return (
    <form className="gi-form" method="post" action={formAction} data-gi-component="application-scope-type-form">
      <label className="gi-field"><span className="gi-field-label">Key</span><IdentityInput name="key" required /></label>
      <label className="gi-field"><span className="gi-field-label">Display name</span><IdentityInput name="displayName" required /></label>
      <label className="gi-field"><span className="gi-field-label">Parent scope type</span><select className="gi-input" name="parentKey" defaultValue=""><option value="">None</option>{parentKeys.map((key) => <option key={key} value={key}>{key}</option>)}</select></label>
      <label className="gi-field"><span className="gi-field-label">Tenant attachment</span><select className="gi-input" name="canAttachToTenant" defaultValue="false"><option value="false">Not allowed</option><option value="true">Allowed</option></select></label>
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">Add scope type</IdentityButton>
    </form>
  );
}
