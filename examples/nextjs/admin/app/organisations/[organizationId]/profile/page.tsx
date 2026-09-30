import { AdminPageHeader } from "../../../../components/AdminPageHeader";
import { AdminSecurityBanner } from "../../../../components/AdminSecurityBanner";
import { OrganisationProfileCreatePanel } from "../../../../components/organisation-profile/OrganisationProfileCreatePanel";
import { OrganisationProfilePanel } from "../../../../components/organisation-profile/OrganisationProfilePanel";
import { IdentityAccessAdminRequest } from "../../../../server/IdentityAccessAdminRequest";
import { OrganisationProfileWorkspaceService } from "../../../../server/OrganisationProfileWorkspaceService";

type RouteParams = { readonly organizationId: string };
type SearchParams = { readonly tenantId?: string };

export default async function OrganisationProfilePage({
  params,
  searchParams,
}: {
  readonly params: Promise<RouteParams>;
  readonly searchParams: Promise<SearchParams>;
}) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const { organizationId } = await params;
  const { tenantId } = await searchParams;
  const context = request.selectedTenantContext(tenantId);

  if (context === undefined) {
    return (
      <section className="ia-page">
        <AdminPageHeader
          eyebrow="Organization context"
          badge="Tenant required"
          title="OrganisationProfile"
          description="This workspace requires one server-authorized tenant context. The consuming application should route here with the Organization's tenant; there is no project selector."
        />
        <AdminSecurityBanner
          title="No tenant context is selected."
          description="A tenant may be selected only from the trusted effective administration context. The workspace will not scan databases or guess an Organization owner."
        />
      </section>
    );
  }

  const workspace = await new OrganisationProfileWorkspaceService(request).load(
    context.tenantId,
    organizationId,
  );

  if (!workspace.permissions.canReadOrganization || workspace.organization === null) {
    return (
      <section className="ia-page">
        <AdminPageHeader
          eyebrow="Organization context"
          badge="Protected"
          title="OrganisationProfile"
          description="Organization identity is read from the existing Organization Directory and is never recreated by this workspace."
        />
        <AdminSecurityBanner
          title="Organization is not readable in this tenant context."
          description="The semantic profile UI does not infer or duplicate Organization identity when the trusted directory reference cannot be read."
        />
      </section>
    );
  }

  const organization = workspace.organization;

  return (
    <section className="ia-page">
      <AdminPageHeader
        eyebrow="Organization workspace"
        badge={organization.status === 1 ? "Organization active" : "Organization inactive"}
        title={organization.displayName}
        description={`Semantic capability composition for ${organization.organizationKey}. Organization identity, hierarchy, membership, and ResourceScope linkage remain managed by their existing authorities.`}
      />

      {!workspace.permissions.canReadProfile ? (
        <AdminSecurityBanner
          title="OrganisationProfile is not readable."
          description="Organization identity is visible, but semantic profile state is protected by a separate administration capability."
        />
      ) : workspace.profile === null ? (
        <OrganisationProfileCreatePanel
          tenantId={workspace.tenantId}
          organizationId={organization.organizationId}
          organizationName={organization.displayName}
          templateChoices={workspace.templateChoices}
          canCreate={workspace.permissions.canManageProfile}
        />
      ) : (
        <OrganisationProfilePanel workspace={workspace} />
      )}

      <AdminSecurityBanner
        title="Organization identity is not editable here."
        description="This workspace references the existing Organization and configures only its OrganisationProfile. It never creates OrganizationMemberships, grants permissions, edits ResourceScopes, or stores provider credentials."
      />
    </section>
  );
}
