import Link from "next/link";
import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import {
  addPolicyStatementAction,
  createPolicyAction,
  removePolicyStatementAction,
  updatePolicyAction,
} from "../actions";

export default async function PoliciesPage({ searchParams }: { readonly searchParams: Promise<{ readonly policyId?: string }> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const { policyId } = await searchParams;
  const context = request.tenantContext();
  const policies = await request.client.administration.policies.list(context, { limit: 50 });
  const selectedPolicy = policyId ? policies.find((policy) => policy.policyId === policyId) ?? await request.client.administration.policies.get(context, policyId) : null;
  const statements = selectedPolicy ? await request.client.administration.policies.listStatements(context, selectedPolicy.policyId) : [];

  const create = (
    <AdminMutationDialog title="Create policy" description="Create a tenant-scoped permission policy. Statements and bindings remain explicit follow-up operations." triggerLabel="Create policy" submitLabel="Create policy" action={createPolicyAction}>
      <AdminField label="Display name" name="displayName" autoComplete="off" required maxLength={200} />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );

  return (
    <section className="ia-page">
      <AdminPageHeader eyebrow="Tenant access" badge="RBAC delegated" title="Policies" description="Create and edit policy containers, then add or remove typed capability statements while wildcard evaluation stays in the external RBAC engine." actions={create} />
      <AdminEntityTable
        title="Permission policies"
        description="Policy containers are lifecycle-managed. Open Manage to maintain the statements that can be physically added or removed."
        entityLabel="policies"
        rows={policies.map((policy) => ({
          id: policy.policyId,
          name: policy.displayName,
          status: policy.status === 1 ? "Active" : "Inactive",
          version: policy.version,
          actions: (
            <>
              <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/policies?policyId=${encodeURIComponent(policy.policyId)}`}>Manage</Link>
              <AdminMutationDialog title="Edit policy" description="Update the policy display name or lifecycle state." triggerLabel="Edit" submitLabel="Save changes" action={updatePolicyAction} triggerVariant="secondary" triggerIcon="edit" compact>
                <input type="hidden" name="policyId" value={policy.policyId} />
                <input type="hidden" name="expectedVersion" value={policy.version} />
                <AdminField label="Display name" name="displayName" defaultValue={policy.displayName} required maxLength={200} />
                <AdminStatusField name="status" defaultValue={String(policy.status)} />
              </AdminMutationDialog>
            </>
          ),
        }))}
      />

      {selectedPolicy ? (
        <section className="ia-management-workspace">
          <div className="ia-management-heading">
            <div><p className="ia-card-kicker">Selected policy</p><h2>{selectedPolicy.displayName}</h2><p><code className="ia-id-chip">{selectedPolicy.policyId}</code></p></div>
            <AdminMutationDialog title="Add policy statement" description="Add one typed capability pattern to this policy. Matching remains server-side in the external RBAC engine." triggerLabel="Add statement" submitLabel="Add statement" action={addPolicyStatementAction} compact>
              <input type="hidden" name="policyId" value={selectedPolicy.policyId} />
              <AdminField label="Security model version" name="modelVersion" type="number" min={1} step={1} required />
              <AdminField label="Resource" name="resource" required maxLength={128} placeholder="billing" />
              <AdminField label="Feature" name="feature" required maxLength={128} placeholder="invoice" />
              <AdminField label="Action" name="action" required maxLength={128} placeholder="read" />
            </AdminMutationDialog>
          </div>

          <section className="ia-card">
            <div className="ia-card-heading"><div><p className="ia-card-kicker">Capability statements</p><h2>Policy statements</h2><p>Each statement remains independently removable without deleting the policy identity.</p></div></div>
            {statements.length === 0 ? <p className="ia-empty-inline">This policy has no statements yet.</p> : (
              <div className="ia-manage-list">
                {statements.map((statement) => (
                  <div className="ia-manage-row" key={statement.statementId}>
                    <div>
                      <strong>{statement.resource}:{statement.feature}:{statement.action}</strong>
                      <span>Model v{statement.modelVersion}</span>
                      <code>{statement.statementId}</code>
                    </div>
                    <AdminMutationDialog title="Remove policy statement" description="Remove this statement from the policy. Type REMOVE to confirm." triggerLabel="Remove" submitLabel="Remove statement" action={removePolicyStatementAction} dangerous compact>
                      <input type="hidden" name="policyId" value={selectedPolicy.policyId} />
                      <input type="hidden" name="statementId" value={statement.statementId} />
                      <AdminField label="Confirmation" name="confirmation" required placeholder="REMOVE" autoComplete="off" />
                    </AdminMutationDialog>
                  </div>
                ))}
              </div>
            )}
          </section>
        </section>
      ) : null}

      <AdminSecurityBanner title="No wildcard evaluator exists in the UI." description="Capability patterns can be authored through the typed API, but all matching and final authorization decisions remain server-side." />
    </section>
  );
}
