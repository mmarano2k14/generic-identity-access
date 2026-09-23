import { AdminEntityTable } from "../../../components/AdminEntityTable";
import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { createGroupAction } from "../actions";

export default async function GroupsPage() {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const groups = await request.client.listGroups(request.tenantContext(), { limit: 50 });
  const create = (
    <AdminMutationDialog title="Create group" description="Create a tenant-scoped group for explicit authorization assignments." triggerLabel="Create group" submitLabel="Create group" action={createGroupAction}>
      <AdminField label="Display name" name="displayName" autoComplete="off" required maxLength={200} />
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );
  return (
    <section className="ia-page">
      <AdminPageHeader eyebrow="Tenant access" badge="Tenant scoped" title="Groups" description="Organize tenant members into reusable authorization groups without turning group names into implicit permissions." actions={create} />
      <AdminEntityTable title="Authorization groups" description="Groups collect members; policies and bindings determine what those members can actually do." entityLabel="groups" rows={groups.map((group) => ({ id: group.groupId, name: group.displayName, status: group.status === 1 ? "Active" : "Inactive", version: group.version }))} />
    </section>
  );
}
