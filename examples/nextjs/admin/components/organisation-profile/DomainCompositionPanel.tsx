import type {
  IdentityOrganisationProfileDomainOverrideRecord,
  IdentityOrganisationProfileRecord,
} from "@identity-access/client";
import type { OrganisationProfileDomainChoice } from "../../server/OrganisationProfileWorkspaceService";
import { AdminSelectField } from "../AdminField";
import { AdminMutationDialog } from "../AdminMutationDialog";
import { AdminSecurityBanner } from "../AdminSecurityBanner";
import { replaceOrganisationProfileDomainOverridesAction } from "../../app/organisations/[organizationId]/profile/actions";

function currentValue(
  override: IdentityOrganisationProfileDomainOverrideRecord | undefined,
): string {
  if (override === undefined) return "inherit";
  if (override.operation === 2) return "disable";
  return override.domainVersion === undefined
    ? "inherit"
    : `enable:${override.domainVersion}`;
}

export function DomainCompositionPanel({
  tenantId,
  organizationId,
  profile,
  overrides,
  domainChoices,
  canRead,
  canWrite,
}: {
  readonly tenantId: string;
  readonly organizationId: string;
  readonly profile: IdentityOrganisationProfileRecord;
  readonly overrides: readonly IdentityOrganisationProfileDomainOverrideRecord[];
  readonly domainChoices: readonly OrganisationProfileDomainChoice[];
  readonly canRead: boolean;
  readonly canWrite: boolean;
}) {
  if (!canRead) {
    return (
      <AdminSecurityBanner
        title="Domain overrides are not readable."
        description="The profile remains visible, but this access context cannot inspect Organization-specific composition overrides."
      />
    );
  }

  const overrideByDomain = new Map(
    overrides.map((override) => [override.domainKey, override]),
  );

  const allDomainKeys = Array.from(
    new Set([
      ...domainChoices.map((choice) => choice.domainKey),
      ...overrides.map((override) => override.domainKey),
    ]),
  ).sort((left, right) => left.localeCompare(right));

  const choiceByDomain = new Map(
    domainChoices.map((choice) => [choice.domainKey, choice]),
  );

  const historicalEnableOverride = overrides.find((override) => {
    if (override.operation !== 1 || override.domainVersion === undefined) return false;
    const choice = choiceByDomain.get(override.domainKey);
    return choice === undefined || !choice.publishedVersions.includes(override.domainVersion);
  });

  const editorEnabled = canWrite && historicalEnableOverride === undefined;

  return (
    <section className="ia-card">
      <div className="ia-card-heading">
        <div>
          <p className="ia-card-kicker">Composition</p>
          <h2>Domain overrides</h2>
          <p>
            Overrides are layered over the pinned template. Effective composition remains derived and versioned separately.
          </p>
        </div>
        {editorEnabled ? (
          <AdminMutationDialog
            title="Replace domain overrides"
            description="The complete override set is replaced atomically. Enable choices come only from published domain references observed through immutable profile-template versions."
            triggerLabel="Edit overrides"
            submitLabel="Replace overrides"
            action={replaceOrganisationProfileDomainOverridesAction}
            triggerIcon="edit"
            triggerVariant="secondary"
          >
            <input type="hidden" name="tenantId" value={tenantId} />
            <input type="hidden" name="organizationId" value={organizationId} />
            <input type="hidden" name="organisationProfileId" value={profile.organisationProfileId} />
            <input type="hidden" name="expectedRowVersion" value={profile.rowVersion} />

            {allDomainKeys.length === 0 ? (
              <p className="ia-muted">No trusted domain references are available from the current template catalog.</p>
            ) : (
              allDomainKeys.map((domainKey) => {
                const choice = choiceByDomain.get(domainKey);
                const current = overrideByDomain.get(domainKey);
                const currentSelection = currentValue(current);
                const publishedVersions = choice?.publishedVersions ?? [];

                return (
                  <AdminSelectField
                    key={domainKey}
                    label={domainKey}
                    name={`domain:${domainKey}`}
                    defaultValue={currentSelection}
                    hint="Inherit template state, explicitly disable, or pin one published domain version."
                  >
                    <option value="inherit">Inherit template</option>
                    <option value="disable">Disable</option>
                    {publishedVersions.map((version) => (
                      <option key={version} value={`enable:${version}`}>
                        Enable @{version}
                      </option>
                    ))}
                  </AdminSelectField>
                );
              })
            )}
          </AdminMutationDialog>
        ) : null}
      </div>

      {canWrite && historicalEnableOverride !== undefined ? (
        <AdminSecurityBanner
          title="Override editor is temporarily read-only."
          description={`The current ${historicalEnableOverride.domainKey}@${historicalEnableOverride.domainVersion ?? "?"} enable override is no longer a published selectable reference. The complete-set editor will not silently remove or repin historical state.`}
        />
      ) : null}

      {overrides.length === 0 ? (
        <p>No Organization-specific overrides are active. Effective domains come from the pinned template only.</p>
      ) : (
        <div className="ia-table-scroll">
          <table className="ia-table">
            <thead>
              <tr>
                <th>Domain</th>
                <th>Operation</th>
                <th>Version</th>
              </tr>
            </thead>
            <tbody>
              {[...overrides]
                .sort((left, right) => left.domainKey.localeCompare(right.domainKey))
                .map((override) => (
                  <tr key={override.domainKey}>
                    <td data-label="Domain"><strong>{override.domainKey}</strong></td>
                    <td data-label="Operation">{override.operation === 1 ? "Enable" : "Disable"}</td>
                    <td data-label="Version">{override.domainVersion ?? "—"}</td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
