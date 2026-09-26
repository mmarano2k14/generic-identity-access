import Link from "next/link";
import { AdminCreateManagedPolicyDialog } from "../../../components/AdminCreateManagedPolicyDialog";
import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminCheckboxField, AdminField, AdminSelectField, AdminStatusField } from "../../../components/AdminField";
import { AdminFailureFeedback } from "../../../components/AdminFailureFeedback";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminRecordContext } from "../../../components/AdminRecordContext";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { IdentityAccessAdminManagedPolicyReadService } from "../../../server/IdentityAccessAdminManagedPolicyReadService";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import {
  addManagedPolicyStatementAction,
  createManagedPolicyVersionAction,
  publishManagedPolicyVersionAction,
  removeManagedPolicyStatementAction,
  updateManagedPolicyAction,
} from "../actions";

type SearchParams = { readonly policyId?: string; readonly policyVersion?: string };

export default async function PoliciesPage({ searchParams }: { readonly searchParams: Promise<SearchParams> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const { policyId, policyVersion } = await searchParams;
  const {
    policies,
    selectedPolicy,
    versions,
    selectedVersion,
    statements,
    policyBuilderModels,
    failure,
  } = await new IdentityAccessAdminManagedPolicyReadService(request).load(policyId, policyVersion);

  if (failure !== undefined) {
    return (
      <section className="ia-page">
        <AdminPageHeader
          eyebrow="Application security"
          badge="Protected"
          title="Managed policies"
          description="The shared managed-policy catalog requires identity-scope administration authority. Tenant-scoped grants can consume published policies but do not own or edit their definitions."
        />
        <AdminFailureFeedback failure={failure} />
        <AdminSecurityBanner
          title="Managed policy definitions are not tenant resources."
          description="This page reports the protected read failure returned by Identity Access. It does not fall back to tenant authority, infer permission in the browser, or turn technical authorization failures into normal denials."
        />
      </section>
    );
  }

  const rows = policies.map((policy) => ({
    id: policy.policyId,
    name: policy.displayName,
    status: policy.status === 1 ? "Active" : "Inactive",
    version: policy.defaultVersion,
    actions: (
      <>
        <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/policies?policyId=${encodeURIComponent(policy.policyId)}`}>Manage</Link>
        <AdminMutationDialog
          title="Edit managed policy"
          description="Update shared policy metadata. Published version content remains immutable and tenant bindings stay separate."
          triggerLabel="Edit"
          submitLabel="Save changes"
          action={updateManagedPolicyAction}
          triggerVariant="secondary"
          triggerIcon="edit"
          compact
        >
          <input type="hidden" name="policyId" value={policy.policyId} />
          <input type="hidden" name="expectedVersion" value={policy.version} />
          <AdminField label="Policy key" name="policyKey" defaultValue={policy.policyKey} required maxLength={128} pattern="[a-z][a-z0-9-]{0,127}" />
          <AdminField label="Display name" name="displayName" defaultValue={policy.displayName} required maxLength={200} />
          <AdminStatusField name="status" defaultValue={String(policy.status)} />
        </AdminMutationDialog>
      </>
    ),
  }));

  const nextPolicyVersion = versions.length === 0
    ? 1
    : Math.max(...versions.map((version) => version.policyVersion)) + 1;
  const selectedModel = selectedVersion
    ? policyBuilderModels.find((model) => model.modelVersion === selectedVersion.modelVersion)
    : undefined;
  const selectedPublished = selectedVersion?.publishedAt !== undefined;
  const selectedIsDefault = selectedPolicy !== null
    && selectedVersion !== null
    && selectedPolicy.defaultVersion === selectedVersion.policyVersion;

  return (
    <section className="ia-page">
      <AdminPageHeader
        eyebrow="Application security"
        badge="Shared catalog"
        title="Managed policies"
        description="Define reusable, versioned policy templates once for the application. Tenants attach published versions through tenant-scoped group bindings without cloning policy definitions."
        actions={<AdminCreateManagedPolicyDialog />}
      />

      <AdminEntityTable
        title="Managed policy catalog"
        description="Policies are owned by the identity scope and application, never by a tenant. The Version column shows the currently selected published default when one exists."
        entityLabel="managed policies"
        selectedId={selectedPolicy?.policyId}
        rows={rows}
      />

      {selectedPolicy ? (
        <section className="ia-management-workspace">
          <AdminRecordContext
            kicker="Selected managed policy"
            title={selectedPolicy.displayName}
            description="Maintain shared metadata and immutable published versions. Tenant grants are managed separately from Groups."
            identifier={selectedPolicy.policyId}
            status={selectedPolicy.status === 1 ? "Active" : "Inactive"}
            version={selectedPolicy.version}
            facts={[
              { label: "Policy key", value: selectedPolicy.policyKey, mono: true },
              { label: "Default version", value: selectedPolicy.defaultVersion === undefined ? "None" : `v${selectedPolicy.defaultVersion}` },
              { label: "Versions", value: versions.length },
              { label: "Tenant ownership", value: "None" },
            ]}
            actions={(
              <div className="ia-action-row">
                <AdminMutationDialog
                  title="Edit managed policy"
                  description="Update shared policy metadata. Version definitions and tenant bindings are independent."
                  triggerLabel="Edit policy"
                  submitLabel="Save changes"
                  action={updateManagedPolicyAction}
                  triggerVariant="secondary"
                  triggerIcon="edit"
                  compact
                >
                  <input type="hidden" name="policyId" value={selectedPolicy.policyId} />
                  <input type="hidden" name="expectedVersion" value={selectedPolicy.version} />
                  <AdminField label="Policy key" name="policyKey" defaultValue={selectedPolicy.policyKey} required maxLength={128} pattern="[a-z][a-z0-9-]{0,127}" />
                  <AdminField label="Display name" name="displayName" defaultValue={selectedPolicy.displayName} required maxLength={200} />
                  <AdminStatusField name="status" defaultValue={String(selectedPolicy.status)} />
                </AdminMutationDialog>

                {policyBuilderModels.length > 0 ? (
                  <AdminMutationDialog
                    title="Create policy version"
                    description="Create a new draft pinned to one immutable registered application security-model version."
                    triggerLabel="New draft version"
                    submitLabel="Create draft"
                    action={createManagedPolicyVersionAction}
                    compact
                  >
                    <input type="hidden" name="policyId" value={selectedPolicy.policyId} />
                    <AdminField label="Policy version" name="policyVersion" type="number" min={1} step={1} defaultValue={nextPolicyVersion} required />
                    <AdminSelectField label="Security model version" name="modelVersion" required hint="Draft statements can only use capabilities declared by the selected immutable security-model version.">
                      <option value="">Select a registered model</option>
                      {policyBuilderModels.map((model) => (
                        <option key={model.modelVersion} value={model.modelVersion}>Model v{model.modelVersion} · {model.rbacProject}</option>
                      ))}
                    </AdminSelectField>
                  </AdminMutationDialog>
                ) : (
                  <Link className="ia-button ia-button-secondary ia-button-compact" href="/identity/security-models">Register security model</Link>
                )}
              </div>
            )}
            closeHref="/identity/policies"
          />

          <section className="ia-card">
            <div className="ia-card-heading">
              <div>
                <p className="ia-card-kicker">Version lifecycle</p>
                <h2>Managed policy versions</h2>
                <p>Drafts are editable. Publishing freezes their security-model pin and statements. Bindings always reference one concrete published version.</p>
              </div>
            </div>
            {versions.length === 0 ? <p className="ia-empty-inline">This managed policy has no versions yet. Create the first draft version.</p> : (
              <div className="ia-manage-list">
                {versions.map((version) => {
                  const isPublished = version.publishedAt !== undefined;
                  const isDefault = selectedPolicy.defaultVersion === version.policyVersion;
                  return (
                    <div className="ia-manage-row" key={version.policyVersion}>
                      <div>
                        <strong>Version {version.policyVersion}</strong>
                        <span>Security model v{version.modelVersion} · {isPublished ? "Published" : "Draft"}{isDefault ? " · Default" : ""}</span>
                        <code>{isPublished ? version.publishedAt : "Unpublished draft"}</code>
                      </div>
                      <div className="ia-row-actions">
                        <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/policies?policyId=${encodeURIComponent(selectedPolicy.policyId)}&policyVersion=${version.policyVersion}`}>Manage</Link>
                        {!isPublished ? (
                          <AdminMutationDialog
                            title={`Publish version ${version.policyVersion}`}
                            description="Publishing freezes this version and its statements. Existing tenant bindings remain pinned to their current versions."
                            triggerLabel="Publish"
                            submitLabel="Publish version"
                            action={publishManagedPolicyVersionAction}
                            compact
                          >
                            <input type="hidden" name="policyId" value={selectedPolicy.policyId} />
                            <input type="hidden" name="policyVersion" value={version.policyVersion} />
                            <AdminCheckboxField label="Make this the default version" name="makeDefault" hint="New group bindings may use the published default version when no explicit version is requested." />
                          </AdminMutationDialog>
                        ) : !isDefault ? (
                          <AdminMutationDialog
                            title={`Set version ${version.policyVersion} as default`}
                            description="This changes the default selected for future bindings. Existing bindings remain pinned to their stored version."
                            triggerLabel="Set default"
                            submitLabel="Set default"
                            action={publishManagedPolicyVersionAction}
                            triggerVariant="secondary"
                            compact
                          >
                            <input type="hidden" name="policyId" value={selectedPolicy.policyId} />
                            <input type="hidden" name="policyVersion" value={version.policyVersion} />
                            <input type="hidden" name="makeDefault" value="true" />
                          </AdminMutationDialog>
                        ) : null}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </section>

          {selectedVersion ? (
            <section className="ia-card">
              <div className="ia-card-heading">
                <div>
                  <p className="ia-card-kicker">Selected version</p>
                  <h2>Version {selectedVersion.policyVersion}</h2>
                  <p>Security model v{selectedVersion.modelVersion} · {selectedPublished ? "Published and immutable" : "Draft and editable"}{selectedIsDefault ? " · Default" : ""}</p>
                </div>
                {!selectedPublished && selectedModel ? (
                  <AdminMutationDialog
                    title="Add managed policy statement"
                    description={`Choose one capability declared by security model v${selectedVersion.modelVersion}. The server validates the capability against the version's immutable model pin.`}
                    triggerLabel="Add statement"
                    submitLabel="Add statement"
                    action={addManagedPolicyStatementAction}
                    compact
                  >
                    <input type="hidden" name="policyId" value={selectedPolicy.policyId} />
                    <input type="hidden" name="policyVersion" value={selectedVersion.policyVersion} />
                    <AdminSelectField label="Capability" name="capability" required hint="Project and namespace remain registered model context; managed policy statements store resource, feature, and action against the pinned model.">
                      <option value="">Select a registered capability</option>
                      {selectedModel.capabilities.map((capability) => (
                        <option key={capability.value} value={capability.value}>{capability.label}</option>
                      ))}
                    </AdminSelectField>
                  </AdminMutationDialog>
                ) : null}
              </div>

              {selectedPublished ? (
                <AdminSecurityBanner title="Published versions are immutable." description="Create a new draft version to change capability statements. Tenant bindings remain pinned to the concrete version they reference." />
              ) : selectedModel === undefined ? (
                <AdminSecurityBanner title="Pinned security model is unavailable." description="The selected draft references a security-model version that is not currently returned by the registered application catalog. Do not author free-text capability coordinates." />
              ) : null}

              {statements.length === 0 ? <p className="ia-empty-inline">This version has no statements.</p> : (
                <div className="ia-manage-list">
                  {statements.map((statement) => (
                    <div className="ia-manage-row" key={statement.statementId}>
                      <div>
                        <strong>{statement.resource}:{statement.feature}:{statement.action}</strong>
                        <span>Policy v{statement.policyVersion} · Security model v{statement.modelVersion}</span>
                        <code>{statement.statementId}</code>
                      </div>
                      {!selectedPublished ? (
                        <AdminMutationDialog
                          title="Remove managed policy statement"
                          description="Remove this statement from the draft. Type REMOVE to confirm."
                          triggerLabel="Remove"
                          submitLabel="Remove statement"
                          action={removeManagedPolicyStatementAction}
                          dangerous
                          compact
                        >
                          <input type="hidden" name="policyId" value={selectedPolicy.policyId} />
                          <input type="hidden" name="policyVersion" value={selectedVersion.policyVersion} />
                          <input type="hidden" name="statementId" value={statement.statementId} />
                          <AdminField label="Confirmation" name="confirmation" required placeholder="REMOVE" autoComplete="off" />
                        </AdminMutationDialog>
                      ) : null}
                    </div>
                  ))}
                </div>
              )}
            </section>
          ) : null}
        </section>
      ) : null}

      <AdminSecurityBanner
        title="Policies are shared definitions; grants remain tenant-scoped."
        description="The managed policy catalog contains reusable application policy definitions. Groups, memberships, resource scopes, and managed policy bindings retain concrete tenant boundaries, and the external RBAC engine remains the final authorization authority."
      />
    </section>
  );
}
