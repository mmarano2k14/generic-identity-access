import type { ComponentProps, ReactNode } from "react";
import type { IdentityPasswordCredentialMetadataRecord } from "@generic-identity/contracts/account";
import { IdentityButton, IdentityInput } from "../components/index";

export interface PasswordCredentialFormProps {
  readonly userId?: string;
  readonly tenantId?: string;
  readonly credential?: IdentityPasswordCredentialMetadataRecord | null;
  readonly formAction?: ComponentProps<"form">["action"];
  readonly error?: ReactNode;
}

/** Passwords are submitted to a server action only and are never re-rendered. */
export function PasswordCredentialForm({
  userId,
  tenantId,
  credential = null,
  formAction,
  error,
}: PasswordCredentialFormProps) {
  return (
    <form
      className="gi-form"
      method={typeof formAction === "function" ? undefined : "post"}
      action={formAction}
      data-gi-component="password-credential-form"
    >
      {tenantId ? <input type="hidden" name="tenantId" value={tenantId} /> : null}
      {(userId ?? credential?.userId) ? (
        <input type="hidden" name="userId" value={userId ?? credential?.userId} />
      ) : null}
      <label className="gi-field">
        <span className="gi-field-label">Login identifier</span>
        <IdentityInput
          name="loginIdentifier"
          defaultValue={credential?.loginIdentifier ?? ""}
          autoComplete="username"
          required
          maxLength={320}
        />
      </label>
      <label className="gi-field">
        <span className="gi-field-label">{credential ? "New password" : "Password"}</span>
        <IdentityInput
          name="password"
          type="password"
          autoComplete="new-password"
          minLength={12}
          maxLength={256}
          required
        />
      </label>
      {credential ? <input type="hidden" name="expectedVersion" value={credential.version} /> : null}
      <p className="gi-field-hint">12–256 characters. Secret values are not returned or displayed.</p>
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">
        {credential ? "Change password credential" : "Set password credential"}
      </IdentityButton>
    </form>
  );
}
