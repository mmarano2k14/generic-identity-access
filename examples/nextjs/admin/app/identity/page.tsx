import { AdminFeatureCard } from "../../components/AdminFeatureCard";
import { AdminIcon } from "../../components/AdminIcon";
import { AdminMetricCard } from "../../components/AdminMetricCard";
import { AdminPageHeader } from "../../components/AdminPageHeader";
import { AdminSecurityBanner } from "../../components/AdminSecurityBanner";
import { IdentityAccessAdminRequest } from "../../server/IdentityAccessAdminRequest";

export default async function IdentityOverviewPage() {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const [users, tenants, groups, policies] = await Promise.all([
    request.client.administration.users.list(request.administrationContext, { limit: 50 }),
    request.client.administration.tenants.list(request.administrationContext, { limit: 50 }),
    request.client.administration.groups.list(request.tenantContext(), { limit: 50 }),
    request.client.administration.policies.list(request.tenantContext(), { limit: 50 }),
  ]);
  const visibleCount = (count: number) => count >= 50 ? "50+" : String(count);

  return (
    <section className="ia-page ia-overview-page">
      <AdminPageHeader
        eyebrow="Security control center"
        badge="Server protected"
        title="Identity administration, without the noise."
        description="Manage identity boundaries, tenant access, authorization structures, and active sessions through one controlled administration surface."
      />

      <section className="ia-overview-hero">
        <div className="ia-overview-hero-content">
          <span className="ia-hero-icon"><AdminIcon name="shield" /></span>
          <div>
            <p className="ia-card-kicker">Trusted administration plane</p>
            <h2>Every visible action still ends at server-side authorization.</h2>
            <p>Navigation filtering improves the experience, but the .NET API, current session state, resource scope, and external RBAC engine remain authoritative.</p>
          </div>
        </div>
        <div className="ia-hero-protocols" aria-label="Active security architecture">
          <span>OIDC + PKCE</span><span>Refresh rotation</span><span>Multi-key JWKS</span><span>Bearer revalidation</span>
        </div>
      </section>

      <div className="ia-metric-grid">
        <AdminMetricCard icon="users" label="Users" value={visibleCount(users.length)} description="Directory records in the current bounded view." tone="accent" />
        <AdminMetricCard icon="tenants" label="Tenants" value={visibleCount(tenants.length)} description="Security boundaries visible to this administrator." />
        <AdminMetricCard icon="groups" label="Groups" value={visibleCount(groups.length)} description="Tenant authorization groups in the current view." tone="success" />
        <AdminMetricCard icon="policies" label="Policies" value={visibleCount(policies.length)} description="Permission policies available in the tenant context." tone="warning" />
      </div>

      <section className="ia-section-block">
        <div className="ia-section-heading">
          <div><p className="ia-card-kicker">Workspaces</p><h2>Move directly to the control surface you need.</h2></div>
          <p>Each workspace is isolated by scope and uses the same typed Identity Access client.</p>
        </div>
        <div className="ia-feature-grid">
          <AdminFeatureCard href="/identity/users" icon="users" title="Identity directory" description="Create and inspect stable user identities and lifecycle state." />
          <AdminFeatureCard href="/identity/groups" icon="groups" title="Authorization groups" description="Organize tenant members into explicit authorization groups." />
          <AdminFeatureCard href="/identity/policies" icon="policies" title="Permission policies" description="Manage policy containers that feed the existing RBAC path." />
          <AdminFeatureCard href="/identity/sessions" icon="sessions" title="Session security" description="Perform deliberate, server-confirmed session revocation." eyebrow="Security operation" />
        </div>
      </section>

      <AdminSecurityBanner
        title="Presentation is never authority."
        description="Hidden navigation, disabled controls, and client-side filtering are usability features only. Every protected read and mutation is re-authorized by the backend."
      />
    </section>
  );
}
