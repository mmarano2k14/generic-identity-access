import type { ComponentProps, ReactNode } from "react";
import { IdentityButton, IdentityInput, IdentityPanel } from "../components/index";

export interface AuthenticatorRevocationFormProps {
  readonly userId: string;
  readonly authenticatorId: string;
  readonly expectedVersion: number;
  readonly recovery?: boolean;
  readonly formAction?: ComponentProps<"form">["action"];
  readonly submitLabel?: string;
  readonly error?: ReactNode;
}

/**
 * Confirmation form for normal or lost-factor recovery revocation.
 *
 * Recovery revocation is intentionally explicit because that backend path may
 * remove the final factor and contains active user sessions.
 */
export function AuthenticatorRevocationForm({
  userId,
  authenticatorId,
  expectedVersion,
  recovery = false,
  formAction,
  submitLabel = recovery ? "Revoke and contain" : "Revoke authenticator",
  error,
}: AuthenticatorRevocationFormProps) {
  return (
    <IdentityPanel title={recovery ? "Lost-factor recovery revoke" : "Authenticator revocation"}>
      <form
        className="gi-form"
        method={typeof formAction === "function" ? undefined : "post"}
        action={formAction}
        data-gi-component="authenticator-revocation-form"
      >
        <input type="hidden" name="userId" value={userId} />
        <input type="hidden" name="authenticatorId" value={authenticatorId} />
        <input type="hidden" name="expectedVersion" value={String(expectedVersion)} />
        <label className="gi-field">
          <span className="gi-field-label">Type REVOKE to confirm</span>
          <IdentityInput name="confirmation" autoComplete="off" required placeholder="REVOKE" />
        </label>
        {recovery ? <p className="gi-help-text">Use this path only for a lost factor. It may revoke the final factor and contain active sessions.</p> : null}
        {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
        <IdentityButton variant="danger" type="submit">{submitLabel}</IdentityButton>
      </form>
    </IdentityPanel>
  );
}
