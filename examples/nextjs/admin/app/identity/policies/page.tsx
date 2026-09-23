import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { createPolicyAction } from "../actions";

export default async function PoliciesPage() {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const policies = await request.client.administration.policies.list(request.tenantContext(), { limit: 50 });
  const create = (
    <AdminMutationDialog title="Create policy" description="Create a tenant-scoped permission policy. Statements and bindings remain explicit follow-up operations." triggerLabel="Create policy" submitLabel="Create policy" action={createPolicyAction}>
      <AdminField label="Display name" name="displayName" autoComplete="off" required maxLength={200} />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );
  return (
    <section className="ia-page">
      <AdminPageHeader eyebrow="Tenant access" badge="RBAC delegated" title="Policies" description="Permission policy containers remain structured inputs to the server-side authorization path; wildcard evaluation stays in the external RBAC engine." actions={create} />
      <AdminEntityTable title="Permission policies" description="Manage named policy containers before attaching statements and resource-scoped bindings." entityLabel="policies" rows={policies.map((policy) => ({ id: policy.policyId, name: policy.displayName, status: policy.status === 1 ? "Active" : "Inactive", version: policy.version }))} />
      <AdminSecurityBanner title="No wildcard evaluator exists in the UI." description="Capability patterns can be authored through the typed API, but all matching and final authorization decisions remain server-side." />
    </section>
  );
}
