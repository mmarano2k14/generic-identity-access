import Link from "next/link";
import { AdminCheckboxField, AdminField, AdminSelectField, AdminStatusField } from "../../../components/AdminField";
import { AdminCreateGroupDialog } from "../../../components/AdminCreateGroupDialog";
import { AdminCreateGroupFromTemplateDialog } from "../../../components/AdminCreateGroupFromTemplateDialog";
import { AdminEntityAutocomplete } from "../../../components/AdminEntityAutocomplete";
import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminTenantGroupCatalog } from "../../../components/AdminGroupCatalog";
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
  createGroupFromTemplateAction,
  removeGroupMemberAction,
  removeManagedGroupPolicyBindingAction,
  updateGroupAction,
  updateReusableGroupAction,
} from "../actions";

type SearchParams = { readonly tenantId?: string; readonly tenantView?: string; readonly groupId?: string };

export default async function GroupsPage({ searchParams }: { readonly searchParams: Promise<SearchParams> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const { tenantId, tenantView, groupId } = await searchParams;
  const scopeWide = request.effectiveContext.tenantVisibility === "scope-wide";
  const allTenants = tenantView === "all";
  const context = allTenants ? undefined : request.selectedTenantContext(tenantId);

  if (context === undefined && !allTenants) {
    return (
      <section className="ia-page">
        <AdminPageHeader eyebrow="Tenant access" badge={scopeWide ? "Identity scope" : "Tenant scoped"} title="Groups" description="Select a tenant to manage real groups. A reusable template is a normal group explicitly marked reusable by a scope administrator." />
        <AdminTenantContextSelector effectiveContext={request.effectiveContext} selectedTenantId={tenantId} actionPath="/identity/groups" />
        <AdminSecurityBanner title="Templates use the normal group model." description="There is no separate template catalogue or security entity. Open a tenant to create groups, clone a reusable group, or mark a real group reusable when scope authority permits it." />
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
  const reusableGroups = context ? await request.client.administration.groups.listTemplates(context, { limit: 100 }) : [];
  const [targetResourceScopes, reusableScopeRequirements] = context ? await Promise.all([
    request.client.administration.resourceScopes.list(context, { limit: 200 }),
    Promise.all(reusableGroups.map((group) =>
      request.client.administration.groups.listTemplateScopeRequirements(
        context,
        group.tenantId,
        group.groupId,
      ))),
  ]) : [[], []];
  const reusableTemplateOptions = reusableGroups.map((group, index) => ({
    value: `${group.tenantId}:${group.groupId}`,
    displayName: group.displayName,
    requirements: reusableScopeRequirements[index] ?? [],
  }));
  const selectedGroup = context && groupId
    ? groups.find((group) => group.groupId === groupId) ?? await request.client.administration.groups.get(context, groupId)
    : null;
  const [members, managedBindings] = selectedGroup && context ? await Promise.all([
    request.client.administration.groups.listMembers(context, selectedGroup.groupId),
    request.client.administration.managedPolicyBindings.list(context, selectedGroup.groupId),
  ]) : [[], []];
  const scopeIds = Array.from(new Set(managedBindings.flatMap((binding) => binding.resourceScopeId !== undefined ? [binding.resourceScopeId] : [])));
  const [memberUsers, boundScopes, managedBoundPolicies] = selectedGroup && context ? await Promise.all([
    Promise.all(members.map(async (member) => {
      const matches = await request.client.administration.tenantUsers.list(context, { search: member.tenantMembershipId, limit: 20 });
      return matches.find((record) => record.membershipId === member.tenantMembershipId) ?? null;
    })),
    Promise.all(scopeIds.map((resourceScopeId) => request.client.administration.resourceScopes.get(context, resourceScopeId))),
    Promise.all(managedBindings.map(async (binding) => {
      const matches = await request.client.administration.managedPolicyBindings.listAvailablePolicies(context, { search: binding.policyId, limit: 20 });
      return matches.find((record) => record.policyId === binding.policyId) ?? null;
    })),
  ]) : [[], [], []];
  const usersById = new Map(memberUsers.filter((user) => user !== null).map((user) => [user.userId, user]));
  const managedPoliciesById = new Map(managedBoundPolicies.filter((policy) => policy !== null).map((policy) => [policy.policyId, policy]));
  const scopesById = new Map(boundScopes.filter((scope) => scope !== null).map((scope) => [scope.resourceScopeId, scope]));

  const aggregateRows = aggregateGroups.map(({ tenant, record: group }) => ({
    key: `${tenant.tenantId}:${group.groupId}`,
    id: group.groupId,
    name: `${group.isTemplate ? "[TEMPLATE] " : ""}${group.displayName}`,
    status: group.status === 1 ? "Active" : "Inactive",
    version: group.version,
    tenant: { tenantId: tenant.tenantId, displayName: tenant.displayName },
    actions: <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/groups?tenantId=${encodeURIComponent(tenant.tenantId)}&groupId=${encodeURIComponent(group.groupId)}`}>Manage</Link>,
  }));

  const editAction = context ? (group: (typeof groups)[number]) => {
    if (!scopeWide && group.isTemplate) return undefined;
    const action = scopeWide ? updateReusableGroupAction : updateGroupAction;
    return (
      <AdminMutationDialog title="Edit group" description={scopeWide ? "Update the real group and choose whether it is reusable as a template." : "Update this tenant group."} triggerLabel="Edit" submitLabel="Save changes" action={action} triggerVariant="secondary" triggerIcon="edit" compact>
        <input type="hidden" name="tenantId" value={context.tenantId} />
        <input type="hidden" name="groupId" value={group.groupId} />
        <input type="hidden" name="expectedVersion" value={group.version} />
        <AdminField label="Display name" name="displayName" defaultValue={group.displayName} required maxLength={200} />
        <AdminStatusField name="status" defaultValue={String(group.status)} />
        {scopeWide ? <AdminCheckboxField label="Make available as template" name="isTemplate" defaultChecked={group.isTemplate} hint="Copies use this group's managed policy bindings but never its members." /> : null}
      </AdminMutationDialog>
    );
  } : () => undefined;

  const tenantRows = context ? groups.map((group) => ({
    groupId: group.groupId,
    displayName: group.displayName,
    status: group.status === 1 ? "Active" : "Inactive",
    version: group.version,
    isTemplate: group.isTemplate,
    selected: selectedGroup?.groupId === group.groupId,
    manageHref: `/identity/groups?tenantId=${encodeURIComponent(context.tenantId)}&groupId=${encodeURIComponent(group.groupId)}`,
    actions: editAction(group),
  })) : [];

  const createActions = context ? (
    <div className="ia-row-actions">
      <AdminCreateGroupDialog effectiveContext={request.effectiveContext} selectedTenantId={context.tenantId} />
      <AdminCreateGroupFromTemplateDialog
        tenantId={context.tenantId}
        templates={reusableTemplateOptions}
        targetResourceScopes={targetResourceScopes}
        action={createGroupFromTemplateAction}
      />
    </div>
  ) : undefined;

  const canMutateReusableDefinition = selectedGroup ? scopeWide || !selectedGroup.isTemplate : false;

  return (
    <section className="ia-page">
      <AdminPageHeader eyebrow="Tenant access" badge={allTenants ? "Authorized aggregate" : "Tenant scoped"} title="Groups" description={allTenants ? "Browse real groups across authorized tenants. Reusable groups are explicitly marked Template." : "Create real tenant groups, optionally clone a reusable group's policy definition, and manage membership within the tenant."} actions={createActions} />
      <AdminTenantContextSelector effectiveContext={request.effectiveContext} selectedTenantId={context?.tenantId} allTenantsSelected={allTenants} actionPath="/identity/groups" />

      {allTenants ? (
        <AdminEntityTable title="Groups across authorized tenants" description="Every row remains a real group owned by one tenant. [TEMPLATE] marks groups explicitly made reusable by a scope administrator." entityLabel="groups" rows={aggregateRows} />
      ) : context ? <AdminTenantGroupCatalog rows={tenantRows} /> : null}

      {selectedGroup && context ? (
        <section className="ia-management-workspace">
          <AdminRecordContext
            kicker={selectedGroup.isTemplate ? "Reusable tenant group" : "Tenant group"}
            title={selectedGroup.displayName}
            description={selectedGroup.isTemplate ? "This is a real tenant group whose policy definition may also be cloned into another tenant. Its members are never part of the clone." : "This real group belongs to this tenant and can be marked reusable only by a scope administrator."}
            identifier={selectedGroup.groupId}
            status={selectedGroup.status === 1 ? "Active" : "Inactive"}
            version={selectedGroup.version}
            facts={[
              { label: "Tenant", value: context.tenantId },
              { label: "Template", value: selectedGroup.isTemplate ? "Yes" : "No" },
              { label: "Members", value: members.length },
              { label: "Policy bindings", value: managedBindings.length },
            ]}
            actions={editAction(selectedGroup)}
            closeHref={`/identity/groups?tenantId=${encodeURIComponent(context.tenantId)}`}
          />

          <div className="ia-management-grid">
            <section className="ia-card">
              <div className="ia-card-heading">
                <div><p className="ia-card-kicker">Membership</p><h2>Group members</h2><p>Membership stays tenant-local and is never copied when this group is used as a template.</p></div>
                <AdminMutationDialog title="Add group member" description="Attach an existing tenant membership to this group." triggerLabel="Add member" submitLabel="Add member" action={addGroupMemberAction} compact>
                  <input type="hidden" name="tenantId" value={context.tenantId} />
                  <input type="hidden" name="groupId" value={selectedGroup.groupId} />
                  <AdminEntityAutocomplete label="Tenant member" name="tenantMembershipId" kind="tenant-membership" tenantId={context.tenantId} required hint="Type at least 3 characters of the user display name, or enter a full user/membership ID." />
                </AdminMutationDialog>
              </div>
              {members.length === 0 ? <p className="ia-empty-inline">No members are assigned to this group.</p> : <div className="ia-manage-list">{members.map((member) => (
                <div className="ia-manage-row" key={member.tenantMembershipId}>
                  <div><strong>{usersById.get(member.userId)?.displayName ?? member.userId}</strong><span>{member.userId}</span><code>{member.tenantMembershipId}</code></div>
                  <div className="ia-row-actions">
                    <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/users?tenantId=${encodeURIComponent(context.tenantId)}&userId=${encodeURIComponent(member.userId)}`}>Access insight</Link>
                    <AdminMutationDialog title="Remove group member" description="Remove only this group-membership edge. Type REMOVE to confirm." triggerLabel="Remove" submitLabel="Remove member" action={removeGroupMemberAction} dangerous compact>
                      <input type="hidden" name="tenantId" value={context.tenantId} /><input type="hidden" name="groupId" value={selectedGroup.groupId} /><input type="hidden" name="tenantMembershipId" value={member.tenantMembershipId} />
                      <AdminField label="Confirmation" name="confirmation" required placeholder="REMOVE" autoComplete="off" />
                    </AdminMutationDialog>
                  </div>
                </div>
              ))}</div>}
            </section>

            <section className="ia-card">
              <div className="ia-card-heading">
                <div><p className="ia-card-kicker">Authorization</p><h2>Managed policy bindings</h2><p>{selectedGroup.isTemplate ? "These bindings form the reusable policy definition; cloned groups receive these bindings but no memberships." : "Attach reusable application-managed policies to this group."}</p></div>
                {canMutateReusableDefinition ? <AdminMutationDialog title="Add managed policy binding" description="Grant the selected shared managed policy at its published default version." triggerLabel="Add binding" submitLabel="Add binding" action={addManagedGroupPolicyBindingAction} compact>
                  <input type="hidden" name="tenantId" value={context.tenantId} /><input type="hidden" name="groupId" value={selectedGroup.groupId} />
                  <AdminEntityAutocomplete label="Managed policy" name="managedPolicyId" kind="managed-policy" tenantId={context.tenantId} required hint="Only active policies with a published default version are selectable." />
                  <AdminEntityAutocomplete label="Resource scope" name="resourceScopeId" kind="resource-scope" tenantId={context.tenantId} emptyLabel="No resource scope" hint="Optional tenant-owned resource scope." />
                  <AdminCheckboxField label="Include descendants" name="includeDescendants" hint="Apply the binding to descendants of the selected resource scope." />
                </AdminMutationDialog> : null}
              </div>
              {managedBindings.length === 0 ? <p className="ia-empty-inline">No managed policies are bound to this group.</p> : <div className="ia-manage-list">{managedBindings.map((binding) => {
                const key = `${binding.policyId}:v${binding.policyVersion}:${binding.resourceScopeId ?? "root"}`;
                const policy = managedPoliciesById.get(binding.policyId);
                const scope = binding.resourceScopeId !== undefined ? scopesById.get(binding.resourceScopeId) : undefined;
                return <div className="ia-manage-row" key={key}>
                  <div><strong>{policy?.displayName ?? binding.policyId}</strong><span>Managed policy · v{binding.policyVersion} · {scope?.displayName ?? "No resource scope"}{binding.includeDescendants ? " · descendants" : ""}</span></div>
                  {canMutateReusableDefinition ? <AdminMutationDialog title="Remove managed policy binding" description="Remove only this binding. Type REMOVE to confirm." triggerLabel="Remove" submitLabel="Remove binding" action={removeManagedGroupPolicyBindingAction} dangerous compact>
                    <input type="hidden" name="tenantId" value={context.tenantId} /><input type="hidden" name="groupId" value={selectedGroup.groupId} /><input type="hidden" name="managedPolicyId" value={binding.policyId} /><input type="hidden" name="policyVersion" value={binding.policyVersion} />
                    {binding.resourceScopeId !== undefined ? <input type="hidden" name="resourceScopeId" value={binding.resourceScopeId} /> : null}
                    <AdminField label="Confirmation" name="confirmation" required placeholder="REMOVE" autoComplete="off" />
                  </AdminMutationDialog> : null}
                </div>;
              })}</div>}
            </section>
          </div>
        </section>
      ) : allTenants ? (
        <AdminSecurityBanner title="All authorized tenants is a collection context." description="Open Manage to enter one concrete tenant context before changing a group." />
      ) : context ? (
        <AdminSecurityBanner title="Select a tenant group to manage it." description="Create a normal group or use Create from template to clone only the reusable group's managed policy bindings." />
      ) : null}
      <AdminSecurityBanner title="A template is a property of a real group." description="Only a scope administrator can promote or demote reusable groups. Tenant administrators may clone an active reusable group, but cloned memberships are never carried over." />
    </section>
  );
}
