import Link from "next/link";
import { AdminCheckboxField, AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminCreateGroupDialog } from "../../../components/AdminCreateGroupDialog";
import { AdminEntityAutocomplete } from "../../../components/AdminEntityAutocomplete";
import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminRecordContext } from "../../../components/AdminRecordContext";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { AdminTenantContextSelector } from "../../../components/AdminTenantContextSelector";
import { IdentityAccessAdminAuthorizedTenantService } from "../../../server/IdentityAccessAdminAuthorizedTenantService";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { IdentityAccessAdminTenantAggregateLoader } from "../../../server/IdentityAccessAdminTenantAggregateLoader";
import {
  addGroupMemberAction,
  addManagedGroupPolicyBindingAction,
  removeGroupMemberAction,
  removeManagedGroupPolicyBindingAction,
  updateGroupAction,
} from "../actions";

type SearchParams = { readonly tenantId?: string; readonly tenantView?: string; readonly groupId?: string };

export default async function GroupsPage({ searchParams }: { readonly searchParams: Promise<SearchParams> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const { tenantId, tenantView, groupId } = await searchParams;
  const allTenants = tenantView === "all";
  const context = allTenants ? undefined : request.selectedTenantContext(tenantId);
  const create = <AdminCreateGroupDialog effectiveContext={request.effectiveContext} selectedTenantId={context?.tenantId} />;

  if (context === undefined && !allTenants) {
    return (
      <section className="ia-page">
        <AdminPageHeader eyebrow="Tenant access" badge="Tenant scoped" title="Groups" description="Select one authorized tenant or use All authorized tenants to browse groups without duplicating the workspace. Identity Scope Administrators can still create a group by choosing its concrete tenant inside the mutation dialog." actions={create} />
        <AdminTenantContextSelector effectiveContext={request.effectiveContext} selectedTenantId={tenantId} actionPath="/identity/groups" />
        <AdminSecurityBanner title="Tenant selection is not authorization." description="The selected tenant is validated against the trusted subject context and every protected API operation is independently authorized." />
      </section>
    );
  }

  const aggregateGroups = allTenants
    ? await IdentityAccessAdminTenantAggregateLoader.load(
      await new IdentityAccessAdminAuthorizedTenantService(request).list(),
      (tenant) => request.client.administration.groups.list(tenant.context, { limit: 50 }),
    )
    : [];
  const groups = context ? await request.client.administration.groups.list(context, { limit: 50 }) : [];
  const selectedGroup = context && groupId
    ? groups.find((group) => group.groupId === groupId) ?? await request.client.administration.groups.get(context, groupId)
    : null;
  const [members, managedBindings] = selectedGroup && context ? await Promise.all([
    request.client.administration.groups.listMembers(context, selectedGroup.groupId),
    request.client.administration.managedPolicyBindings.list(context, selectedGroup.groupId),
  ]) : [[], []];
  const scopeIds = Array.from(new Set(
    managedBindings.flatMap((binding) => binding.resourceScopeId !== undefined ? [binding.resourceScopeId] : []),
  ));
  const [memberUsers, boundScopes, managedBoundPolicies] = selectedGroup && context ? await Promise.all([
    Promise.all(members.map(async (member) => {
      const matches = await request.client.administration.tenantUsers.list(context, {
        search: member.tenantMembershipId,
        limit: 20,
      });
      return matches.find((record) => record.membershipId === member.tenantMembershipId) ?? null;
    })),
    Promise.all(scopeIds.map((resourceScopeId) =>
      request.client.administration.resourceScopes.get(context, resourceScopeId),
    )),
    Promise.all(managedBindings.map(async (binding) => {
      const matches = await request.client.administration.managedPolicyBindings.listAvailablePolicies(context, {
        search: binding.policyId,
        limit: 20,
      });
      return matches.find((record) => record.policyId === binding.policyId) ?? null;
    })),
  ]) : [[], [], []];
  const usersById = new Map(memberUsers.filter((user) => user !== null).map((user) => [user.userId, user]));
  const managedPoliciesById = new Map(managedBoundPolicies.filter((policy) => policy !== null).map((policy) => [policy.policyId, policy]));
  const scopesById = new Map(boundScopes.filter((scope) => scope !== null).map((scope) => [scope.resourceScopeId, scope]));

  const rows = allTenants
    ? aggregateGroups.map(({ tenant, record: group }) => ({
      key: `${tenant.tenantId}:${group.groupId}`,
      id: group.groupId,
      name: group.displayName,
      status: group.status === 1 ? "Active" : "Inactive",
      version: group.version,
      tenant: { tenantId: tenant.tenantId, displayName: tenant.displayName },
      actions: (
        <>
          <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/groups?tenantId=${encodeURIComponent(tenant.tenantId)}&groupId=${encodeURIComponent(group.groupId)}`}>Manage</Link>
          <AdminMutationDialog title="Edit group" description="Update the group name or lifecycle state in its concrete tenant." triggerLabel="Edit" submitLabel="Save changes" action={updateGroupAction} triggerVariant="secondary" triggerIcon="edit" compact>
            <input type="hidden" name="tenantId" value={tenant.tenantId} />
            <input type="hidden" name="groupId" value={group.groupId} />
            <input type="hidden" name="expectedVersion" value={group.version} />
            <AdminField label="Display name" name="displayName" defaultValue={group.displayName} required maxLength={200} />
            <AdminStatusField name="status" defaultValue={String(group.status)} />
          </AdminMutationDialog>
        </>
      ),
    }))
    : groups.map((group) => ({
      id: group.groupId,
      name: group.displayName,
      status: group.status === 1 ? "Active" : "Inactive",
      version: group.version,
      actions: (
        <>
          <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/groups?tenantId=${encodeURIComponent(context!.tenantId)}&groupId=${encodeURIComponent(group.groupId)}`}>Manage</Link>
          <AdminMutationDialog title="Edit group" description="Update the group name or lifecycle state." triggerLabel="Edit" submitLabel="Save changes" action={updateGroupAction} triggerVariant="secondary" triggerIcon="edit" compact>
            <input type="hidden" name="tenantId" value={context!.tenantId} />
            <input type="hidden" name="groupId" value={group.groupId} />
            <input type="hidden" name="expectedVersion" value={group.version} />
            <AdminField label="Display name" name="displayName" defaultValue={group.displayName} required maxLength={200} />
            <AdminStatusField name="status" defaultValue={String(group.status)} />
          </AdminMutationDialog>
        </>
      ),
    }));

  return (
    <section className="ia-page">
      <AdminPageHeader eyebrow="Tenant access" badge={allTenants ? "Authorized aggregate" : "Tenant scoped"} title="Groups" description={allTenants ? "Browse authorization groups across every tenant where the current subject can perform this protected read. Every row retains concrete tenant ownership." : "Create groups, edit their lifecycle, manage membership, and attach or remove explicit policy bindings."} actions={create} />
      <AdminTenantContextSelector effectiveContext={request.effectiveContext} selectedTenantId={context?.tenantId} allTenantsSelected={allTenants} actionPath="/identity/groups" />
      <AdminEntityTable
        title={allTenants ? "Authorization groups across authorized tenants" : "Authorization groups"}
        description={allTenants ? "This is one aggregate collection surface. Create chooses one concrete tenant, row edits retain their row tenant, and Manage enters that tenant before relationship administration." : "Groups collect members. Open Manage to maintain member and policy-binding edges; those edges support true removal while group identity remains lifecycle-managed."}
        entityLabel="groups"
        selectedId={selectedGroup?.groupId}
        rows={rows}
      />

      {selectedGroup && context ? (
        <section className="ia-management-workspace">
          <AdminRecordContext
            kicker="Selected group"
            title={selectedGroup.displayName}
            description="Maintain this group record and its explicit membership and policy-binding relationships from one bounded context."
            identifier={selectedGroup.groupId}
            status={selectedGroup.status === 1 ? "Active" : "Inactive"}
            version={selectedGroup.version}
            facts={[
              { label: "Tenant", value: context.tenantId },
              { label: "Members", value: members.length },
              { label: "Policy bindings", value: managedBindings.length },
            ]}
            actions={(
              <AdminMutationDialog title="Edit group" description="Update the group name or lifecycle state." triggerLabel="Edit group" submitLabel="Save changes" action={updateGroupAction} triggerVariant="secondary" triggerIcon="edit" compact>
                <input type="hidden" name="tenantId" value={context.tenantId} />
                <input type="hidden" name="groupId" value={selectedGroup.groupId} />
                <input type="hidden" name="expectedVersion" value={selectedGroup.version} />
                <AdminField label="Display name" name="displayName" defaultValue={selectedGroup.displayName} required maxLength={200} />
                <AdminStatusField name="status" defaultValue={String(selectedGroup.status)} />
              </AdminMutationDialog>
            )}
            closeHref={`/identity/groups?tenantId=${encodeURIComponent(context.tenantId)}`}
          />

          <div className="ia-management-grid">
            <section className="ia-card">
              <div className="ia-card-heading">
                <div><p className="ia-card-kicker">Membership</p><h2>Group members</h2><p>Add tenant memberships or remove existing membership edges.</p></div>
                <AdminMutationDialog title="Add group member" description="Attach an existing tenant membership to this authorization group." triggerLabel="Add member" submitLabel="Add member" action={addGroupMemberAction} compact>
                  <input type="hidden" name="tenantId" value={context.tenantId} />
                  <input type="hidden" name="groupId" value={selectedGroup.groupId} />
                  <AdminEntityAutocomplete label="Tenant member" name="tenantMembershipId" kind="tenant-membership" tenantId={context.tenantId} required hint="Type at least 3 characters of the user display name, or enter a full user/membership ID." />
                </AdminMutationDialog>
              </div>
              {members.length === 0 ? <p className="ia-empty-inline">No members are assigned to this group.</p> : (
                <div className="ia-manage-list">
                  {members.map((member) => (
                    <div className="ia-manage-row" key={member.tenantMembershipId}>
                      <div><strong>{usersById.get(member.userId)?.displayName ?? member.userId}</strong><span>{member.userId}</span><code>{member.tenantMembershipId}</code></div>
                      <div className="ia-row-actions">
                        <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/users?tenantId=${encodeURIComponent(context.tenantId)}&userId=${encodeURIComponent(member.userId)}`}>Access insight</Link>
                        <AdminMutationDialog title="Remove group member" description="Remove only this group-membership edge. The tenant membership and user remain intact. Type REMOVE to confirm." triggerLabel="Remove" submitLabel="Remove member" action={removeGroupMemberAction} dangerous compact>
                          <input type="hidden" name="tenantId" value={context.tenantId} />
                          <input type="hidden" name="groupId" value={selectedGroup.groupId} />
                          <input type="hidden" name="tenantMembershipId" value={member.tenantMembershipId} />
                          <AdminField label="Confirmation" name="confirmation" required placeholder="REMOVE" autoComplete="off" />
                        </AdminMutationDialog>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </section>

            <section className="ia-card">
              <div className="ia-card-heading">
                <div><p className="ia-card-kicker">Authorization</p><h2>Managed policy bindings</h2><p>Attach one reusable application-managed policy to this tenant group, optionally under one tenant resource scope.</p></div>
                <AdminMutationDialog title="Add managed policy binding" description="Grant the selected shared managed policy at its published default version. The binding remains owned by this tenant group." triggerLabel="Add binding" submitLabel="Add binding" action={addManagedGroupPolicyBindingAction} compact>
                  <input type="hidden" name="tenantId" value={context.tenantId} />
                  <input type="hidden" name="groupId" value={selectedGroup.groupId} />
                  <AdminEntityAutocomplete label="Managed policy" name="managedPolicyId" kind="managed-policy" tenantId={context.tenantId} required hint="Type at least 3 characters of the shared policy display name/key, or enter its full ID. Only active policies with a published default version are selectable." />
                  <AdminEntityAutocomplete label="Resource scope" name="resourceScopeId" kind="resource-scope" tenantId={context.tenantId} emptyLabel="No resource scope" hint="Optional. Resource scopes remain tenant-owned even though the managed policy definition is shared." />
                  <AdminCheckboxField label="Include descendants" name="includeDescendants" hint="Apply the binding to descendants of the selected resource scope." />
                </AdminMutationDialog>
              </div>
              {managedBindings.length === 0 ? <p className="ia-empty-inline">No managed policies are bound to this group.</p> : (
                <div className="ia-manage-list">
                  {managedBindings.map((binding) => {
                    const key = `${binding.policyId}:v${binding.policyVersion}:${binding.resourceScopeId ?? "root"}`;
                    const policy = managedPoliciesById.get(binding.policyId);
                    const scope = binding.resourceScopeId !== undefined ? scopesById.get(binding.resourceScopeId) : undefined;
                    return (
                      <div className="ia-manage-row" key={key}>
                        <div><strong>{policy?.displayName ?? binding.policyId}</strong><span>Managed policy · v{binding.policyVersion} · {scope?.displayName ?? "No resource scope"}{binding.includeDescendants ? " · descendants" : ""}</span></div>
                        <AdminMutationDialog title="Remove managed policy binding" description="Remove only this tenant-scoped binding. The shared managed policy remains available to other authorized tenants. Type REMOVE to confirm." triggerLabel="Remove" submitLabel="Remove binding" action={removeManagedGroupPolicyBindingAction} dangerous compact>
                          <input type="hidden" name="tenantId" value={context.tenantId} />
                          <input type="hidden" name="groupId" value={selectedGroup.groupId} />
                          <input type="hidden" name="managedPolicyId" value={binding.policyId} />
                          <input type="hidden" name="policyVersion" value={binding.policyVersion} />
                          {binding.resourceScopeId !== undefined ? <input type="hidden" name="resourceScopeId" value={binding.resourceScopeId} /> : null}
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
      ) : allTenants ? (
        <AdminSecurityBanner title="All authorized tenants is a collection context." description="Records remain tenant-owned. Open Manage to enter one concrete tenant context before changing relationships, members, or bindings." />
      ) : (
        <AdminSecurityBanner title="Select a group to manage assignments." description="Edit the group record from the table or open Manage to add/remove membership and policy-binding edges using the existing server-authorized contracts." />
      )}
      <AdminSecurityBanner title="Delete is exposed only where the API models deletion." description="Users, tenants, groups, policies, memberships, and resource scopes use lifecycle state. Membership and binding edges can be physically removed because the backend exposes explicit remove contracts for them." />
    </section>
  );
}
