import { AdminDetailCard } from "../../../components/AdminDetailCard";
import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { createTenantMembershipAction } from "../actions";

export default async function MembershipsPage({ searchParams }: { readonly searchParams: Promise<{ readonly userId?: string }> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const { userId } = await searchParams;
  const membership = userId ? await request.client.administration.memberships.findByUser(request.tenantContext(), userId) : null;
  const create = (
    <AdminMutationDialog title="Add tenant membership" description="Attach an existing identity-scope user to the configured tenant." triggerLabel="Add membership" submitLabel="Add membership" action={createTenantMembershipAction}>
      <AdminField label="User ID" name="userId" required autoComplete="off" hint="Use the stable user UUID from the Users page." />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );

  const fields = membership ? [
    { label: "Membership ID", value: membership.membershipId, mono: true },
    { label: "User ID", value: membership.userId, mono: true },
    { label: "Tenant ID", value: membership.tenantId, mono: true },
    { label: "Status", value: membership.status === 1 ? "Active" : "Inactive" },
    { label: "Version", value: `v${membership.version}` },
  ] : [];

  return (
    <section className="ia-page">
      <AdminPageHeader eyebrow="Tenant access" badge="Tenant scoped" title="Memberships" description="Resolve and manage the explicit relationship between a stable identity and the current tenant boundary." actions={create} />
      <section className="ia-card ia-lookup-card">
        <div className="ia-card-heading"><div><p className="ia-card-kicker">Lookup</p><h2>Inspect membership by user</h2><p>Use a stable user identifier to resolve its membership in this tenant.</p></div></div>
        <form className="ia-inline-form" method="get">
          <AdminField label="User ID" name="userId" defaultValue={userId ?? ""} required />
          <button className="ia-button ia-button-secondary" type="submit">Find membership</button>
        </form>
      </section>
      <AdminDetailCard
        title={membership ? "Membership resolved" : "Membership result"}
        description={membership ? "The current tenant relationship was loaded from the trusted administration API." : "No membership is currently loaded."}
        fields={fields}
        emptyMessage={userId ? "No membership exists for this user in the configured tenant." : "Enter a user ID above to inspect its tenant membership."}
      />
      <AdminSecurityBanner title="Membership is not permission." description="Joining a tenant does not implicitly grant capabilities. Group membership, policy bindings, resource scope, and RBAC evaluation remain separate steps." />
    </section>
  );
}
