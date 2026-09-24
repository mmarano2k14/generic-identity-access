import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { createTenantAction, updateTenantAction } from "../actions";

export default async function TenantsPage() {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const tenants = await request.client.administration.tenants.list(request.administrationContext, { limit: 50 });
  const create = (
    <AdminMutationDialog title="Create tenant" description="Create a security and account boundary inside the current identity scope." triggerLabel="Create tenant" submitLabel="Create tenant" action={createTenantAction}>
      <AdminField label="Display name" name="displayName" autoComplete="off" required maxLength={200} />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );
  return (
    <section className="ia-page">
      <AdminPageHeader eyebrow="Directory" badge="Boundary control" title="Tenants" description="Security and account boundaries remain distinct from physical database placement and application-specific business identities." actions={create} />
      <AdminEntityTable
        title="Tenant boundaries"
        description="Create and edit tenant boundaries. Deactivation preserves references and audit history instead of silently deleting a security boundary."
        entityLabel="tenants"
        rows={tenants.map((tenant) => ({
          id: tenant.tenantId,
          name: tenant.displayName,
          status: tenant.status === 1 ? "Active" : "Inactive",
          version: tenant.version,
          actions: (
            <AdminMutationDialog title="Edit tenant" description="Update the tenant display name or lifecycle state." triggerLabel="Edit" submitLabel="Save changes" action={updateTenantAction} triggerVariant="secondary" triggerIcon="edit" compact>
              <input type="hidden" name="tenantId" value={tenant.tenantId} />
              <input type="hidden" name="expectedVersion" value={tenant.version} />
              <AdminField label="Display name" name="displayName" defaultValue={tenant.displayName} required maxLength={200} />
              <AdminStatusField name="status" defaultValue={String(tenant.status)} />
            </AdminMutationDialog>
          ),
        }))}
      />
    </section>
  );
}
