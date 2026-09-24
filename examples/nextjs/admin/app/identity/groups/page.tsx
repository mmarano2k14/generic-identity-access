import Link from "next/link";
import { AdminCheckboxField, AdminField, AdminSelectField, AdminStatusField } from "../../../components/AdminField";
import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import {
  addGroupMemberAction,
  addGroupPolicyBindingAction,
  createGroupAction,
  removeGroupMemberAction,
  removeGroupPolicyBindingAction,
  updateGroupAction,
} from "../actions";

export default async function GroupsPage({ searchParams }: { readonly searchParams: Promise<{ readonly groupId?: string }> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const { groupId } = await searchParams;
  const context = request.tenantContext();
  const groups = await request.client.administration.groups.list(context, { limit: 50 });
  const selectedGroup = groupId ? groups.find((group) => group.groupId === groupId) ?? await request.client.administration.groups.get(context, groupId) : null;
  const [members, bindings, policies, scopes] = selectedGroup ? await Promise.all([
    request.client.administration.groups.listMembers(context, selectedGroup.groupId),
    request.client.administration.policies.listBindings(context, selectedGroup.groupId),
    request.client.administration.policies.list(context, { limit: 100 }),
    request.client.administration.resourceScopes.list(context),
  ]) : [[], [], [], []];

  const create = (
    <AdminMutationDialog title="Create group" description="Create a tenant-scoped group for explicit authorization assignments." triggerLabel="Create group" submitLabel="Create group" action={createGroupAction}>
      <AdminField label="Display name" name="displayName" autoComplete="off" required maxLength={200} />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );

  return (
    <section className="ia-page">
      <AdminPageHeader eyebrow="Tenant access" badge="Tenant scoped" title="Groups" description="Create groups, edit their lifecycle, manage membership, and attach or remove explicit policy bindings." actions={create} />
      <AdminEntityTable
        title="Authorization groups"
        description="Groups collect members. Open Manage to maintain member and policy-binding edges; those edges support true removal while group identity remains lifecycle-managed."
        entityLabel="groups"
        rows={groups.map((group) => ({
          id: group.groupId,
          name: group.displayName,
          status: group.status === 1 ? "Active" : "Inactive",
          version: group.version,
          actions: (
            <>
              <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/groups?groupId=${encodeURIComponent(group.groupId)}`}>Manage</Link>
              <AdminMutationDialog title="Edit group" description="Update the group name or lifecycle state." triggerLabel="Edit" submitLabel="Save changes" action={updateGroupAction} triggerVariant="secondary" triggerIcon="edit" compact>
                <input type="hidden" name="groupId" value={group.groupId} />
                <input type="hidden" name="expectedVersion" value={group.version} />
                <AdminField label="Display name" name="displayName" defaultValue={group.displayName} required maxLength={200} />
                <AdminStatusField name="status" defaultValue={String(group.status)} />
              </AdminMutationDialog>
            </>
          ),
        }))}
      />

      {selectedGroup ? (
        <section className="ia-management-workspace">
          <div className="ia-management-heading">
            <div><p className="ia-card-kicker">Selected group</p><h2>{selectedGroup.displayName}</h2><p><code className="ia-id-chip">{selectedGroup.groupId}</code></p></div>
            <span className="ia-status ia-status-active"><span className="ia-status-dot" />Manage edges</span>
          </div>

          <div className="ia-management-grid">
            <section className="ia-card">
              <div className="ia-card-heading">
                <div><p className="ia-card-kicker">Membership</p><h2>Group members</h2><p>Add tenant memberships or remove existing membership edges.</p></div>
                <AdminMutationDialog title="Add group member" description="Attach an existing tenant membership to this authorization group." triggerLabel="Add member" submitLabel="Add member" action={addGroupMemberAction} compact>
                  <input type="hidden" name="groupId" value={selectedGroup.groupId} />
                  <AdminField label="Tenant membership ID" name="tenantMembershipId" required autoComplete="off" />
                </AdminMutationDialog>
              </div>
              {members.length === 0 ? <p className="ia-empty-inline">No members are assigned to this group.</p> : (
                <div className="ia-manage-list">
                  {members.map((member) => (
                    <div className="ia-manage-row" key={member.tenantMembershipId}>
                      <div><strong>{member.userId}</strong><code>{member.tenantMembershipId}</code></div>
                      <AdminMutationDialog title="Remove group member" description="Remove only this group-membership edge. The tenant membership and user remain intact. Type REMOVE to confirm." triggerLabel="Remove" submitLabel="Remove member" action={removeGroupMemberAction} dangerous compact>
                        <input type="hidden" name="groupId" value={selectedGroup.groupId} />
                        <input type="hidden" name="tenantMembershipId" value={member.tenantMembershipId} />
                        <AdminField label="Confirmation" name="confirmation" required placeholder="REMOVE" autoComplete="off" />
                      </AdminMutationDialog>
                    </div>
                  ))}
                </div>
              )}
            </section>

            <section className="ia-card">
              <div className="ia-card-heading">
                <div><p className="ia-card-kicker">Authorization</p><h2>Policy bindings</h2><p>Attach policy containers to this group, optionally under one resource scope.</p></div>
                <AdminMutationDialog title="Add policy binding" description="Grant one existing policy to this group in the selected resource scope." triggerLabel="Add binding" submitLabel="Add binding" action={addGroupPolicyBindingAction} compact>
                  <input type="hidden" name="groupId" value={selectedGroup.groupId} />
                  <AdminSelectField label="Policy" name="policyId" required defaultValue="">
                    <option value="" disabled>Select a policy</option>
                    {policies.map((policy) => <option key={policy.policyId} value={policy.policyId}>{policy.displayName} ({policy.policyId})</option>)}
                  </AdminSelectField>
                  <AdminSelectField label="Resource scope" name="resourceScopeId" defaultValue="" hint="Leave empty for a binding without a resource-scope restriction.">
                    <option value="">No resource scope</option>
                    {scopes.map((scope) => <option key={scope.resourceScopeId} value={scope.resourceScopeId}>{scope.displayName}</option>)}
                  </AdminSelectField>
                  <AdminCheckboxField label="Include descendants" name="includeDescendants" hint="Apply the binding to descendants of the selected resource scope." />
                </AdminMutationDialog>
              </div>
              {bindings.length === 0 ? <p className="ia-empty-inline">No policies are bound to this group.</p> : (
                <div className="ia-manage-list">
                  {bindings.map((binding) => {
                    const key = `${binding.policyId}:${binding.resourceScopeId ?? "root"}`;
                    const policy = policies.find((item) => item.policyId === binding.policyId);
                    const scope = binding.resourceScopeId ? scopes.find((item) => item.resourceScopeId === binding.resourceScopeId) : undefined;
                    return (
                      <div className="ia-manage-row" key={key}>
                        <div><strong>{policy?.displayName ?? binding.policyId}</strong><span>{scope?.displayName ?? "No resource scope"}{binding.includeDescendants ? " · descendants" : ""}</span></div>
                        <AdminMutationDialog title="Remove policy binding" description="Remove this explicit policy binding from the group. Type REMOVE to confirm." triggerLabel="Remove" submitLabel="Remove binding" action={removeGroupPolicyBindingAction} dangerous compact>
                          <input type="hidden" name="groupId" value={selectedGroup.groupId} />
                          <input type="hidden" name="policyId" value={binding.policyId} />
                          {binding.resourceScopeId ? <input type="hidden" name="resourceScopeId" value={binding.resourceScopeId} /> : null}
                          <AdminField label="Confirmation" name="confirmation" required placeholder="REMOVE" autoComplete="off" />
                        </AdminMutationDialog>
                      </div>
                    );
                  })}
                </div>
              )}
            </section>
          </div>
        </section>
      ) : (
        <AdminSecurityBanner title="Select a group to manage assignments." description="Edit the group record from the table or open Manage to add/remove membership and policy-binding edges using the existing server-authorized contracts." />
      )}
      <AdminSecurityBanner title="Delete is exposed only where the API models deletion." description="Users, tenants, groups, policies, memberships, and resource scopes use lifecycle state. Membership and binding edges can be physically removed because the backend exposes explicit remove contracts for them." />
    </section>
  );
}
