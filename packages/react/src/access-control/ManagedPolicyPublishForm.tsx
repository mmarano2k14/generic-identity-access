import type { ReactNode } from "react";
import { IdentityButton } from "../components/index";

type ManagedPolicyPublishFormAction = string | ((formData: FormData) => void | Promise<void>);

export interface ManagedPolicyPublishFormProps {
  readonly policyId: string;
  readonly policyVersion: number;
  readonly setDefaultOnly?: boolean;
  readonly formAction?: ManagedPolicyPublishFormAction;
  readonly error?: ReactNode;
}

export function ManagedPolicyPublishForm({ policyId, policyVersion, setDefaultOnly = false, formAction, error }: ManagedPolicyPublishFormProps) {
  return <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="managed-policy-publish-form">
    <input type="hidden" name="policyId" value={policyId} />
    <input type="hidden" name="policyVersion" value={policyVersion} />
    {setDefaultOnly ? <input type="hidden" name="makeDefault" value="true" /> : <label className="gi-field gi-checkbox-field"><input type="checkbox" name="makeDefault" value="true" /><span>Make this the default version</span></label>}
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">{setDefaultOnly ? "Set default" : "Publish version"}</IdentityButton>
  </form>;
}
