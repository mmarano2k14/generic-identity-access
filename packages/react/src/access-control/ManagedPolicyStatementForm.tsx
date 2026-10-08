import type { ReactNode } from "react";
import { IdentityButton } from "../components/index";

type ManagedPolicyStatementFormAction = string | ((formData: FormData) => void | Promise<void>);

export interface ManagedPolicyCapabilityOption {
  readonly value: string;
  readonly label: string;
}

export interface ManagedPolicyStatementFormProps {
  readonly policyId: string;
  readonly policyVersion: number;
  readonly capabilities: readonly ManagedPolicyCapabilityOption[];
  readonly formAction?: ManagedPolicyStatementFormAction;
  readonly error?: ReactNode;
}

/** Statements are selected from the immutable registered model, never free-text authored. */
export function ManagedPolicyStatementForm({ policyId, policyVersion, capabilities, formAction, error }: ManagedPolicyStatementFormProps) {
  return <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="managed-policy-statement-form">
    <input type="hidden" name="policyId" value={policyId} />
    <input type="hidden" name="policyVersion" value={policyVersion} />
    <label className="gi-field"><span className="gi-field-label">Capability</span><select className="gi-input" name="capability" required defaultValue=""><option value="">Select a registered capability</option>{capabilities.map((capability) => <option key={capability.value} value={capability.value}>{capability.label}</option>)}</select><span className="gi-field-hint">The server validates this capability against the policy version's pinned security model.</span></label>
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">Add statement</IdentityButton>
  </form>;
}
