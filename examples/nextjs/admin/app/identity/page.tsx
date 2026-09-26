import { AdminFeatureCard } from "../../components/AdminFeatureCard";
import { AdminIcon } from "../../components/AdminIcon";
import { AdminMetricCard } from "../../components/AdminMetricCard";
import { AdminPageHeader } from "../../components/AdminPageHeader";
import { AdminSecurityBanner } from "../../components/AdminSecurityBanner";
import { AdminTenantContextSelector } from "../../components/AdminTenantContextSelector";
import { IdentityAccessAdminAuthorizedTenantService } from "../../server/IdentityAccessAdminAuthorizedTenantService";
import { IdentityAccessAdminRequest } from "../../server/IdentityAccessAdminRequest";
import { IdentityAccessAdminTenantAggregateLoader } from "../../server/IdentityAccessAdminTenantAggregateLoader";

type SearchParams = { readonly tenantId?: string; readonly tenantView?: string };

export default async function IdentityOverviewPage({ searchParams }: { readonly searchParams: Promise<SearchParams> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const { tenantId, tenantView } = await searchParams;
  const allTenants = tenantView === "all";
  const context = allTenants ? undefined : request.selectedTenantContext(tenantId);
  const scopeWide = request.effectiveContext.tenantVisibility === "scope-wide";
  const authorizedTenants = allTenants ? await new IdentityAccessAdminAuthorizedTenantService(request).list() : [];
  const [aggregateUsers, aggregateGroups] = allTenants ? await Promise.all([
    IdentityAccessAdminTenantAggregateLoader.load(
      authorizedTenants,
      (tenant) => request.client.administration.tenantUsers.list(tenant.context, { limit: 50 }),
    ),
    IdentityAccessAdminTenantAggregateLoader.load(
      authorizedTenants,
      (tenant) => request.client.administration.groups.list(tenant.context, { limit: 50 }),
    ),
  ]) : [[], []];

  const [userCount, tenantCount, groups] = allTenants
    ? [
      new Set(aggregateUsers.map(({ record }) => record.userId)).size,
      authorizedTenants.length,
      aggregateGroups,
    ] as const
    : await Promise.all([
      scopeWide
        ? request.client.administration.users.list(request.administrationContext, { limit: 50 }).then((records) => records.length)
        : context
          ? request.client.administration.tenantUsers.list(context, { limit: 50 }).then((records) => records.length)
          : Promise.resolve(0),
      scopeWide
        ? request.client.administration.tenants.list(request.administrationContext, { limit: 50 }).then((records) => records.length)
        : Promise.resolve(request.effectiveContext.activeTenantMemberships.length),
      context ? request.client.administration.groups.list(context, { limit: 50 }) : Promise.resolve([]),
    ]);

  const visibleCount = (count: number) => count >= 50 && !allTenants ? "50+" : String(count);
  const tenantQuery = allTenants ? "?tenantView=all" : context ? `?tenantId=${encodeURIComponent(context.tenantId)}` : "";

  return (
    <section className="ia-page ia-overview-page">
      <AdminPageHeader
        eyebrow="Security control center"
        badge={allTenants ? "Authorized aggregate" : "Server protected"}
        title="Identity administration, without the noise."
        description={allTenants ? "One administration surface now aggregates authorized tenant collections while preserving concrete tenant ownership and server-side authorization for every operation." : "Manage identity boundaries, tenant access, authorization structures, and active sessions through one controlled administration surface."}
      />

      <section className="ia-overview-hero">
        <div className="ia-overview-hero-content">
          <span className="ia-hero-icon"><AdminIcon name="shield" /></span>
          <div>
            <p className="ia-card-kicker">Trusted administration plane</p>
            <h2>Every visible action still ends at server-side authorization.</h2>
            <p>Navigation filtering improves the experience, but the .NET API, current session state, tenant visibility, resource scope, and external RBAC engine remain authoritative.</p>
          </div>
        </div>
        <div className="ia-hero-protocols" aria-label="Active security architecture">
          <span>OIDC + PKCE</span><span>Refresh rotation</span><span>Multi-key JWKS</span><span>Bearer revalidation</span>
        </div>
      </section>

      <AdminTenantContextSelector effectiveContext={request.effectiveContext} selectedTenantId={context?.tenantId} allTenantsSelected={allTenants} actionPath="/identity" />

      <div className="ia-metric-grid">
        <AdminMetricCard icon="users" label="Users" value={visibleCount(userCount)} description={allTenants ? "Unique identities linked to the authorized tenant collection view." : scopeWide ? "Directory records in the current identity-scope view." : "Users linked to the active tenant context."} tone="accent" />
        <AdminMetricCard icon="tenants" label="Tenants" value={visibleCount(tenantCount)} description={allTenants ? "Tenant targets participating in the current authorized aggregate." : scopeWide ? "Security boundaries visible to this administrator." : "Active tenant memberships available to this subject."} />
        <AdminMetricCard icon="groups" label="Groups" value={allTenants || context ? visibleCount(groups.length) : "—"} description={allTenants ? "Authorization groups returned across authorized tenants." : context ? "Tenant authorization groups in the selected context." : "Select a tenant context to load groups."} tone="success" />
        <AdminMetricCard icon="policies" label="Policies" value="Shared" description="Managed policy definitions live in the identity-scope/application catalog; tenants consume published versions through bindings." tone="warning" />
      </div>

      <section className="ia-section-block">
        <div className="ia-section-heading">
          <div><p className="ia-card-kicker">Workspaces</p><h2>Move directly to the control surface you need.</h2></div>
          <p>Each workspace is isolated by scope and uses the same typed Identity Access client.</p>
        </div>
        <div className="ia-feature-grid">
          <AdminFeatureCard href={`/identity/users${tenantQuery}`} icon="users" title="Identity directory" description={allTenants ? "Browse tenant-linked users across all authorized tenant contexts." : scopeWide ? "Create and inspect stable identity-scope users." : "Inspect users linked to the selected tenant membership boundary."} />
          <AdminFeatureCard href={`/identity/groups${tenantQuery}`} icon="groups" title="Authorization groups" description={allTenants ? "Browse groups across authorized tenants without duplicating the workspace." : "Organize tenant members into explicit authorization groups."} />
          <AdminFeatureCard href="/identity/policies" icon="policies" title="Managed policies" description="Manage shared versioned policy definitions. Tenant grants are attached independently from Groups." />
          <AdminFeatureCard href="/identity/sessions" icon="sessions" title="Session security" description="Perform deliberate, server-confirmed session revocation." eyebrow="Security operation" />
          <AdminFeatureCard href="/identity/security-audit" icon="audit" title="Security audit" description="Inspect bounded secret-safe authentication and administration evidence." eyebrow="Read-only evidence" />
        </div>
      </section>

      <AdminSecurityBanner
        title="Presentation is never authority."
        description={allTenants ? "All authorized tenants is a read-only collection context. Every record still carries one concrete tenant owner, and every protected mutation is re-authorized against that tenant." : "Tenant selectors, hidden navigation, disabled controls, and client-side state are usability features only. Every protected read and mutation is re-authorized by the backend."}
      />
    </section>
  );
}
