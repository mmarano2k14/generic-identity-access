import { AdminDetailCard } from "../../../components/AdminDetailCard";
import { AdminEmptyState } from "../../../components/AdminEmptyState";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";

export default async function MfaPage() {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const context = request.administrationContext;
  const [providers, policy] = await Promise.all([
    request.client.administration.mfa.listProviders(context),
    request.client.administration.mfa.getPolicy(context),
  ]);

  const policyFields = policy ? [
    { label: "Mode", value: policy.mode === 1 ? "Disabled" : policy.mode === 2 ? "Optional" : "Required" },
    { label: "Allowed providers", value: policy.allowedProviders.length === 0 ? "None" : policy.allowedProviders.join(", ") },
    { label: "Version", value: `v${policy.version}` },
  ] : [];

  return (
    <section className="ia-page">
      <AdminPageHeader
        eyebrow="Security"
        badge="Provider-neutral foundation"
        title="Multi-factor authentication"
        description="Inspect the generic MFA policy and authentication-factor providers registered by the host. Provider-specific enrollment arrives in dedicated provider packs."
      />
      <AdminSecurityBanner
        title="The core owns policy and lifecycle, not provider secrets."
        description="TOTP secrets, WebAuthn credential material, and recovery-code material remain owned by their dedicated providers and are never stored in generic authenticator metadata."
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
            <div><p className="ia-card-kicker">Provider registry</p><h2>Installed factor providers</h2><p>Provider capabilities are declared by the host and resolved through the generic registry.</p></div>
          </div>
          {providers.length === 0 ? (
            <AdminEmptyState title="No providers installed" description="The generic MFA foundation is active. TOTP, recovery, and WebAuthn providers are delivered by later provider packs." />
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
    </section>
  );
}
