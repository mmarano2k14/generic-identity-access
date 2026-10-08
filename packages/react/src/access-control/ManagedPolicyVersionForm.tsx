import type { ReactNode } from "react";
import { IdentityButton, IdentityInput } from "../components/index";

type ManagedPolicyVersionFormAction = string | ((formData: FormData) => void | Promise<void>);

export interface ManagedPolicyVersionModelOption {
  readonly modelVersion: number;
  readonly rbacProject: string;
}

export interface ManagedPolicyVersionFormProps {
  readonly policyId: string;
  readonly nextPolicyVersion: number;
  readonly models: readonly ManagedPolicyVersionModelOption[];
  readonly formAction?: ManagedPolicyVersionFormAction;
  readonly error?: ReactNode;
}

export function ManagedPolicyVersionForm({ policyId, nextPolicyVersion, models, formAction, error }: ManagedPolicyVersionFormProps) {
  return <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="managed-policy-version-form">
    <input type="hidden" name="policyId" value={policyId} />
    <label className="gi-field"><span className="gi-field-label">Policy version</span><IdentityInput name="policyVersion" type="number" min={1} step={1} defaultValue={nextPolicyVersion} required /></label>
    <label className="gi-field"><span className="gi-field-label">Security model version</span><select className="gi-input" name="modelVersion" required defaultValue=""><option value="">Select a registered model</option>{models.map((model) => <option key={model.modelVersion} value={model.modelVersion}>Model v{model.modelVersion} · {model.rbacProject}</option>)}</select><span className="gi-field-hint">Draft statements can only use capabilities declared by the selected immutable security-model version.</span></label>
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">Create draft</IdentityButton>
  </form>;
}
