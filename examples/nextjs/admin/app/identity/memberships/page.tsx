import Link from "next/link";
import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminAddTenantMemberDialog } from "../../../components/AdminAddTenantMemberDialog";
import { AdminManageMemberGroupsDialog, type AdminMemberGroupOption } from "../../../components/AdminManageMemberGroupsDialog";
import { AdminIcon } from "../../../components/AdminIcon";
import { AdminMembershipMemberTable, AdminMembershipTenantTable } from "../../../components/AdminTenantMembershipOverview";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminRecordContext } from "../../../components/AdminRecordContext";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { IdentityAccessAdminMembershipOverviewService } from "../../../server/IdentityAccessAdminMembershipOverviewService";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { createTenantAction, updateTenantMembershipAction } from "../actions";

type SearchParams = {
  readonly tenantId?: string;
};

export default async function MembershipsPage({ searchParams }: { readonly searchParams: Promise<SearchParams> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const { tenantId } = await searchParams;
  const overview = await new IdentityAccessAdminMembershipOverviewService(request).load(tenantId);

  const createTenant = overview.scopeWide ? (
    <AdminMutationDialog
      title="Create tenant"
      description="Create an empty tenant security boundary. Users are attached later through explicit memberships."
      triggerLabel="Create tenant"
      submitLabel="Create tenant"
      action={createTenantAction}
    >
      <AdminField label="Display name" name="displayName" autoComplete="off" required maxLength={200} />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  ) : null;

  const tenantRows = overview.tenants.map((tenant) => ({
    tenantId: tenant.tenantId,
    displayName: tenant.displayName ?? (overview.scopeWide ? "Tenant" : "Current tenant"),
    status: tenant.status === undefined ? "Current" : tenant.status === 1 ? "Active" : "Inactive",
    memberCountLabel: tenant.memberCount === undefined
      ? "Own membership"
      : `${tenant.memberCount} ${tenant.memberCount === 1 ? "member" : "members"}`,
    selected: overview.selectedTenant?.tenantId === tenant.tenantId,
  }));

  const groupsById = new Map(overview.groups.map((group) => [group.groupId, group]));
  const assignedGroupIdsByMembership = new Map<string, Set<string>>();
  for (const assignment of overview.groupAssignments) {
    const ids = assignedGroupIdsByMembership.get(assignment.tenantMembershipId) ?? new Set<string>();
    ids.add(assignment.groupId);
    assignedGroupIdsByMembership.set(assignment.tenantMembershipId, ids);
  }


  const selectedTenantId = overview.selectedTenant?.tenantId;
  const canManageGroups = overview.permissions.canReadGroups
    && overview.permissions.canReadGroupMemberships
    && overview.permissions.canManageGroupMemberships;

  const groupOptionsFor = (membershipId: string): readonly AdminMemberGroupOption[] => {
    const assigned = assignedGroupIdsByMembership.get(membershipId) ?? new Set<string>();
    return overview.groups.map((group) => ({
      key: `group:${group.groupId}`,
      displayName: group.displayName,
      isTemplate: group.isTemplate,
      checked: assigned.has(group.groupId),
    })).sort((left, right) => left.displayName.localeCompare(right.displayName));
  };

  const badgesFor = (membershipId: string) => Array.from(assignedGroupIdsByMembership.get(membershipId) ?? [])
    .flatMap((groupId) => {
      const group = groupsById.get(groupId);
      return group ? [{ groupId: group.groupId, displayName: group.displayName, isTemplate: group.isTemplate }] : [];
    })
    .sort((left, right) => left.displayName.localeCompare(right.displayName));

  const directoryVisible = overview.permissions.canListMembers;
  const memberRows = directoryVisible
    ? overview.members.map((member) => ({
        membershipId: member.membershipId,
        userId: member.userId,
        displayName: member.displayName,
        userStatus: member.userStatus === 1 ? "Active" : "Inactive",
        membershipStatus: member.membershipStatus === 1 ? "Active" : "Inactive",
        version: member.membershipVersion,
        groups: badgesFor(member.membershipId),
        actions: selectedTenantId ? (
          <>
            {overview.scopeWide ? (
              <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/users?userId=${encodeURIComponent(member.userId)}&tenantId=${encodeURIComponent(member.tenantId)}`}><AdminIcon name="manage" />Open user</Link>
            ) : null}
            <AdminManageMemberGroupsDialog
              tenantId={selectedTenantId}
              tenantMembershipId={member.membershipId}
              userDisplayName={member.displayName}
              options={groupOptionsFor(member.membershipId)}
              enabled={canManageGroups}
            />
            {overview.permissions.canAddMembers ? (
              <AdminMutationDialog
                title="Edit tenant membership"
                description="Change only the lifecycle state of this tenant relationship."
                triggerLabel="Edit"
                submitLabel="Save changes"
                action={updateTenantMembershipAction}
                triggerVariant="secondary"
                triggerIcon="edit"
                compact
              >
                <input type="hidden" name="tenantId" value={member.tenantId} />
                <input type="hidden" name="membershipId" value={member.membershipId} />
                <input type="hidden" name="expectedVersion" value={member.membershipVersion} />
                <AdminStatusField name="status" defaultValue={String(member.membershipStatus)} />
              </AdminMutationDialog>
            ) : null}
          </>
        ) : undefined,
      }))
    : overview.ownMembership
      ? [{
          membershipId: overview.ownMembership.membershipId,
          userId: overview.ownMembership.userId,
          displayName: "Current user",
          userStatus: "Active",
          membershipStatus: overview.ownMembership.status === 1 ? "Active" : "Inactive",
          version: overview.ownMembership.version,
          groups: badgesFor(overview.ownMembership.membershipId),
          actions: selectedTenantId ? (
            <AdminManageMemberGroupsDialog
              tenantId={selectedTenantId}
              tenantMembershipId={overview.ownMembership.membershipId}
              userDisplayName="Current user"
              options={groupOptionsFor(overview.ownMembership.membershipId)}
              enabled={canManageGroups}
            />
          ) : undefined,
        }]
      : [];

  const selectedTenantName = overview.selectedTenant?.displayName
    ?? (overview.selectedTenant ? "Current tenant" : undefined);

  return (
    <section className="ia-page">
      <AdminPageHeader
        eyebrow="Tenant access"
        badge={overview.scopeWide ? "Identity scope" : "Membership limited"}
        title="Memberships"
        description={overview.scopeWide
          ? "Select a tenant boundary, inspect its member count, and manage tenant membership lifecycle without using technical user/tenant lookup pairs."
          : "Your tenant visibility is derived from active trusted memberships. Other tenants and their directories are not loaded into this page."}
        actions={createTenant}
      />

      <AdminMembershipTenantTable rows={tenantRows} />

      {overview.selectedTenant ? (
        <>
          <AdminRecordContext
            kicker={overview.scopeWide ? "Selected tenant" : "Current tenant membership"}
            title={selectedTenantName ?? "Tenant"}
            description={overview.scopeWide
              ? "Membership administration stays inside this concrete tenant boundary. Tenant creation and membership creation remain independent operations."
              : "This tenant is present because the authenticated subject has an active membership. No other tenant directory is exposed."}
            identifier={overview.selectedTenant.tenantId}
            status={overview.selectedTenant.status === undefined ? undefined : overview.selectedTenant.status === 1 ? "Active" : "Inactive"}
            version={overview.selectedTenant.version}
            facts={[
              { label: "Visibility", value: overview.scopeWide ? "Identity-scope administration" : directoryVisible ? "Delegated tenant administration" : "Own active membership" },
              { label: "Members visible", value: directoryVisible ? String(overview.members.length) : overview.ownMembership ? "1 (self)" : "0" },
              { label: "Groups visible", value: overview.permissions.canReadGroups ? String(overview.groups.length) : "Not authorized" },
            ]}
            actions={selectedTenantId ? (
              <AdminAddTenantMemberDialog
                tenantId={selectedTenantId}
                scopeWide={overview.scopeWide}
                enabled={overview.permissions.canAddMembers}
              />
            ) : undefined}
            closeHref="/identity/memberships"
            closeLabel="Back to tenants"
          />
          <AdminMembershipMemberTable rows={memberRows} selfOnly={!directoryVisible} />
        </>
      ) : (
        <AdminSecurityBanner
          title={overview.scopeWide ? "Select a tenant to inspect memberships." : "Select one of your active tenant memberships."}
          description={overview.scopeWide
            ? "Tenant creation may occur with zero users. Memberships are added independently and never imply permission."
            : "Only tenant identifiers from the trusted effective administration context can be selected."}
        />
      )}

      <AdminSecurityBanner
        title="Membership is not permission."
        description="Joining a tenant does not implicitly grant capabilities. Group assignments are tenant-scoped, templates remain globally immutable from tenant context, and managed policy/RBAC evaluation stays authoritative."
      />
    </section>
  );
}
