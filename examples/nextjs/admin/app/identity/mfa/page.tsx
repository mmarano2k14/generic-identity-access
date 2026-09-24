import { AdminDetailCard } from "../../../components/AdminDetailCard";
import { AdminEmptyState } from "../../../components/AdminEmptyState";
import { AdminField } from "../../../components/AdminField";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";

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

  const policyFields = policy ? [
    { label: "Mode", value: policy.mode === 1 ? "Disabled" : policy.mode === 2 ? "Optional" : "Required" },
    { label: "Allowed providers", value: policy.allowedProviders.length === 0 ? "None" : policy.allowedProviders.join(", ") },
    { label: "Version", value: `v${policy.version}` },
  ] : [];

  const stateFields = state ? [
    { label: "Policy", value: state.policyConfigured ? (state.policyMode === 1 ? "Disabled" : state.policyMode === 2 ? "Optional" : "Required") : "Not configured" },
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
        description="Inspect application MFA policy, installed factor providers, and the effective factor state of a user without exposing provider-owned security material."
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

      <section className="ia-card">
        <div className="ia-card-heading"><div><p className="ia-card-kicker">Authenticator lifecycle</p><h2>Generic authenticator metadata</h2><p>Provider secrets and credential payloads are intentionally excluded.</p></div></div>
        {authenticators.length === 0 ? (
          <AdminEmptyState title="No authenticators loaded" description={userId ? "This user has no generic authenticator records." : "Select a user to inspect enrolled authenticators."} />
        ) : (
          <div className="ia-detail-list">
            {authenticators.map((authenticator) => (
              <div className="ia-detail-row" key={authenticator.authenticatorId}>
                <span>{authenticator.displayName}</span>
                <strong>{authenticator.providerKey} · {authenticator.status === 1 ? "Pending" : authenticator.status === 2 ? "Active" : "Revoked"} · v{authenticator.version}</strong>
              </div>
            ))}
          </div>
        )}
      </section>
    </section>
  );
}
