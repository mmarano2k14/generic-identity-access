import type { IdentityOrganisationProfileRecord } from "@identity-access/client";
import type { OrganisationProfileTemplateChoice } from "../../server/OrganisationProfileWorkspaceService";
import { AdminSelectField } from "../AdminField";
import { AdminMutationDialog } from "../AdminMutationDialog";
import { setOrganisationProfileTemplateAction } from "../../app/organisations/[organizationId]/profile/actions";

export function ProfileTemplateSelector({
  tenantId,
  organizationId,
  profile,
  templateChoices,
  canWrite,
}: {
  readonly tenantId: string;
  readonly organizationId: string;
  readonly profile: IdentityOrganisationProfileRecord;
  readonly templateChoices: readonly OrganisationProfileTemplateChoice[];
  readonly canWrite: boolean;
}) {
  const current = profile.templatePin
    ? `${profile.templatePin.templateKey}@${profile.templatePin.templateVersion}`
    : "";
  const currentSelectable = current === "" || templateChoices.some(
    (choice) => `${choice.templateKey}@${choice.templateVersion}` === current,
  );

  return (
    <section className="ia-card">
      <div className="ia-card-heading">
        <div>
          <p className="ia-card-kicker">Template pin</p>
          <h2>Profile template</h2>
          <p>Template selection is a version-pinned semantic reference, not an application architecture switch.</p>
        </div>
        {canWrite ? (
          <AdminMutationDialog
            title="Change profile template"
            description="Choose only from active template definitions and published immutable versions returned by the protected catalog."
            triggerLabel="Change template"
            submitLabel="Apply template"
            action={setOrganisationProfileTemplateAction}
            triggerIcon="edit"
            triggerVariant="secondary"
          >
            <input type="hidden" name="tenantId" value={tenantId} />
            <input type="hidden" name="organizationId" value={organizationId} />
            <input type="hidden" name="organisationProfileId" value={profile.organisationProfileId} />
            <input type="hidden" name="expectedRowVersion" value={profile.rowVersion} />
            <AdminSelectField
              label="Published template"
              name="templateSelection"
              defaultValue={current}
              hint={currentSelectable
                ? "Clearing the selection removes only the template pin. Organization-specific overrides remain explicit state."
                : "The current pin is historical or no longer selectable. Choose an active published replacement or explicitly remove the pin."}
            >
              <option value="">No template pin</option>
              {!currentSelectable && current !== "" ? (
                <option value={current}>{current} · current historical selection</option>
              ) : null}
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
        ) : null}
      </div>

      <dl className="ia-detail-grid">
        <div className="ia-detail-item">
          <dt>Template key</dt>
          <dd>{profile.templatePin?.templateKey ?? "None"}</dd>
        </div>
        <div className="ia-detail-item">
          <dt>Template version</dt>
          <dd>{profile.templatePin?.templateVersion ?? "—"}</dd>
        </div>
      </dl>
    </section>
  );
}
