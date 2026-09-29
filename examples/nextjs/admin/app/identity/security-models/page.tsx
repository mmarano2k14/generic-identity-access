import { IdentityAccessClientError } from "@identity-access/client";
import Link from "next/link";
import { registerSecurityModelManifestAction } from "../actions";
import { addScopeTypeAction } from "./actions";
import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminField, AdminSelectField } from "../../../components/AdminField";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminRecordContext } from "../../../components/AdminRecordContext";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";

export default async function SecurityModelsPage({ searchParams }: { readonly searchParams: Promise<{ readonly modelVersion?: string }> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const { modelVersion } = await searchParams;
  const context = request.administrationContext;
  const models = await request.client.administration.securityModels.list(context);
  const selectedVersion = modelVersion === undefined ? undefined : Number(modelVersion);
  const selected = Number.isSafeInteger(selectedVersion) && selectedVersion! > 0
    ? await request.client.administration.securityModels.get(context, selectedVersion!)
    : null;
  let scopeTypes = selected
    ? await listScopeTypesIfAuthorized(request, selected.modelVersion)
    : { available: true, records: [] as const };

  return (
    <section className="ia-page">
      <AdminPageHeader
        eyebrow="Application security"
        badge="Manifest-backed"
        title="Security models"
        description="Register and inspect immutable application-owned security manifests. The consuming project authors the JSON; Identity Access validates and stores the versioned projection used by policy administration."
        actions={(
          <AdminMutationDialog
            title="Register security manifest"
            description="Select a project-owned JSON manifest. Identity Access validates and registers the complete immutable model version; capabilities are not authored or edited in this UI."
            triggerLabel="Register manifest"
            submitLabel="Register model"
            action={registerSecurityModelManifestAction}
            triggerIcon="plus"
          >
            <AdminField
              label="Security manifest JSON"
              name="manifestFile"
              type="file"
              accept="application/json,.json"
              required
              hint="Select the project-owned security-manifest JSON file. The configured applicationKey must match this administration host."
            />
          </AdminMutationDialog>
        )}
      />

      <AdminEntityTable
        title="Registered models"
        description="Each model version pins one RBAC project, one or more namespaces, a manifest fingerprint, and a concrete capability catalog."
        entityLabel="security models"
        selectedId={selected?.modelVersion === undefined ? undefined : String(selected.modelVersion)}
        rows={models.map((model) => ({
          id: String(model.modelVersion),
          name: `Model v${model.modelVersion}`,
          status: `${model.capabilityCount} capabilities`,
          version: model.modelVersion,
          actions: <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/security-models?modelVersion=${model.modelVersion}`}>Inspect</Link>,
        }))}
      />

      {selected ? (
        <section className="ia-management-workspace">
          <AdminRecordContext
            kicker="Registered security model"
            title={`Model v${selected.modelVersion}`}
            description="This projection is immutable. Register a new project-owned manifest version to change application capabilities or RBAC context."
            identifier={selected.manifestSha256}
            status="Registered"
            version={selected.modelVersion}
            facts={[
              { label: "RBAC project", value: selected.rbacProject },
              { label: "Namespaces", value: selected.rbacNamespaces.join(", ") },
              { label: "Capabilities", value: selected.capabilities.length },
              { label: "Scope types", value: scopeTypes.available ? scopeTypes.records.length : "Not authorized" },
              { label: "Manifest schema", value: selected.schemaVersion },
            ]}
            actions={scopeTypes.available ? (
              <AdminMutationDialog
                title="Register scope type"
                description="Register one generic resource-scope type for this immutable security-model version. Root types attach directly to a tenant; child types must reference another registered type in the same model version."
                triggerLabel="Add scope type"
                submitLabel="Register scope type"
                action={addScopeTypeAction.bind(null, selected.modelVersion)}
                triggerIcon="plus"
                compact
              >
                <AdminField
                  label="Scope type key"
                  name="key"
                  required
                  maxLength={64}
                  pattern="[a-z][a-z0-9-]{0,63}"
                  placeholder="ecommerce"
                  hint="Stable machine-readable key. Use lower-case letters, digits, and hyphens only."
                />
                <AdminField label="Display name" name="displayName" required maxLength={200} placeholder="E-commerce" />
                <AdminSelectField
                  label="Parent scope type"
                  name="parentKey"
                  defaultValue=""
                  hint="Leave empty for a root type that can attach directly to the tenant. Child types inherit their required parent type from this declaration."
                >
                  <option value="">Root scope type</option>
                  {scopeTypes.records.map((scopeType) => (
                    <option key={scopeType.key} value={scopeType.key}>{scopeType.displayName} ({scopeType.key})</option>
                  ))}
                </AdminSelectField>
              </AdminMutationDialog>
            ) : undefined}
            closeHref="/identity/security-models"
          />

          {scopeTypes.available ? (
            <AdminEntityTable
              title="Registered scope types"
              description="Scope types belong to this security-model version and constrain Resource Scope creation. They do not grant permissions or define application business semantics."
              entityLabel="scope types"
              rows={scopeTypes.records.map((scopeType) => ({
                id: scopeType.key,
                name: scopeType.displayName,
                status: scopeType.parentKey ? `Child of ${scopeType.parentKey}` : "Tenant root",
                version: selected.modelVersion,
              }))}
            />
          ) : (
            <AdminSecurityBanner
              title="Scope-type catalogue access is not granted."
              description="The current administration context can inspect this security model but cannot read or register its resource-scope type catalogue."
            />
          )}

          <section className="ia-card">
            <div className="ia-card-heading"><div><p className="ia-card-kicker">Declared capabilities</p><h2>Resource / feature / action</h2><p>TRN previews combine these concrete capabilities with each registered namespace. They are descriptive previews, not authorization decisions.</p></div></div>
            <div className="ia-manage-list">
              {selected.capabilities.map((capability) => (
                <div className="ia-manage-row" key={`${capability.resource}:${capability.feature}:${capability.action}`}>
                  <div>
                    <strong>{capability.displayName}</strong>
                    <span>{capability.resource}:{capability.feature}:{capability.action}</span>
                    {selected.rbacNamespaces.map((namespaceValue) => (
                      <code key={namespaceValue}>trn:{selected.rbacProject}:{namespaceValue}:{capability.resource}:{capability.feature}:{capability.action}</code>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </section>
        </section>
      ) : null}

      <AdminSecurityBanner
        title="The administration UI registers manifests; it does not author capabilities."
        description="A consuming project owns its JSON security manifest. This UI only transports a selected file through the protected registration API, then uses the immutable database projection for policy administration."
      />
    </section>
  );
}

async function listScopeTypesIfAuthorized(request: IdentityAccessAdminRequest, modelVersion: number) {
  try {
    const records = await request.client.administration.securityModels.listScopeTypes(request.administrationContext, modelVersion);
    return { available: true as const, records };
  } catch (error) {
    if (error instanceof IdentityAccessClientError && error.httpStatus === 403) {
      return { available: false as const, records: [] as const };
    }
    throw error;
  }
}
