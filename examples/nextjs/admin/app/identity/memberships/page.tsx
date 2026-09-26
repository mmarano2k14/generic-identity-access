import Link from "next/link";
import { AdminDetailCard } from "../../../components/AdminDetailCard";
import { AdminEntityAutocomplete } from "../../../components/AdminEntityAutocomplete";
import { AdminStatusField } from "../../../components/AdminField";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminRecordContext } from "../../../components/AdminRecordContext";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { createTenantMembershipAction, updateTenantMembershipAction } from "../actions";

type SearchParams = {
  readonly tenantId?: string;
  readonly userId?: string;
};

export default async function MembershipsPage({ searchParams }: { readonly searchParams: Promise<SearchParams> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const { tenantId, userId } = await searchParams;
  const membership = tenantId && userId
    ? await request.client.administration.memberships.findByUser(request.tenantContextFor(tenantId), userId)
    : null;

  const create = (
    <AdminMutationDialog title="Add tenant membership" description="Attach an existing identity-scope user to an explicitly selected tenant." triggerLabel="Add membership" submitLabel="Add membership" action={createTenantMembershipAction}>
      <AdminEntityAutocomplete label="Tenant" name="tenantId" kind="tenant" required hint="Type at least 3 characters of the tenant display name, or enter its full ID." />
      <AdminEntityAutocomplete label="User" name="userId" kind="user" required hint="Type at least 3 characters of the user display name, or enter its full ID." />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );

  const edit = membership ? (
    <AdminMutationDialog title="Edit tenant membership" description="Change only the lifecycle state of this explicit tenant relationship." triggerLabel="Edit membership" submitLabel="Save changes" action={updateTenantMembershipAction} triggerVariant="secondary" triggerIcon="edit">
      <input type="hidden" name="tenantId" value={membership.tenantId} />
      <input type="hidden" name="membershipId" value={membership.membershipId} />
      <input type="hidden" name="expectedVersion" value={membership.version} />
      <AdminStatusField name="status" defaultValue={String(membership.status)} />
    </AdminMutationDialog>
  ) : null;

  const fields = membership ? [
    { label: "Membership ID", value: membership.membershipId, mono: true },
    { label: "User ID", value: membership.userId, mono: true },
    { label: "Tenant ID", value: membership.tenantId, mono: true },
    { label: "Status", value: membership.status === 1 ? "Active" : "Inactive" },
    { label: "Version", value: `v${membership.version}` },
  ] : [];

  return (
    <section className="ia-page">
      <AdminPageHeader eyebrow="Tenant access" badge="Explicit tenant" title="Memberships" description="Resolve and manage the explicit relationship between a stable identity and a selected tenant boundary." actions={<div className="ia-action-row">{create}{edit}</div>} />
      <section className="ia-card ia-lookup-card">
        <div className="ia-card-heading"><div><p className="ia-card-kicker">Lookup</p><h2>Inspect tenant membership</h2><p>Select both the tenant and user. Reference search stays server-side and never preloads the directory into the browser.</p></div></div>
        <form className="ia-inline-form" method="get">
          <AdminEntityAutocomplete label="Tenant" name="tenantId" kind="tenant" defaultValue={tenantId ?? ""} required hint="Search by tenant display name or full ID." />
          <AdminEntityAutocomplete label="User" name="userId" kind="user" defaultValue={userId ?? ""} required hint="Search by user display name or full ID." />
          <button className="ia-button ia-button-secondary" type="submit">Find membership</button>
        </form>
      </section>
      {membership ? (
        <AdminRecordContext
          kicker="Selected membership"
          title="Tenant membership"
          description="This relationship links one stable identity to one explicit tenant. Permission assignment remains a separate group and policy concern."
          identifier={membership.membershipId}
          status={membership.status === 1 ? "Active" : "Inactive"}
          version={membership.version}
          facts={[
            { label: "User ID", value: membership.userId, mono: true },
            { label: "Tenant ID", value: membership.tenantId, mono: true },
          ]}
          actions={(
            <div className="ia-action-row">
              {edit}
              <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/users?userId=${encodeURIComponent(membership.userId)}`}>Open user</Link>
              <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/mfa?userId=${encodeURIComponent(membership.userId)}`}>MFA state</Link>
            </div>
          )}
          closeHref="/identity/memberships"
          closeLabel="Clear lookup"
        />
      ) : (
        <AdminDetailCard
          title="Membership result"
          description="No membership is currently loaded."
          fields={fields}
          emptyMessage={tenantId && userId ? "No membership exists for this user in the selected tenant." : "Select a tenant and user above to inspect their relationship."}
        />
      )}
      <AdminSecurityBanner title="Membership is not permission." description="Joining a tenant does not implicitly grant capabilities. Group membership, policy bindings, resource scope, and RBAC evaluation remain separate steps." />
    </section>
  );
}
