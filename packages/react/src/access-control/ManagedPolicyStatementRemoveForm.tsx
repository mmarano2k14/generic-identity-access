import type { ReactNode } from "react";
import { IdentityButton, IdentityInput } from "../components/index";

type ManagedPolicyStatementRemoveFormAction = string | ((formData: FormData) => void | Promise<void>);

export interface ManagedPolicyStatementRemoveFormProps {
  readonly policyId: string;
  readonly policyVersion: number;
  readonly statementId: string;
  readonly formAction?: ManagedPolicyStatementRemoveFormAction;
  readonly error?: ReactNode;
}

export function ManagedPolicyStatementRemoveForm({ policyId, policyVersion, statementId, formAction, error }: ManagedPolicyStatementRemoveFormProps) {
  return <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="managed-policy-statement-remove-form">
    <input type="hidden" name="policyId" value={policyId} />
    <input type="hidden" name="policyVersion" value={policyVersion} />
    <input type="hidden" name="statementId" value={statementId} />
    <label className="gi-field"><span className="gi-field-label">Type REMOVE to confirm</span><IdentityInput name="confirmation" autoComplete="off" required placeholder="REMOVE" /></label>
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="secondary" type="submit">Remove statement</IdentityButton>
  </form>;
}
