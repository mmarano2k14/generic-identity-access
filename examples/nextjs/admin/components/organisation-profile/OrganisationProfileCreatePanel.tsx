import type { OrganisationProfileTemplateChoice } from "../../server/OrganisationProfileWorkspaceService";
import { AdminSelectField } from "../AdminField";
import { AdminMutationDialog } from "../AdminMutationDialog";
import { AdminSecurityBanner } from "../AdminSecurityBanner";
import { createOrganisationProfileAction } from "../../app/organisations/[organizationId]/profile/actions";

export function OrganisationProfileCreatePanel({
  tenantId,
  organizationId,
  organizationName,
  templateChoices,
  canCreate,
}: {
  readonly tenantId: string;
  readonly organizationId: string;
  readonly organizationName: string;
  readonly templateChoices: readonly OrganisationProfileTemplateChoice[];
  readonly canCreate: boolean;
}) {
  if (!canCreate) {
    return (
      <AdminSecurityBanner
        title="No OrganisationProfile is configured for this Organization."
        description="The current access context can inspect the Organization, but it cannot create its semantic profile."
      />
    );
  }

  return (
    <section className="ia-card">
      <div className="ia-card-heading">
        <div>
          <p className="ia-card-kicker">Semantic configuration</p>
          <h2>Create OrganisationProfile</h2>
          <p>
            Attach one application-owned semantic profile to {organizationName}. Organization identity remains unchanged.
          </p>
        </div>
        <AdminMutationDialog
          title={`Create profile for ${organizationName}`}
          description="Create the semantic profile only. The Organization record, hierarchy, memberships, and ResourceScope links are not modified."
          triggerLabel="Create profile"
          submitLabel="Create profile"
          action={createOrganisationProfileAction}
          triggerIcon="plus"
        >
          <input type="hidden" name="tenantId" value={tenantId} />
          <input type="hidden" name="organizationId" value={organizationId} />
          <AdminSelectField
            label="Published template"
            name="templateSelection"
            defaultValue=""
            hint="Optional. Only active templates with published immutable versions are selectable."
          >
            <option value="">No template pin</option>
            {templateChoices.map((choice) => (
              <option
                key={`${choice.templateKey}@${choice.templateVersion}`}
                value={`${choice.templateKey}@${choice.templateVersion}`}
              >
                {choice.displayName} · {choice.templateKey}@{choice.templateVersion}
              </option>
            ))}
          </AdminSelectField>
        </AdminMutationDialog>
      </div>
      <p>
        Profile type remains composition. Creating this record does not create a second Organization identity and does not grant authorization.
      </p>
    </section>
  );
}
