import Link from "next/link";
import { registerSecurityModelManifestAction } from "../actions";
import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminField } from "../../../components/AdminField";
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
              { label: "Manifest schema", value: selected.schemaVersion },
            ]}
            closeHref="/identity/security-models"
          />

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
