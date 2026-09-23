import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { createUserAction } from "../actions";

export default async function UsersPage() {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const users = await request.client.listUsers(request.administrationContext, { limit: 50 });
  const create = (
    <AdminMutationDialog title="Create user" description="Create a new identity record in the current identity scope." triggerLabel="Create user" submitLabel="Create user" action={createUserAction}>
      <AdminField label="Display name" name="displayName" autoComplete="off" required maxLength={200} />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );
  return <section className="ia-page"><AdminPageHeader title="Users" description="Identity records and account lifecycle." actions={create} /><AdminEntityTable rows={users.map((user) => ({ id: user.userId, name: user.displayName, status: user.status === 1 ? "Active" : "Inactive", version: user.version }))} /></section>;
}
