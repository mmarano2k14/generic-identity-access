import type { ReactNode } from "react";
import { IdentityButton, IdentityEntityAutocomplete } from "../components/index";

type ManagedPolicyBindingFormAction = string | ((formData: FormData) => void | Promise<void>);

export interface ManagedPolicyBindingFormProps {
  readonly tenantId: string;
  readonly groupId: string;
  readonly allowResourceScope?: boolean;
  readonly formAction?: ManagedPolicyBindingFormAction;
  readonly error?: ReactNode;
}

export function ManagedPolicyBindingForm({ tenantId, groupId, allowResourceScope = true, formAction, error }: ManagedPolicyBindingFormProps) {
  return <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="managed-policy-binding-form">
    <input type="hidden" name="tenantId" value={tenantId} />
    <input type="hidden" name="groupId" value={groupId} />
    <IdentityEntityAutocomplete
      label="Managed policy"
      name="managedPolicyId"
      kind="managed-policy"
      tenantId={tenantId}
      required
      hint="Only active policies with a published default version are selectable."
    />
    {allowResourceScope ? <IdentityEntityAutocomplete
      label="Resource scope"
      name="resourceScopeId"
      kind="resource-scope"
      tenantId={tenantId}
      emptyLabel="No resource scope"
      hint="Optional tenant-owned active resource scope."
    /> : null}
    <label className="gi-field gi-checkbox-field"><input type="checkbox" name="includeDescendants" value="true" /><span>Include descendant scopes</span></label>
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">Bind managed policy</IdentityButton>
  </form>;
}
