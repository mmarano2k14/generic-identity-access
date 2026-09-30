import type { OrganisationProfileWorkspace } from "../../server/OrganisationProfileWorkspaceService";
import { AdminRecordContext } from "../AdminRecordContext";
import { DomainCompositionPanel } from "./DomainCompositionPanel";
import { ProfileLifecycleActions } from "./ProfileLifecycleActions";
import { ProfileTemplateSelector } from "./ProfileTemplateSelector";
import { ProfileVersionPanel } from "./ProfileVersionPanel";

/** Focused composition root; individual profile concerns stay in separate UI components. */
export function OrganisationProfilePanel({
  workspace,
}: {
  readonly workspace: OrganisationProfileWorkspace;
}) {
  const { organization, profile, permissions } = workspace;
  if (organization === null || profile === null) return null;

  return (
    <div className="ia-management-workspace">
      <AdminRecordContext
        kicker="OrganisationProfile"
        title={organization.displayName}
        description="Application-owned semantic capability composition attached to an existing Organization. Organization identity and authorization remain external authorities."
        identifier={profile.organisationProfileId}
        status={profile.status === 1 ? "Active" : "Disabled"}
        version={profile.rowVersion}
        facts={[
          { label: "Organization", value: organization.organizationId },
          { label: "Organization type", value: organization.organizationType },
          { label: "Template", value: profile.templatePin ? `${profile.templatePin.templateKey}@${profile.templatePin.templateVersion}` : "None" },
          { label: "Overrides", value: String(workspace.overrides.length) },
        ]}
        actions={(
          <ProfileLifecycleActions
            tenantId={workspace.tenantId}
            organizationId={organization.organizationId}
            profile={profile}
            canWrite={permissions.canManageProfile}
          />
        )}
      />

      <div className="ia-card-grid">
        <ProfileTemplateSelector
          tenantId={workspace.tenantId}
          organizationId={organization.organizationId}
          profile={profile}
          templateChoices={workspace.templateChoices}
          canWrite={permissions.canManageProfile && permissions.canReadTemplates}
        />

        <DomainCompositionPanel
          tenantId={workspace.tenantId}
          organizationId={organization.organizationId}
          profile={profile}
          overrides={workspace.overrides}
          domainChoices={workspace.domainChoices}
          canRead={permissions.canReadDomainOverrides}
          canWrite={permissions.canManageDomainOverrides && permissions.canReadTemplates}
        />
      </div>

      <ProfileVersionPanel
        tenantId={workspace.tenantId}
        organizationId={organization.organizationId}
        profile={profile}
        versions={workspace.effectiveVersions}
        canRead={permissions.canReadEffectiveVersions}
        canResolve={permissions.canResolveEffectiveVersions}
      />
    </div>
  );
}
