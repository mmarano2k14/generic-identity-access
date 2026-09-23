import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { createUserAction } from "../actions";

export default async function UsersPage() {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const users = await request.client.administration.users.list(request.administrationContext, { limit: 50 });
  const create = (
    <AdminMutationDialog title="Create user" description="Create a new stable identity record in the current identity scope." triggerLabel="Create user" submitLabel="Create user" action={createUserAction}>
      <AdminField label="Display name" name="displayName" autoComplete="off" required maxLength={200} />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );
  return (
    <section className="ia-page">
      <AdminPageHeader eyebrow="Directory" badge="Identity scope" title="Users" description="Stable identity records and lifecycle state, independent from tenant placement and application permissions." actions={create} />
      <AdminEntityTable title="Identity directory" description="Search the bounded server-loaded user collection without exposing credentials or authentication secrets." entityLabel="users" rows={users.map((user) => ({ id: user.userId, name: user.displayName, status: user.status === 1 ? "Active" : "Inactive", version: user.version }))} />
      <AdminSecurityBanner title="Credentials stay separate." description="This directory view exposes identity metadata only. Password material, session tokens, refresh tokens, and authenticator secrets are never part of these records." />
    </section>
  );
}
