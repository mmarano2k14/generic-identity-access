import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { createTenantMembershipAction } from "../actions";

export default async function MembershipsPage({ searchParams }: { readonly searchParams: Promise<{ readonly userId?: string }> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const { userId } = await searchParams;
  const membership = userId ? await request.client.findTenantMembershipByUser(request.tenantContext(), userId) : null;
  const create = (
    <AdminMutationDialog title="Add tenant membership" description="Attach an existing identity-scope user to the configured tenant." triggerLabel="Add membership" submitLabel="Add membership" action={createTenantMembershipAction}>
      <AdminField label="User ID" name="userId" required autoComplete="off" hint="Use the stable user UUID from the Users page." />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );
  return (
    <section className="ia-page">
      <AdminPageHeader title="Memberships" description="Resolve and manage tenant membership for a user." actions={create} />
      <section className="ia-card">
        <form className="ia-inline-form" method="get">
          <AdminField label="Find by user ID" name="userId" defaultValue={userId ?? ""} required />
          <button className="ia-button ia-button-secondary" type="submit">Find membership</button>
        </form>
        {membership ? <pre className="ia-code-panel">{JSON.stringify(membership, null, 2)}</pre> : userId ? <p className="ia-muted">No membership exists for this user in the configured tenant.</p> : <p className="ia-muted">Enter a user ID to inspect its tenant membership.</p>}
      </section>
    </section>
  );
}
