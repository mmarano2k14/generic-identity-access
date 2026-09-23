import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { createTenantAction } from "../actions";

export default async function TenantsPage() {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const tenants = await request.client.listTenants(request.administrationContext, { limit: 50 });
  const create = (
    <AdminMutationDialog title="Create tenant" description="Create a security/account boundary in this identity scope." triggerLabel="Create tenant" submitLabel="Create tenant" action={createTenantAction}>
      <AdminField label="Display name" name="displayName" autoComplete="off" required maxLength={200} />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );
  return <section className="ia-page"><AdminPageHeader title="Tenants" description="Security and account boundaries within the identity scope." actions={create} /><AdminEntityTable rows={tenants.map((tenant) => ({ id: tenant.tenantId, name: tenant.displayName, status: tenant.status === 1 ? "Active" : "Inactive", version: tenant.version }))} /></section>;
}
