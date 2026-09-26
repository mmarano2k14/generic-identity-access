import Link from "next/link";
import { AdminAccessInsight } from "../../../components/AdminAccessInsight";
import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminIcon } from "../../../components/AdminIcon";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminRecordContext } from "../../../components/AdminRecordContext";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { AdminTenantContextSelector } from "../../../components/AdminTenantContextSelector";
import { IdentityAccessAdminAccessInsightService } from "../../../server/IdentityAccessAdminAccessInsightService";
import { IdentityAccessAdminAuthorizedTenantService } from "../../../server/IdentityAccessAdminAuthorizedTenantService";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { IdentityAccessAdminTenantAggregateLoader } from "../../../server/IdentityAccessAdminTenantAggregateLoader";
import { IdentityAccessAdminUserReadService } from "../../../server/IdentityAccessAdminUserReadService";
import { createUserAction, updateUserAction } from "../actions";

type SearchParams = { readonly tenantId?: string; readonly tenantView?: string; readonly userId?: string };

export default async function UsersPage({ searchParams }: { readonly searchParams: Promise<SearchParams> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const { tenantId, tenantView, userId } = await searchParams;
  const scopeWide = request.effectiveContext.tenantVisibility === "scope-wide";
  const allTenants = tenantView === "all";
  const context = allTenants ? undefined : request.selectedTenantContext(tenantId);
  const hasTenantContext = context !== undefined || allTenants || request.hasTenantContext();

  const aggregateTenantUsers = allTenants
    ? await IdentityAccessAdminTenantAggregateLoader.load(
      await new IdentityAccessAdminAuthorizedTenantService(request).list(),
      (tenant) => request.client.administration.tenantUsers.list(tenant.context, { limit: 50 }),
    )
    : [];
  const { users, selectedUser } = allTenants
    ? { users: [], selectedUser: null }
    : await new IdentityAccessAdminUserReadService(request).load(context, userId);
  const accessInsight = selectedUser && context
    ? await new IdentityAccessAdminAccessInsightService(request).inspectUser(selectedUser, context)
    : null;

  const create = scopeWide ? (
    <AdminMutationDialog title="Create user" description="Create a new stable identity record in the current identity scope." triggerLabel="Create user" submitLabel="Create user" action={createUserAction}>
      <AdminField label="Display name" name="displayName" autoComplete="off" required maxLength={200} />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  ) : undefined;

  const selectedUserActions = selectedUser ? (
    <div className="ia-action-row">
      {scopeWide ? (
        <AdminMutationDialog title="Edit user" description="Update display metadata or lifecycle state using optimistic concurrency." triggerLabel="Edit user" submitLabel="Save changes" action={updateUserAction} triggerVariant="secondary" triggerIcon="edit" compact>
          <input type="hidden" name="userId" value={selectedUser.userId} />
          <input type="hidden" name="expectedVersion" value={selectedUser.version} />
          <AdminField label="Display name" name="displayName" defaultValue={selectedUser.displayName} required maxLength={200} />
          <AdminStatusField name="status" defaultValue={String(selectedUser.status)} />
        </AdminMutationDialog>
      ) : null}
      <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/memberships?userId=${encodeURIComponent(selectedUser.userId)}${context ? `&tenantId=${encodeURIComponent(context.tenantId)}` : ""}`}><AdminIcon name="memberships" />Membership</Link>
      {scopeWide ? <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/mfa?userId=${encodeURIComponent(selectedUser.userId)}`}><AdminIcon name="mfa" />MFA state</Link> : null}
      {scopeWide ? <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/sessions?userId=${encodeURIComponent(selectedUser.userId)}`}><AdminIcon name="sessions" />Sessions</Link> : null}
      {scopeWide ? <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/security-audit?userId=${encodeURIComponent(selectedUser.userId)}`}><AdminIcon name="audit" />Security audit</Link> : null}
    </div>
  ) : null;

  const rows = allTenants
    ? aggregateTenantUsers.map(({ tenant, record }) => ({
      key: `${tenant.tenantId}:${record.membershipId}`,
      id: record.userId,
      name: record.displayName,
      status: record.userStatus === 1 ? "Active" : "Inactive",
      version: record.userVersion,
      tenant: { tenantId: tenant.tenantId, displayName: tenant.displayName },
      actions: (
        <>
          <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/users?tenantId=${encodeURIComponent(tenant.tenantId)}&userId=${encodeURIComponent(record.userId)}`}><AdminIcon name="manage" />Details</Link>
          {scopeWide ? (
            <AdminMutationDialog title="Edit user" description="Update identity-scope metadata from this concrete tenant row." triggerLabel="Edit" submitLabel="Save changes" action={updateUserAction} triggerVariant="secondary" triggerIcon="edit" compact>
              <input type="hidden" name="userId" value={record.userId} />
              <input type="hidden" name="expectedVersion" value={record.userVersion} />
              <AdminField label="Display name" name="displayName" defaultValue={record.displayName} required maxLength={200} />
              <AdminStatusField name="status" defaultValue={String(record.userStatus)} />
            </AdminMutationDialog>
          ) : null}
        </>
      ),
    }))
    : users.map((user) => ({
      id: user.userId,
      name: user.displayName,
      status: user.status === 1 ? "Active" : "Inactive",
      version: user.version,
      actions: (
        <>
          <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/users?${context ? `tenantId=${encodeURIComponent(context.tenantId)}&` : ""}userId=${encodeURIComponent(user.userId)}`}><AdminIcon name="manage" />Details</Link>
          {scopeWide ? (
            <AdminMutationDialog title="Edit user" description="Update display metadata or lifecycle state using optimistic concurrency." triggerLabel="Edit" submitLabel="Save changes" action={updateUserAction} triggerVariant="secondary" triggerIcon="edit" compact>
              <input type="hidden" name="userId" value={user.userId} />
              <input type="hidden" name="expectedVersion" value={user.version} />
              <AdminField label="Display name" name="displayName" defaultValue={user.displayName} required maxLength={200} />
              <AdminStatusField name="status" defaultValue={String(user.status)} />
            </AdminMutationDialog>
          ) : null}
        </>
      ),
    }));

  return (
    <section className="ia-page">
      <AdminPageHeader
        eyebrow="Directory"
        badge={allTenants ? "Authorized aggregate" : scopeWide ? "Identity scope" : "Tenant bounded"}
        title="Users"
        description={allTenants ? "Browse tenant-linked users across every tenant where the current subject can perform the protected tenant-user read. The same identity may appear in more than one tenant because membership is tenant-specific." : scopeWide ? "Stable identity records and lifecycle state in the authorized identity-scope view." : "Users are constrained by the selected tenant membership boundary before paging."}
        actions={create}
      />
      <AdminTenantContextSelector effectiveContext={request.effectiveContext} selectedTenantId={context?.tenantId} allTenantsSelected={allTenants} actionPath="/identity/users" />
      {!scopeWide && context === undefined && !allTenants ? (
        <AdminSecurityBanner title="Select an authorized tenant." description="Membership-limited subjects never load the identity-scope user directory. Select one active tenant membership or use All authorized tenants to aggregate tenant-linked users." />
      ) : (
        <AdminEntityTable
          title={allTenants ? "Tenant-linked users across authorized tenants" : scopeWide ? "Identity directory" : "Tenant user directory"}
          description={allTenants ? "Each row is backed by a tenant membership and carries the concrete tenant boundary used for its read." : scopeWide ? "Create and edit identity metadata without exposing credentials." : "Only users joined to the active tenant are returned by the tenant-constrained server query."}
          entityLabel="users"
          selectedId={selectedUser?.userId}
          rows={rows}
        />
      )}
      {selectedUser ? (
        <AdminRecordContext
          kicker="Selected identity"
          title={selectedUser.displayName}
          description={scopeWide ? "Use this stable identity context to move into tenant membership or account-security administration." : "This identity is visible because it is linked to the active tenant boundary."}
          identifier={selectedUser.userId}
          status={selectedUser.status === 1 ? "Active" : "Inactive"}
          version={selectedUser.version}
          facts={[
            { label: "Identity record", value: "Stable user" },
            { label: "Credential exposure", value: "None" },
            ...(context ? [{ label: "Tenant context", value: context.tenantId, mono: true }] : []),
          ]}
          actions={selectedUserActions}
          closeHref={`/identity/users${context ? `?tenantId=${encodeURIComponent(context.tenantId)}` : ""}`}
        />
      ) : null}
      {selectedUser && accessInsight ? <AdminAccessInsight userId={selectedUser.userId} userName={selectedUser.displayName} insight={accessInsight} /> : null}
      {selectedUser && !hasTenantContext ? (
        <AdminSecurityBanner title="Tenant access insight unavailable." description="Select a server-authorized tenant context before composing tenant-scoped assignment provenance for this identity." />
      ) : null}
      {allTenants ? <AdminSecurityBanner title="All authorized tenants is a membership-backed collection view." description="The aggregate never converts memberships into permission. Details always enter one concrete tenant context before tenant-scoped access insight is composed." /> : null}
      <AdminSecurityBanner title="Credentials stay separate." description="This directory view exposes identity metadata only. Password material, session tokens, refresh tokens, and authenticator secrets are never part of these records." />
    </section>
  );
}
