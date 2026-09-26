import Link from "next/link";
import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminIcon } from "../../../components/AdminIcon";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminRecordContext } from "../../../components/AdminRecordContext";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { createTenantAction, updateTenantAction } from "../actions";

export default async function TenantsPage({ searchParams }: { readonly searchParams: Promise<{ readonly tenantId?: string }> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const { tenantId } = await searchParams;
  const tenants = await request.client.administration.tenants.list(request.administrationContext, { limit: 50 });
  const selectedTenant = tenantId ? tenants.find((tenant) => tenant.tenantId === tenantId) ?? await request.client.administration.tenants.get(request.administrationContext, tenantId) : null;
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
        description="Create and edit tenant boundaries. Open Details to keep the selected lifecycle boundary visible while performing record-level maintenance."
        entityLabel="tenants"
        selectedId={selectedTenant?.tenantId}
        rows={tenants.map((tenant) => ({
          id: tenant.tenantId,
          name: tenant.displayName,
          status: tenant.status === 1 ? "Active" : "Inactive",
          version: tenant.version,
          actions: (
            <>
              <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/tenants?tenantId=${encodeURIComponent(tenant.tenantId)}`}><AdminIcon name="manage" />Details</Link>
              <AdminMutationDialog title="Edit tenant" description="Update the tenant display name or lifecycle state." triggerLabel="Edit" submitLabel="Save changes" action={updateTenantAction} triggerVariant="secondary" triggerIcon="edit" compact>
                <input type="hidden" name="tenantId" value={tenant.tenantId} />
                <input type="hidden" name="expectedVersion" value={tenant.version} />
                <AdminField label="Display name" name="displayName" defaultValue={tenant.displayName} required maxLength={200} />
                <AdminStatusField name="status" defaultValue={String(tenant.status)} />
              </AdminMutationDialog>
            </>
          ),
        }))}
      />
      {selectedTenant ? (
        <AdminRecordContext
          kicker="Selected tenant"
          title={selectedTenant.displayName}
          description="This is a logical security boundary. Its stable identity remains independent from PostgreSQL placement and routing configuration."
          identifier={selectedTenant.tenantId}
          status={selectedTenant.status === 1 ? "Active" : "Inactive"}
          version={selectedTenant.version}
          facts={[
            { label: "Boundary type", value: "Tenant" },
            { label: "Database placement", value: "Server-resolved" },
          ]}
          actions={(
            <AdminMutationDialog title="Edit tenant" description="Update the tenant display name or lifecycle state." triggerLabel="Edit tenant" submitLabel="Save changes" action={updateTenantAction} triggerVariant="secondary" triggerIcon="edit" compact>
              <input type="hidden" name="tenantId" value={selectedTenant.tenantId} />
              <input type="hidden" name="expectedVersion" value={selectedTenant.version} />
              <AdminField label="Display name" name="displayName" defaultValue={selectedTenant.displayName} required maxLength={200} />
              <AdminStatusField name="status" defaultValue={String(selectedTenant.status)} />
            </AdminMutationDialog>
          )}
          closeHref="/identity/tenants"
        />
      ) : null}
    </section>
  );
}
