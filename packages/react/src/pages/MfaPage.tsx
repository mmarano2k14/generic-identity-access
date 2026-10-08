import type { ReactNode } from "react";
import type {
  IdentityMfaPolicyRecord,
  IdentityMfaProviderRecord,
  IdentityMfaUserSecurityState,
  IdentityUserAuthenticatorRecord,
  IdentityUserRecord,
} from "@generic-identity/contracts";
import {
  IdentityEmptyState,
  IdentityPageFrame,
  IdentityPanel,
  IdentityStatus,
  IdentityTable,
} from "../components/index";
import { authenticatorStatus, mfaModeLabel } from "./internal";

export interface MfaPageProps {
  readonly providers?: readonly IdentityMfaProviderRecord[];
  readonly policy?: IdentityMfaPolicyRecord | null;
  readonly selectedUser?: IdentityUserRecord | null;
  readonly securityState?: IdentityMfaUserSecurityState | null;
  readonly authenticators?: readonly IdentityUserAuthenticatorRecord[];
  readonly actions?: ReactNode;
  readonly userLookup?: ReactNode;
  readonly renderAuthenticatorActions?: (authenticator: IdentityUserAuthenticatorRecord) => ReactNode;
}

export function MfaPage({
  providers = [],
  policy = null,
  selectedUser = null,
  securityState = null,
  authenticators = [],
  actions,
  userLookup,
  renderAuthenticatorActions,
}: MfaPageProps) {
  return (
    <IdentityPageFrame
      title="Multi-factor authentication"
      description="Provider-neutral MFA policy, effective factor state and authenticator lifecycle. Provider secrets are never rendered."
      actions={actions}
    >
      <IdentityPanel title="Policy">
        <dl className="gi-definition-list">
          <div><dt>Mode</dt><dd>{mfaModeLabel(policy?.mode)}</dd></div>
          <div><dt>Allowed providers</dt><dd>{policy?.allowedProviders.join(", ") || "—"}</dd></div>
          <div><dt>Version</dt><dd>{policy?.version ?? "—"}</dd></div>
        </dl>
      </IdentityPanel>

      <IdentityPanel title="Available providers">
        {providers.length === 0 ? <IdentityEmptyState title="No providers" /> : (
          <IdentityTable caption="MFA providers">
            <thead><tr><th scope="col">Provider</th><th scope="col">Key</th><th scope="col">Capabilities</th></tr></thead>
            <tbody>{providers.map((provider) => <tr key={provider.key}><td>{provider.displayName}</td><td><code>{provider.key}</code></td><td>{provider.capabilities.join(", ") || "—"}</td></tr>)}</tbody>
          </IdentityTable>
        )}
      </IdentityPanel>

      {userLookup ? <IdentityPanel title="Inspect effective MFA state">{userLookup}</IdentityPanel> : null}

      {selectedUser ? (
        <IdentityPanel title="Selected user">
          <dl className="gi-definition-list">
            <div><dt>Display name</dt><dd>{selectedUser.displayName}</dd></div>
            <div><dt>User ID</dt><dd><code>{selectedUser.userId}</code></dd></div>
            <div><dt>Status</dt><dd>{selectedUser.status === 1 ? "Active" : "Inactive"}</dd></div>
          </dl>
        </IdentityPanel>
      ) : null}

      {securityState ? <IdentityPanel title="Effective MFA state">
        <dl className="gi-definition-list">
          <div><dt>Policy configured</dt><dd>{securityState.policyConfigured ? "Yes" : "No"}</dd></div>
          <div><dt>MFA required</dt><dd><IdentityStatus label={securityState.mfaRequired ? "Yes" : "No"} tone={securityState.mfaRequired ? "warning" : "neutral"} /></dd></div>
          <div><dt>Policy satisfied</dt><dd><IdentityStatus label={securityState.satisfiesCurrentPolicy ? "Yes" : "No"} tone={securityState.satisfiesCurrentPolicy ? "positive" : "negative"} /></dd></div>
          <div><dt>Verification factor</dt><dd>{securityState.hasActiveVerificationFactor ? "Active" : "None"}</dd></div>
          <div><dt>Primary factor</dt><dd>{securityState.hasActivePrimaryFactor ? "Active" : "None"}</dd></div>
          <div><dt>Recovery factor</dt><dd>{securityState.hasActiveRecoveryFactor ? "Active" : "None"}</dd></div>
          <div><dt>Verification providers</dt><dd>{securityState.activeVerificationProviders.join(", ") || "None"}</dd></div>
        </dl>
      </IdentityPanel> : null}

      <IdentityPanel title="Authenticator lifecycle">
        {!selectedUser ? <IdentityEmptyState title="Select a user to inspect authenticators" /> : authenticators.length === 0 ? (
          <IdentityEmptyState title="No authenticators" />
        ) : (
          <IdentityTable caption="Authenticators">
            <thead><tr><th scope="col">Name</th><th scope="col">Provider</th><th scope="col">Status</th><th scope="col">Version</th><th scope="col">Created</th><th scope="col">Last used</th>{renderAuthenticatorActions ? <th scope="col">Actions</th> : null}</tr></thead>
            <tbody>{authenticators.map((authenticator) => <tr key={authenticator.authenticatorId}><td>{authenticator.displayName}</td><td><code>{authenticator.providerKey}</code></td><td>{authenticatorStatus(authenticator.status)}</td><td>{authenticator.version}</td><td>{authenticator.createdAt}</td><td>{authenticator.lastUsedAt ?? "—"}</td>{renderAuthenticatorActions ? <td>{renderAuthenticatorActions(authenticator)}</td> : null}</tr>)}</tbody>
          </IdentityTable>
        )}
      </IdentityPanel>
    </IdentityPageFrame>
  );
}
