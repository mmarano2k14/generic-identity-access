import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { createResourceScopeAction } from "../actions";

export default async function ResourceScopesPage() {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const scopes = await request.client.listResourceScopes(request.tenantContext());
  const create = (
    <AdminMutationDialog title="Create resource scope" description="Register an application-defined resource in the current tenant hierarchy." triggerLabel="Create resource scope" submitLabel="Create resource scope" action={createResourceScopeAction}>
      <AdminField label="Security model version" name="modelVersion" type="number" min={1} step={1} required />
      <AdminField label="Scope type" name="scopeType" required maxLength={128} />
      <AdminField label="External resource ID" name="externalResourceId" required maxLength={256} />
      <AdminField label="Display name" name="displayName" required maxLength={200} />
      <AdminField label="Parent resource scope ID" name="parentResourceScopeId" hint="Optional UUID for hierarchical scopes." />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );
  return (
    <section className="ia-page">
      <AdminPageHeader eyebrow="Tenant access" badge="Hierarchy" title="Resource scopes" description="Application-defined resources form explicit hierarchies for scoped policy bindings without leaking business types into the identity core." actions={create} />
      <AdminEntityTable title="Resource hierarchy" description="Browse resource scopes registered by the consuming application in the current tenant." entityLabel="scopes" rows={scopes.map((scope) => ({ id: scope.resourceScopeId, name: scope.displayName, status: scope.status === 1 ? "Active" : "Inactive", version: scope.version }))} />
    </section>
  );
}
