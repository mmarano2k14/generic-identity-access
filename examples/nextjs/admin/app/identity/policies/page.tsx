import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { createPolicyAction } from "../actions";

export default async function PoliciesPage() {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const policies = await request.client.listPolicies(request.tenantContext(), { limit: 50 });
  const create = (
    <AdminMutationDialog title="Create policy" description="Create a tenant-scoped permission policy. Statements and bindings remain explicit follow-up operations." triggerLabel="Create policy" submitLabel="Create policy" action={createPolicyAction}>
      <AdminField label="Display name" name="displayName" autoComplete="off" required maxLength={200} />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );
  return <section className="ia-page"><AdminPageHeader title="Policies" description="Permission policies and capability statements." actions={create} /><AdminEntityTable rows={policies.map((policy) => ({ id: policy.policyId, name: policy.displayName, status: policy.status === 1 ? "Active" : "Inactive", version: policy.version }))} /></section>;
}
