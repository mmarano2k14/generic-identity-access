import { AdminDetailCard } from "../../../components/AdminDetailCard";
import { AdminEntityAutocomplete } from "../../../components/AdminEntityAutocomplete";
import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import {
  addScopeAuthorityMemberAction,
  addScopeAuthorityPolicyBindingAction,
  addScopeAuthorityPolicyStatementAction,
  createScopeAuthorityGroupAction,
  createScopeAuthorityPolicyAction,
  removeScopeAuthorityMemberAction,
  removeScopeAuthorityPolicyBindingAction,
  removeScopeAuthorityPolicyStatementAction,
  updateScopeAuthorityGroupAction,
  updateScopeAuthorityPolicyAction,
} from "../actions";

export default async function AuthorityPage({ searchParams }: { readonly searchParams: Promise<{ readonly groupId?: string; readonly policyId?: string }> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const context = request.administrationContext;
  const { groupId, policyId } = await searchParams;
  const [group, policy] = await Promise.all([
    groupId ? request.client.administration.scopeAuthority.getGroup(context, groupId) : Promise.resolve(null),
    policyId ? request.client.administration.scopeAuthority.getPolicy(context, policyId) : Promise.resolve(null),
  ]);
  const [members, statements, bindings] = await Promise.all([
    group ? request.client.administration.scopeAuthority.listMembers(context, group.groupId) : Promise.resolve([]),
    policy ? request.client.administration.scopeAuthority.listPolicyStatements(context, policy.policyId) : Promise.resolve([]),
    group ? request.client.administration.scopeAuthority.listPolicyBindings(context, group.groupId) : Promise.resolve([]),
  ]);
  const [memberUsers, boundPolicies] = await Promise.all([
    Promise.all(members.map((member) => request.client.administration.users.get(context, member.userId))),
    Promise.all(bindings.map((binding) => request.client.administration.scopeAuthority.getPolicy(context, binding.policyId))),
  ]);
  const usersById = new Map(memberUsers.filter((user) => user !== null).map((user) => [user.userId, user]));
  const policiesById = new Map(boundPolicies.filter((item) => item !== null).map((item) => [item.policyId, item]));

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

  const groupFields = group ? [
    { label: "Group ID", value: group.groupId, mono: true },
    { label: "Display name", value: group.displayName },
    { label: "Status", value: group.status === 1 ? "Active" : "Inactive" },
    { label: "Version", value: `v${group.version}` },
  ] : [];
  const policyFields = policy ? [
    { label: "Policy ID", value: policy.policyId, mono: true },
    { label: "Display name", value: policy.displayName },
    { label: "Status", value: policy.status === 1 ? "Active" : "Inactive" },
    { label: "Version", value: `v${policy.version}` },
  ] : [];

  return (
    <section className="ia-page">
      <AdminPageHeader eyebrow="Security" badge="Scope global" title="Scope authority" description="Manage identity-scope administration groups, policies, members, statements, and bindings without synthetic tenant records." actions={actions} />
      <section className="ia-card ia-lookup-card">
        <div className="ia-card-heading"><div><p className="ia-card-kicker">Authority lookup</p><h2>Inspect global administration objects</h2><p>Load a scope authority group, policy, or both by their stable identifiers.</p></div></div>
        <form className="ia-lookup-grid" method="get">
          <AdminEntityAutocomplete label="Authority group" name="groupId" kind="authority-group" defaultValue={groupId ?? ""} emptyLabel="No group selected" hint="Type at least 3 characters of the group display name, or enter the full ID." />
          <AdminEntityAutocomplete label="Authority policy" name="policyId" kind="authority-policy" defaultValue={policyId ?? ""} emptyLabel="No policy selected" hint="Type at least 3 characters of the policy display name, or enter the full ID." />
          <button className="ia-button ia-button-secondary" type="submit">Lookup authority</button>
        </form>
      </section>

      <div className="ia-card-grid">
        <AdminDetailCard title="Authority group" description="Identity-scope administration group." fields={groupFields} emptyMessage={groupId ? "No authority group matched this identifier." : "Enter a group ID to load authority details."} />
        <AdminDetailCard title="Authority policy" description="Identity-scope administration policy." fields={policyFields} emptyMessage={policyId ? "No authority policy matched this identifier." : "Enter a policy ID to load authority details."} />
      </div>

      {(group || policy) ? (
        <section className="ia-management-workspace">
          <div className="ia-management-heading">
            <div><p className="ia-card-kicker">Authority maintenance</p><h2>Edit selected authority objects</h2><p>Add/remove relationship edges and update lifecycle-managed records.</p></div>
            <div className="ia-action-row">
              {group ? (
                <AdminMutationDialog title="Edit authority group" description="Update this identity-scope administration group." triggerLabel="Edit group" submitLabel="Save group" action={updateScopeAuthorityGroupAction} triggerVariant="secondary" triggerIcon="edit" compact>
                  <input type="hidden" name="groupId" value={group.groupId} />
                  <input type="hidden" name="expectedVersion" value={group.version} />
                  <AdminField label="Display name" name="displayName" defaultValue={group.displayName} required maxLength={200} />
                  <AdminStatusField name="status" defaultValue={String(group.status)} />
                </AdminMutationDialog>
              ) : null}
              {policy ? (
                <AdminMutationDialog title="Edit authority policy" description="Update this identity-scope administration policy." triggerLabel="Edit policy" submitLabel="Save policy" action={updateScopeAuthorityPolicyAction} triggerVariant="secondary" triggerIcon="edit" compact>
                  <input type="hidden" name="policyId" value={policy.policyId} />
                  <input type="hidden" name="expectedVersion" value={policy.version} />
                  <AdminField label="Display name" name="displayName" defaultValue={policy.displayName} required maxLength={200} />
                  <AdminStatusField name="status" defaultValue={String(policy.status)} />
                </AdminMutationDialog>
              ) : null}
            </div>
          </div>

          <div className="ia-management-grid">
            {group ? (
              <section className="ia-card">
                <div className="ia-card-heading">
                  <div><p className="ia-card-kicker">Members</p><h2>Authority group members</h2><p>Identity-scope users attached directly to this administration group.</p></div>
                  <AdminMutationDialog title="Add authority member" description="Attach an identity-scope user to this authority group." triggerLabel="Add member" submitLabel="Add member" action={addScopeAuthorityMemberAction} compact>
                    <input type="hidden" name="groupId" value={group.groupId} />
                    <AdminEntityAutocomplete label="User" name="userId" kind="user" required hint="Type at least 3 characters of the user display name, or enter the full ID." />
                  </AdminMutationDialog>
                </div>
                {members.length === 0 ? <p className="ia-empty-inline">No authority members are assigned.</p> : (
                  <div className="ia-manage-list">
                    {members.map((member) => (
                      <div className="ia-manage-row" key={member.userId}>
                        <div><strong>{usersById.get(member.userId)?.displayName ?? member.userId}</strong><span>{member.userId}</span></div>
                        <AdminMutationDialog title="Remove authority member" description="Remove this user from the authority group. Type REMOVE to confirm." triggerLabel="Remove" submitLabel="Remove member" action={removeScopeAuthorityMemberAction} dangerous compact>
                          <input type="hidden" name="groupId" value={group.groupId} />
                          <input type="hidden" name="userId" value={member.userId} />
                          <AdminField label="Confirmation" name="confirmation" required placeholder="REMOVE" />
                        </AdminMutationDialog>
                      </div>
                    ))}
                  </div>
                )}
              </section>
            ) : null}

            {policy ? (
              <section className="ia-card">
                <div className="ia-card-heading">
                  <div><p className="ia-card-kicker">Statements</p><h2>Authority policy statements</h2><p>Typed capability statements used for global administration authority.</p></div>
                  <AdminMutationDialog title="Add authority policy statement" description="Add one capability pattern to this scope-authority policy." triggerLabel="Add statement" submitLabel="Add statement" action={addScopeAuthorityPolicyStatementAction} compact>
                    <input type="hidden" name="policyId" value={policy.policyId} />
                    <AdminField label="Security model version" name="modelVersion" type="number" min={1} step={1} required />
                    <AdminField label="Resource" name="resource" required maxLength={128} />
                    <AdminField label="Feature" name="feature" required maxLength={128} />
                    <AdminField label="Action" name="action" required maxLength={128} />
                  </AdminMutationDialog>
                </div>
                {statements.length === 0 ? <p className="ia-empty-inline">No statements are defined for this policy.</p> : (
                  <div className="ia-manage-list">
                    {statements.map((statement) => (
                      <div className="ia-manage-row" key={statement.statementId}>
                        <div><strong>{statement.resource}:{statement.feature}:{statement.action}</strong><span>Model v{statement.modelVersion}</span><code>{statement.statementId}</code></div>
                        <AdminMutationDialog title="Remove authority statement" description="Remove this statement from the authority policy. Type REMOVE to confirm." triggerLabel="Remove" submitLabel="Remove statement" action={removeScopeAuthorityPolicyStatementAction} dangerous compact>
                          <input type="hidden" name="policyId" value={policy.policyId} />
                          <input type="hidden" name="statementId" value={statement.statementId} />
                          <AdminField label="Confirmation" name="confirmation" required placeholder="REMOVE" />
                        </AdminMutationDialog>
                      </div>
                    ))}
                  </div>
                )}
              </section>
            ) : null}

            {group ? (
              <section className="ia-card">
                <div className="ia-card-heading">
                  <div><p className="ia-card-kicker">Bindings</p><h2>Authority policy bindings</h2><p>Policies granted directly to the selected authority group.</p></div>
                  <AdminMutationDialog title="Add authority policy binding" description="Attach an existing identity-scope authority policy to this group." triggerLabel="Add binding" submitLabel="Add binding" action={addScopeAuthorityPolicyBindingAction} compact>
                    <input type="hidden" name="groupId" value={group.groupId} />
                    <AdminEntityAutocomplete label="Authority policy" name="policyId" kind="authority-policy" defaultValue={policy?.policyId ?? ""} required hint="Type at least 3 characters of the policy display name, or enter the full ID." />
                  </AdminMutationDialog>
                </div>
                {bindings.length === 0 ? <p className="ia-empty-inline">No policies are bound to this authority group.</p> : (
                  <div className="ia-manage-list">
                    {bindings.map((binding) => (
                      <div className="ia-manage-row" key={binding.policyId}>
                        <div><strong>{policiesById.get(binding.policyId)?.displayName ?? binding.policyId}</strong><span>{binding.policyId}</span></div>
                        <AdminMutationDialog title="Remove authority policy binding" description="Remove this policy grant from the authority group. Type REMOVE to confirm." triggerLabel="Remove" submitLabel="Remove binding" action={removeScopeAuthorityPolicyBindingAction} dangerous compact>
                          <input type="hidden" name="groupId" value={group.groupId} />
                          <input type="hidden" name="policyId" value={binding.policyId} />
                          <AdminField label="Confirmation" name="confirmation" required placeholder="REMOVE" />
                        </AdminMutationDialog>
                      </div>
                    ))}
                  </div>
                )}
              </section>
            ) : null}
          </div>
        </section>
      ) : null}

      <AdminSecurityBanner title="Scope authority is deliberately tenant-free." description="These objects govern administration at the identity-scope boundary and do not rely on synthetic tenant identities. Destructive relationship removals are explicit and confirmed." />
    </section>
  );
}
