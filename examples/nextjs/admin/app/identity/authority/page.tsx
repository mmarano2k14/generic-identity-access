import { AdminDetailCard } from "../../../components/AdminDetailCard";
import { AdminField, AdminStatusField } from "../../../components/AdminField";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { createScopeAuthorityGroupAction, createScopeAuthorityPolicyAction } from "../actions";

export default async function AuthorityPage({ searchParams }: { readonly searchParams: Promise<{ readonly groupId?: string; readonly policyId?: string }> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const { groupId, policyId } = await searchParams;
  const [group, policy] = await Promise.all([
    groupId ? request.client.administration.scopeAuthority.getGroup(request.administrationContext, groupId) : Promise.resolve(null),
    policyId ? request.client.administration.scopeAuthority.getPolicy(request.administrationContext, policyId) : Promise.resolve(null),
  ]);
  const actions = (
    <div className="ia-action-row">
      <AdminMutationDialog title="Create authority group" description="Create an identity-scope administration group without synthesizing a tenant." triggerLabel="New authority group" submitLabel="Create group" action={createScopeAuthorityGroupAction}>
        <AdminField label="Display name" name="displayName" required maxLength={200} />
        <AdminStatusField name="status" />
      </AdminMutationDialog>
      <AdminMutationDialog title="Create authority policy" description="Create a policy used only by identity-scope administration authority." triggerLabel="New authority policy" submitLabel="Create policy" action={createScopeAuthorityPolicyAction}>
        <AdminField label="Display name" name="displayName" required maxLength={200} />
        <AdminStatusField name="status" />
      </AdminMutationDialog>
    </div>
  );

  const groupFields = group ? [
    { label: "Group ID", value: group.groupId, mono: true },
    { label: "Display name", value: group.displayName },
    { label: "Status", value: group.status === 1 ? "Active" : "Inactive" },
    { label: "Version", value: `v${group.version}` },
  ] : [];
  const policyFields = policy ? [
    { label: "Policy ID", value: policy.policyId, mono: true },
    { label: "Display name", value: policy.displayName },
    { label: "Status", value: policy.status === 1 ? "Active" : "Inactive" },
    { label: "Version", value: `v${policy.version}` },
  ] : [];

  return (
    <section className="ia-page">
      <AdminPageHeader eyebrow="Security" badge="Scope global" title="Scope authority" description="Identity-scope administration groups and policies operate without synthetic tenant records." actions={actions} />
      <section className="ia-card ia-lookup-card">
        <div className="ia-card-heading"><div><p className="ia-card-kicker">Authority lookup</p><h2>Inspect global administration objects</h2><p>Load a scope authority group, policy, or both by their stable identifiers.</p></div></div>
        <form className="ia-lookup-grid" method="get">
          <AdminField label="Group ID" name="groupId" defaultValue={groupId ?? ""} />
          <AdminField label="Policy ID" name="policyId" defaultValue={policyId ?? ""} />
          <button className="ia-button ia-button-secondary" type="submit">Lookup authority</button>
        </form>
      </section>
      <div className="ia-card-grid">
        <AdminDetailCard title="Authority group" description="Identity-scope administration group." fields={groupFields} emptyMessage={groupId ? "No authority group matched this identifier." : "Enter a group ID to load authority details."} />
        <AdminDetailCard title="Authority policy" description="Identity-scope administration policy." fields={policyFields} emptyMessage={policyId ? "No authority policy matched this identifier." : "Enter a policy ID to load authority details."} />
      </div>
      <AdminSecurityBanner title="Scope authority is deliberately tenant-free." description="These objects govern administration at the identity-scope boundary and do not rely on synthetic tenant identities." />
    </section>
  );
}
