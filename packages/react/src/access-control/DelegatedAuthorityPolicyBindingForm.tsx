import type { ReactNode } from "react";
import { IdentityButton, IdentityEntityAutocomplete } from "../components/index";

type DelegatedAuthorityPolicyBindingFormAction = string | ((formData: FormData) => void | Promise<void>);

export interface DelegatedAuthorityPolicyBindingFormProps {
  readonly groupId: string;
  readonly defaultPolicyId?: string;
  readonly excludePolicyIds?: readonly string[];
  readonly formAction?: DelegatedAuthorityPolicyBindingFormAction;
  readonly error?: ReactNode;
}

export function DelegatedAuthorityPolicyBindingForm({ groupId, defaultPolicyId = "", excludePolicyIds = [], formAction, error }: DelegatedAuthorityPolicyBindingFormProps) {
  return <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="delegated-authority-policy-binding-form">
    <input type="hidden" name="groupId" value={groupId} />
    <IdentityEntityAutocomplete
      label="Authority policy"
      name="policyId"
      kind="authority-policy"
      defaultValue={defaultPolicyId}
      excludeIds={excludePolicyIds}
      required
      hint="Type at least 3 characters of the authority policy display name, or enter the full ID."
    />
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">Bind authority policy</IdentityButton>
  </form>;
}
