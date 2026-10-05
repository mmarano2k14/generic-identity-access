import type { ReactNode } from "react";
import type {
  IdentityMfaPolicyRecord,
  IdentityMfaProviderRecord,
  IdentityMfaUserSecurityState,
  IdentityUserAuthenticatorRecord,
} from "@generic-identity/contracts";
import { IdentityEmptyState, IdentityPageFrame, IdentityPanel, IdentityStatus, IdentityTable } from "../components/index";
import { authenticatorStatus, mfaModeLabel } from "./internal";

export interface MfaPageProps {
  readonly providers?: readonly IdentityMfaProviderRecord[];
  readonly policy?: IdentityMfaPolicyRecord | null;
  readonly securityState?: IdentityMfaUserSecurityState | null;
  readonly authenticators?: readonly IdentityUserAuthenticatorRecord[];
  readonly actions?: ReactNode;
  readonly renderAuthenticatorActions?: (authenticator: IdentityUserAuthenticatorRecord) => ReactNode;
}

export function MfaPage({ providers = [], policy = null, securityState = null, authenticators = [], actions, renderAuthenticatorActions }: MfaPageProps) {
  return (
    <IdentityPageFrame title="Multi-factor authentication" description="MFA policy and factor metadata. Provider secrets are never rendered." actions={actions}>
      <IdentityPanel title="Policy">
        <dl className="gi-definition-list"><div><dt>Mode</dt><dd>{mfaModeLabel(policy?.mode)}</dd></div><div><dt>Allowed providers</dt><dd>{policy?.allowedProviders.join(", ") || "—"}</dd></div><div><dt>Version</dt><dd>{policy?.version ?? "—"}</dd></div></dl>
      </IdentityPanel>
      <IdentityPanel title="Available providers">
        {providers.length === 0 ? <IdentityEmptyState title="No providers" /> : (
          <IdentityTable caption="MFA providers">
            <thead><tr><th scope="col">Provider</th><th scope="col">Key</th><th scope="col">Capabilities</th></tr></thead>
            <tbody>{providers.map((provider) => <tr key={provider.key}><td>{provider.displayName}</td><td><code>{provider.key}</code></td><td>{provider.capabilities.join(", ") || "—"}</td></tr>)}</tbody>
          </IdentityTable>
        )}
      </IdentityPanel>
      {securityState ? <IdentityPanel title="Security state"><dl className="gi-definition-list"><div><dt>MFA required</dt><dd><IdentityStatus label={securityState.mfaRequired ? "Yes" : "No"} tone={securityState.mfaRequired ? "warning" : "neutral"} /></dd></div><div><dt>Policy satisfied</dt><dd><IdentityStatus label={securityState.satisfiesCurrentPolicy ? "Yes" : "No"} tone={securityState.satisfiesCurrentPolicy ? "positive" : "negative"} /></dd></div><div><dt>Primary factor</dt><dd>{securityState.hasActivePrimaryFactor ? "Active" : "None"}</dd></div><div><dt>Recovery factor</dt><dd>{securityState.hasActiveRecoveryFactor ? "Active" : "None"}</dd></div></dl></IdentityPanel> : null}
      <IdentityPanel title="Authenticators">
        {authenticators.length === 0 ? <IdentityEmptyState title="No authenticators" /> : <IdentityTable caption="Authenticators"><thead><tr><th scope="col">Name</th><th scope="col">Provider</th><th scope="col">Status</th><th scope="col">Created</th><th scope="col">Last used</th>{renderAuthenticatorActions ? <th scope="col">Actions</th> : null}</tr></thead><tbody>{authenticators.map((authenticator) => <tr key={authenticator.authenticatorId}><td>{authenticator.displayName}</td><td><code>{authenticator.providerKey}</code></td><td>{authenticatorStatus(authenticator.status)}</td><td>{authenticator.createdAt}</td><td>{authenticator.lastUsedAt ?? "—"}</td>{renderAuthenticatorActions ? <td>{renderAuthenticatorActions(authenticator)}</td> : null}</tr>)}</tbody></IdentityTable>}
      </IdentityPanel>
    </IdentityPageFrame>
  );
}
