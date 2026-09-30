import type {
  IdentityEffectiveOrganisationProfileRecord,
  IdentityOrganisationProfileRecord,
} from "@identity-access/client";
import { AdminMutationDialog } from "../AdminMutationDialog";
import { AdminSecurityBanner } from "../AdminSecurityBanner";
import { resolveOrganisationProfileEffectiveVersionAction } from "../../app/organisations/[organizationId]/profile/actions";

export function ProfileVersionPanel({
  tenantId,
  organizationId,
  profile,
  versions,
  canRead,
  canResolve,
}: {
  readonly tenantId: string;
  readonly organizationId: string;
  readonly profile: IdentityOrganisationProfileRecord;
  readonly versions: readonly IdentityEffectiveOrganisationProfileRecord[];
  readonly canRead: boolean;
  readonly canResolve: boolean;
}) {
  if (!canRead) {
    return (
      <AdminSecurityBanner
        title="Effective versions are not readable."
        description="Immutable semantic-version history is protected independently from mutable profile administration."
      />
    );
  }

  return (
    <section className="ia-table-card">
      <div className="ia-table-heading">
        <div>
          <p className="ia-card-kicker">Deterministic history</p>
          <h2>Effective profile versions</h2>
          <p>Each row is an immutable semantic snapshot with explicit domain-version pins and a stable content hash.</p>
        </div>
        {canResolve ? (
          <AdminMutationDialog
            title="Resolve effective profile"
            description="Resolve the current template plus overrides into an immutable semantic snapshot. Existing snapshots are never rewritten."
            triggerLabel="Resolve version"
            submitLabel="Resolve version"
            action={resolveOrganisationProfileEffectiveVersionAction}
            triggerIcon="spark"
            triggerVariant="secondary"
          >
            <input type="hidden" name="tenantId" value={tenantId} />
            <input type="hidden" name="organizationId" value={organizationId} />
            <input type="hidden" name="organisationProfileId" value={profile.organisationProfileId} />
            <input type="hidden" name="expectedRowVersion" value={profile.rowVersion} />
            <p className="ia-muted">The API will return the existing version when the current semantic content is unchanged.</p>
          </AdminMutationDialog>
        ) : null}
      </div>

      {versions.length === 0 ? (
        <div className="ia-detail-empty">No effective profile version has been resolved yet.</div>
      ) : (
        <div className="ia-table-scroll">
          <table className="ia-table">
            <thead>
              <tr>
                <th>Version</th>
                <th>Template</th>
                <th>Domains</th>
                <th>Content hash</th>
                <th>Resolved</th>
              </tr>
            </thead>
            <tbody>
              {versions.map((version) => (
                <tr key={version.version}>
                  <td data-label="Version"><strong>v{version.version}</strong></td>
                  <td data-label="Template">
                    {version.templatePin
                      ? `${version.templatePin.templateKey}@${version.templatePin.templateVersion}`
                      : "None"}
                  </td>
                  <td data-label="Domains">
                    {version.domains.length === 0
                      ? "None"
                      : version.domains.map((domain) => `${domain.domainKey}@${domain.domainVersion}`).join(", ")}
                  </td>
                  <td data-label="Content hash"><code>{version.contentHash}</code></td>
                  <td data-label="Resolved">{new Date(version.resolvedAt).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
