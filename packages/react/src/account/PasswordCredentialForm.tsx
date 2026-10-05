import type { ReactNode } from "react";
import type { IdentityPasswordCredentialMetadataRecord } from "@generic-identity/contracts/account";
import { IdentityButton, IdentityInput } from "../components/index";

export interface PasswordCredentialFormProps {
  readonly credential?: IdentityPasswordCredentialMetadataRecord | null;
  readonly formAction?: string;
  readonly error?: ReactNode;
}

/** Administrative password-credential form. Authorization remains server-side. */
export function PasswordCredentialForm({ credential = null, formAction, error }: PasswordCredentialFormProps) {
  return (
    <form className="gi-form" method="post" action={formAction} data-gi-component="password-credential-form">
      <label className="gi-field"><span className="gi-field-label">Login identifier</span><IdentityInput name="loginIdentifier" defaultValue={credential?.loginIdentifier ?? ""} autoComplete="username" required /></label>
      <label className="gi-field"><span className="gi-field-label">Password</span><IdentityInput name="password" type="password" autoComplete="new-password" required /></label>
      {credential ? <input type="hidden" name="expectedVersion" value={credential.version} /> : null}
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">{credential ? "Change credential" : "Create credential"}</IdentityButton>
    </form>
  );
}
