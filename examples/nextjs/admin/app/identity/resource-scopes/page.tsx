import Link from "next/link";
import { AdminCreateResourceScopeDialog } from "../../../components/AdminCreateResourceScopeDialog";
import { AdminEntityAutocomplete } from "../../../components/AdminEntityAutocomplete";
import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminIcon } from "../../../components/AdminIcon";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminRecordContext } from "../../../components/AdminRecordContext";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { AdminTenantContextSelector } from "../../../components/AdminTenantContextSelector";
import { IdentityAccessAdminAuthorizedTenantService } from "../../../server/IdentityAccessAdminAuthorizedTenantService";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { IdentityAccessAdminTenantAggregateLoader } from "../../../server/IdentityAccessAdminTenantAggregateLoader";
import { updateResourceScopeAction } from "../actions";

type SearchParams = { readonly tenantId?: string; readonly tenantView?: string; readonly resourceScopeId?: string };

export default async function ResourceScopesPage({ searchParams }: { readonly searchParams: Promise<SearchParams> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const { tenantId, tenantView, resourceScopeId } = await searchParams;
  const allTenants = tenantView === "all";
  const context = allTenants ? undefined : request.selectedTenantContext(tenantId);
  const create = <AdminCreateResourceScopeDialog effectiveContext={request.effectiveContext} selectedTenantId={context?.tenantId} />;

  if (context === undefined && !allTenants) {
    return (
      <section className="ia-page">
        <AdminPageHeader eyebrow="Tenant access" badge="Hierarchy" title="Resource scopes" description="Select one authorized tenant or use All authorized tenants to browse resource-scope hierarchy records. Creation always resolves one concrete owning tenant." actions={create} />
        <AdminTenantContextSelector effectiveContext={request.effectiveContext} selectedTenantId={tenantId} actionPath="/identity/resource-scopes" />
        <AdminSecurityBanner title="Tenant selection is not authorization." description="Resource-scope reads and mutations remain protected by the backend for the selected tenant target." />
      </section>
    );
  }

  const aggregateScopes = allTenants
    ? await IdentityAccessAdminTenantAggregateLoader.load(
      await new IdentityAccessAdminAuthorizedTenantService(request).list(),
      (tenant) => request.client.administration.resourceScopes.list(tenant.context),
    )
    : [];
  const scopes = context ? await request.client.administration.resourceScopes.list(context) : [];
  const selectedScope = context && resourceScopeId
    ? scopes.find((scope) => scope.resourceScopeId === resourceScopeId) ?? await request.client.administration.resourceScopes.get(context, resourceScopeId)
    : null;
  const parent = selectedScope && context && selectedScope.parentResourceScopeId
    ? scopes.find((scope) => scope.resourceScopeId === selectedScope.parentResourceScopeId)
      ?? await request.client.administration.resourceScopes.get(context, selectedScope.parentResourceScopeId)
    : null;
  const rows = allTenants
    ? aggregateScopes.map(({ tenant, record: scope }) => ({
      key: `${tenant.tenantId}:${scope.resourceScopeId}`,
      id: scope.resourceScopeId,
      name: scope.displayName,
      status: scope.status === 1 ? "Active" : "Inactive",
      version: scope.version,
      tenant: { tenantId: tenant.tenantId, displayName: tenant.displayName },
      actions: (
        <>
          <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/resource-scopes?tenantId=${encodeURIComponent(tenant.tenantId)}&resourceScopeId=${encodeURIComponent(scope.resourceScopeId)}`}><AdminIcon name="manage" />Details</Link>
          <AdminMutationDialog title="Edit resource scope" description="Update hierarchy metadata in its concrete tenant while preserving stable resource-scope identity." triggerLabel="Edit" submitLabel="Save changes" action={updateResourceScopeAction} triggerVariant="secondary" triggerIcon="edit" compact>
            <input type="hidden" name="tenantId" value={tenant.tenantId} />
            <input type="hidden" name="resourceScopeId" value={scope.resourceScopeId} />
            <input type="hidden" name="expectedVersion" value={scope.version} />
            <AdminField label="Security model version" name="modelVersion" type="number" min={1} step={1} defaultValue={scope.modelVersion} required />
            <AdminField label="Scope type" name="scopeType" defaultValue={scope.scopeType} required maxLength={128} />
            <AdminField label="External resource ID" name="externalResourceId" defaultValue={scope.externalResourceId} required maxLength={256} />
            <AdminField label="Display name" name="displayName" defaultValue={scope.displayName} required maxLength={200} />
            <AdminEntityAutocomplete label="Parent resource scope" name="parentResourceScopeId" kind="resource-scope" tenantId={tenant.tenantId} excludeIds={[scope.resourceScopeId]} defaultValue={scope.parentResourceScopeId ?? ""} emptyLabel="Root scope" hint="Optional. Type at least 3 characters of the parent scope display name/external ID." />
            <AdminStatusField name="status" defaultValue={String(scope.status)} />
          </AdminMutationDialog>
        </>
      ),
    }))
    : scopes.map((scope) => ({
      id: scope.resourceScopeId,
      name: scope.displayName,
      status: scope.status === 1 ? "Active" : "Inactive",
      version: scope.version,
      actions: (
        <>
          <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/resource-scopes?tenantId=${encodeURIComponent(context!.tenantId)}&resourceScopeId=${encodeURIComponent(scope.resourceScopeId)}`}><AdminIcon name="manage" />Details</Link>
          <AdminMutationDialog title="Edit resource scope" description="Update hierarchy metadata while preserving stable resource-scope identity." triggerLabel="Edit" submitLabel="Save changes" action={updateResourceScopeAction} triggerVariant="secondary" triggerIcon="edit" compact>
            <input type="hidden" name="tenantId" value={context!.tenantId} />
            <input type="hidden" name="resourceScopeId" value={scope.resourceScopeId} />
            <input type="hidden" name="expectedVersion" value={scope.version} />
            <AdminField label="Security model version" name="modelVersion" type="number" min={1} step={1} defaultValue={scope.modelVersion} required />
            <AdminField label="Scope type" name="scopeType" defaultValue={scope.scopeType} required maxLength={128} />
            <AdminField label="External resource ID" name="externalResourceId" defaultValue={scope.externalResourceId} required maxLength={256} />
            <AdminField label="Display name" name="displayName" defaultValue={scope.displayName} required maxLength={200} />
            <AdminEntityAutocomplete label="Parent resource scope" name="parentResourceScopeId" kind="resource-scope" tenantId={context!.tenantId} excludeIds={[scope.resourceScopeId]} defaultValue={scope.parentResourceScopeId ?? ""} emptyLabel="Root scope" hint="Optional. Type at least 3 characters of the parent scope display name/external ID." />
            <AdminStatusField name="status" defaultValue={String(scope.status)} />
          </AdminMutationDialog>
        </>
      ),
    }));

  return (
    <section className="ia-page">
      <AdminPageHeader eyebrow="Tenant access" badge={allTenants ? "Authorized aggregate" : "Hierarchy"} title="Resource scopes" description={allTenants ? "Browse application-defined resource scopes across every tenant where this protected read is authorized. Each row keeps its concrete tenant boundary." : "Application-defined resources form explicit hierarchies for scoped policy bindings without leaking business types into the identity core."} actions={create} />
      <AdminTenantContextSelector effectiveContext={request.effectiveContext} selectedTenantId={context?.tenantId} allTenantsSelected={allTenants} actionPath="/identity/resource-scopes" />
      <AdminEntityTable
        title={allTenants ? "Resource hierarchy across authorized tenants" : "Resource hierarchy"}
        description={allTenants ? "The aggregate collection does not merge hierarchy ownership. Create chooses one concrete tenant, row edits retain their row tenant, and Details enters that concrete tenant for hierarchy maintenance." : "Create and edit registered resource scopes. Open Details to inspect hierarchy metadata without turning business-resource semantics into identity-core types."}
        entityLabel="scopes"
        selectedId={selectedScope?.resourceScopeId}
        rows={rows}
      />
      {selectedScope && context ? (
        <AdminRecordContext
          kicker="Selected resource scope"
          title={selectedScope.displayName}
          description="Hierarchy and external-resource metadata remain explicit so administrators can understand what a scoped policy binding refers to."
          identifier={selectedScope.resourceScopeId}
          status={selectedScope.status === 1 ? "Active" : "Inactive"}
          version={selectedScope.version}
          facts={[
            { label: "Tenant", value: context.tenantId, mono: true },
            { label: "Scope type", value: selectedScope.scopeType },
            { label: "External resource", value: selectedScope.externalResourceId, mono: true },
            { label: "Security model", value: `v${selectedScope.modelVersion}` },
            { label: "Parent", value: parent ? parent.displayName : selectedScope.parentResourceScopeId ?? "Root" },
          ]}
          actions={(
            <AdminMutationDialog title="Edit resource scope" description="Update hierarchy metadata while preserving stable resource-scope identity." triggerLabel="Edit scope" submitLabel="Save changes" action={updateResourceScopeAction} triggerVariant="secondary" triggerIcon="edit" compact>
              <input type="hidden" name="tenantId" value={context.tenantId} />
              <input type="hidden" name="resourceScopeId" value={selectedScope.resourceScopeId} />
              <input type="hidden" name="expectedVersion" value={selectedScope.version} />
              <AdminField label="Security model version" name="modelVersion" type="number" min={1} step={1} defaultValue={selectedScope.modelVersion} required />
              <AdminField label="Scope type" name="scopeType" defaultValue={selectedScope.scopeType} required maxLength={128} />
              <AdminField label="External resource ID" name="externalResourceId" defaultValue={selectedScope.externalResourceId} required maxLength={256} />
              <AdminField label="Display name" name="displayName" defaultValue={selectedScope.displayName} required maxLength={200} />
              <AdminEntityAutocomplete label="Parent resource scope" name="parentResourceScopeId" kind="resource-scope" tenantId={context.tenantId} excludeIds={[selectedScope.resourceScopeId]} defaultValue={selectedScope.parentResourceScopeId ?? ""} emptyLabel="Root scope" hint="Optional. Type at least 3 characters of the parent scope display name/external ID." />
              <AdminStatusField name="status" defaultValue={String(selectedScope.status)} />
            </AdminMutationDialog>
          )}
          closeHref={`/identity/resource-scopes?tenantId=${encodeURIComponent(context.tenantId)}`}
        />
      ) : allTenants ? (
        <AdminSecurityBanner title="All authorized tenants is a collection view." description="Open Details to enter a concrete tenant before changing resource hierarchy metadata." />
      ) : null}
    </section>
  );
}
