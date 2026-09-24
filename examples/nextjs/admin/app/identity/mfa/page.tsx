import { AdminCheckboxField, AdminField, AdminSelectField } from "../../../components/AdminField";
import { AdminDetailCard } from "../../../components/AdminDetailCard";
import { AdminEmptyState } from "../../../components/AdminEmptyState";
import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import {
  createMfaPolicyAction,
  recoveryRevokeMfaAuthenticatorAction,
  revokeMfaAuthenticatorAction,
  updateMfaPolicyAction,
} from "../actions";

function modeLabel(mode: number): string {
  return mode === 1 ? "Disabled" : mode === 2 ? "Optional" : "Required";
}

export default async function MfaPage({ searchParams }: { readonly searchParams: Promise<{ readonly userId?: string }> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const context = request.administrationContext;
  const { userId } = await searchParams;
  const [providers, policy] = await Promise.all([
    request.client.administration.mfa.listProviders(context),
    request.client.administration.mfa.getPolicy(context),
  ]);

  const [state, authenticators] = userId ? await Promise.all([
    request.client.administration.mfa.getUserSecurityState(context, userId),
    request.client.administration.mfa.listAuthenticators(context, userId),
  ]) : [null, []];

  const policyAction = policy ? (
    <AdminMutationDialog title="Edit MFA policy" description="Change the application MFA mode and the provider keys permitted by the generic policy guard." triggerLabel="Edit policy" submitLabel="Save policy" action={updateMfaPolicyAction} triggerVariant="secondary" triggerIcon="edit">
      <input type="hidden" name="expectedVersion" value={policy.version} />
      <AdminSelectField label="Policy mode" name="mode" defaultValue={String(policy.mode)} required>
        <option value="1">Disabled</option>
        <option value="2">Optional</option>
        <option value="3">Required</option>
      </AdminSelectField>
      <fieldset className="ia-checkset">
        <legend>Allowed providers</legend>
        {providers.map((provider) => (
          <AdminCheckboxField key={provider.key} label={provider.displayName} name="allowedProviders" value={provider.key} defaultChecked={policy.allowedProviders.includes(provider.key)} hint={provider.key} />
        ))}
      </fieldset>
    </AdminMutationDialog>
  ) : (
    <AdminMutationDialog title="Create MFA policy" description="Create the application-level provider-neutral MFA policy." triggerLabel="Create policy" submitLabel="Create policy" action={createMfaPolicyAction}>
      <AdminSelectField label="Policy mode" name="mode" defaultValue="2" required>
        <option value="1">Disabled</option>
        <option value="2">Optional</option>
        <option value="3">Required</option>
      </AdminSelectField>
      <fieldset className="ia-checkset">
        <legend>Allowed providers</legend>
        {providers.map((provider) => (
          <AdminCheckboxField key={provider.key} label={provider.displayName} name="allowedProviders" value={provider.key} defaultChecked hint={provider.key} />
        ))}
      </fieldset>
    </AdminMutationDialog>
  );

  const policyFields = policy ? [
    { label: "Mode", value: modeLabel(policy.mode) },
    { label: "Allowed providers", value: policy.allowedProviders.length === 0 ? "None" : policy.allowedProviders.join(", ") },
    { label: "Version", value: `v${policy.version}` },
  ] : [];

  const stateFields = state ? [
    { label: "Policy", value: state.policyConfigured ? (state.policyMode ? modeLabel(state.policyMode) : "Configured") : "Not configured" },
    { label: "Policy satisfied", value: state.satisfiesCurrentPolicy ? "Yes" : "No" },
    { label: "Active verification factor", value: state.hasActiveVerificationFactor ? "Yes" : "No" },
    { label: "Primary factor", value: state.hasActivePrimaryFactor ? "Yes" : "No" },
    { label: "Recovery factor", value: state.hasActiveRecoveryFactor ? "Yes" : "No" },
    { label: "Verification providers", value: state.activeVerificationProviders.length === 0 ? "None" : state.activeVerificationProviders.join(", ") },
  ] : [];

  return (
    <section className="ia-page">
      <AdminPageHeader
        eyebrow="Security"
        badge="Provider-neutral MFA"
        title="Multi-factor authentication"
        description="Edit application MFA policy, inspect effective factor state, and revoke authenticators through the server-authorized lifecycle APIs."
        actions={policyAction}
      />
      <AdminSecurityBanner
        title="The core owns policy and lifecycle, not provider secrets."
        description="TOTP secrets, WebAuthn public-credential state, and recovery-code hashes remain owned by their dedicated providers. Generic administration exposes only safe lifecycle metadata."
      />
      <div className="ia-card-grid">
        <AdminDetailCard
          title="MFA policy"
          description="Application-level policy controlling whether additional factors are disabled, optional, or required."
          fields={policyFields}
          emptyMessage="No MFA policy has been configured for this application yet."
        />
        <section className="ia-card">
          <div className="ia-card-heading">
            <div><p className="ia-card-kicker">Provider registry</p><h2>Installed factor providers</h2><p>Provider capabilities are declared by the host and filtered by application policy at execution time.</p></div>
          </div>
          {providers.length === 0 ? (
            <AdminEmptyState title="No providers installed" description="No authentication-factor provider is registered in this host process." />
          ) : (
            <div className="ia-detail-list">
              {providers.map((provider) => (
                <div className="ia-detail-row" key={provider.key}>
                  <span>{provider.displayName}</span>
                  <strong>{provider.key} · {provider.capabilities.join(" / ")}</strong>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>

      <section className="ia-card ia-lookup-card">
        <div className="ia-card-heading"><div><p className="ia-card-kicker">User security</p><h2>Inspect effective MFA state</h2><p>Resolve active generic authenticator metadata against the current application policy and installed provider capabilities.</p></div></div>
        <form className="ia-inline-form" method="get">
          <AdminField label="User ID" name="userId" defaultValue={userId ?? ""} required autoComplete="off" />
          <button className="ia-button ia-button-secondary" type="submit">Inspect MFA state</button>
        </form>
      </section>

      <AdminDetailCard
        title={state ? "Effective MFA state" : "User MFA state"}
        description={state ? "This view is derived from the current application policy, provider registry, and active generic authenticator metadata." : "No user is currently selected."}
        fields={stateFields}
        emptyMessage={userId ? "No effective MFA state could be displayed for this user." : "Enter a user ID above to inspect its MFA state."}
      />

      {userId ? (
        <AdminEntityTable
          title="Authenticator lifecycle"
          description="Provider-owned secrets stay hidden. Active or pending authenticators can be revoked; lost-factor recovery revocation also contains active user sessions."
          entityLabel="authenticators"
          rows={authenticators.map((authenticator) => ({
            id: authenticator.authenticatorId,
            name: authenticator.displayName,
            status: authenticator.status === 1 ? "Pending" : authenticator.status === 2 ? "Active" : "Revoked",
            version: authenticator.version,
            actions: authenticator.status === 3 ? undefined : (
              <>
                <AdminMutationDialog title="Revoke authenticator" description="Revoke this authenticator through the normal policy-preserving path. Type REVOKE to confirm." triggerLabel="Revoke" submitLabel="Revoke authenticator" action={revokeMfaAuthenticatorAction} dangerous compact>
                  <input type="hidden" name="userId" value={userId} />
                  <input type="hidden" name="authenticatorId" value={authenticator.authenticatorId} />
                  <input type="hidden" name="expectedVersion" value={authenticator.version} />
                  <AdminField label="Confirmation" name="confirmation" required placeholder="REVOKE" autoComplete="off" />
                </AdminMutationDialog>
                <AdminMutationDialog title="Lost-factor recovery revoke" description="Use only when the factor is lost. This path may revoke the final factor and contains active sessions. Type REVOKE to confirm." triggerLabel="Lost factor" submitLabel="Revoke and contain" action={recoveryRevokeMfaAuthenticatorAction} dangerous compact>
                  <input type="hidden" name="userId" value={userId} />
                  <input type="hidden" name="authenticatorId" value={authenticator.authenticatorId} />
                  <input type="hidden" name="expectedVersion" value={authenticator.version} />
                  <AdminField label="Confirmation" name="confirmation" required placeholder="REVOKE" autoComplete="off" />
                </AdminMutationDialog>
              </>
            ),
          }))}
        />
      ) : null}
    </section>
  );
}
