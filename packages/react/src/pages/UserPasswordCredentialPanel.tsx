import type { ReactNode } from "react";
import type { IdentityPasswordCredentialMetadataRecord } from "@generic-identity/contracts/account";
import { IdentityPanel } from "../components/index";

export interface UserPasswordCredentialPanelProps {
  readonly credential: IdentityPasswordCredentialMetadataRecord | null;
  readonly error?: ReactNode;
  readonly actions?: ReactNode;
}

/** Secret-free credential metadata, never a dump of password/hash/token state. */
export function UserPasswordCredentialPanel({ credential, error, actions }: UserPasswordCredentialPanelProps) {
  return (
    <IdentityPanel title="Password credential" description="Account login data is separate from tenant and group access.">
      {error ? <div className="gi-form-error" role="alert">{error}</div> : credential ? (
        <dl className="gi-definition-list">
          <div><dt>Login identifier</dt><dd>{credential.loginIdentifier}</dd></div>
          <div><dt>Failed attempts</dt><dd>{credential.failedAccessCount}</dd></div>
          <div><dt>Lockout until</dt><dd>{credential.lockoutUntil ?? "—"}</dd></div>
          <div><dt>Credential version</dt><dd>{credential.version}</dd></div>
        </dl>
      ) : <p>No password credential is configured for this identity.</p>}
      {actions}
    </IdentityPanel>
  );
}
