import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { createScopeAuthorityGroupAction, createScopeAuthorityPolicyAction } from "../actions";

export default async function AuthorityPage({ searchParams }: { readonly searchParams: Promise<{ readonly groupId?: string; readonly policyId?: string }> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const { groupId, policyId } = await searchParams;
  const [group, policy] = await Promise.all([
    groupId ? request.client.getScopeAuthorityGroup(request.administrationContext, groupId) : Promise.resolve(null),
    policyId ? request.client.getScopeAuthorityPolicy(request.administrationContext, policyId) : Promise.resolve(null),
  ]);
  const actions = (
    <div className="ia-action-row">
      <AdminMutationDialog title="Create authority group" description="Create an identity-scope administration group without synthesizing a tenant." triggerLabel="New authority group" submitLabel="Create group" action={createScopeAuthorityGroupAction}>
        <AdminField label="Display name" name="displayName" required maxLength={200} />
        <AdminStatusField name="status" />
      </AdminMutationDialog>
      <AdminMutationDialog title="Create authority policy" description="Create a policy used only by identity-scope administration authority." triggerLabel="New authority policy" submitLabel="Create policy" action={createScopeAuthorityPolicyAction}>
        <AdminField label="Display name" name="displayName" required maxLength={200} />
        <AdminStatusField name="status" />
      </AdminMutationDialog>
    </div>
  );
  return (
    <section className="ia-page">
      <AdminPageHeader title="Scope authority" description="Identity-scope administration groups and policies." actions={actions} />
      <section className="ia-card">
        <form className="ia-lookup-grid" method="get">
          <AdminField label="Group ID" name="groupId" defaultValue={groupId ?? ""} />
          <AdminField label="Policy ID" name="policyId" defaultValue={policyId ?? ""} />
          <button className="ia-button ia-button-secondary" type="submit">Lookup</button>
        </form>
        <div className="ia-card-grid">
          <div><h2>Group result</h2>{group ? <pre className="ia-code-panel">{JSON.stringify(group, null, 2)}</pre> : <p className="ia-muted">No group loaded.</p>}</div>
          <div><h2>Policy result</h2>{policy ? <pre className="ia-code-panel">{JSON.stringify(policy, null, 2)}</pre> : <p className="ia-muted">No policy loaded.</p>}</div>
        </div>
      </section>
    </section>
  );
}
