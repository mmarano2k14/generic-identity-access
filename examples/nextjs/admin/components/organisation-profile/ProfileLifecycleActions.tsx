import type { IdentityOrganisationProfileRecord } from "@identity-access/client";
import { AdminMutationDialog } from "../AdminMutationDialog";
import {
  disableOrganisationProfileAction,
  enableOrganisationProfileAction,
} from "../../app/organisations/[organizationId]/profile/actions";

export function ProfileLifecycleActions({
  tenantId,
  organizationId,
  profile,
  canWrite,
}: {
  readonly tenantId: string;
  readonly organizationId: string;
  readonly profile: IdentityOrganisationProfileRecord;
  readonly canWrite: boolean;
}) {
  if (!canWrite) return null;

  const active = profile.status === 1;
  const action = active
    ? disableOrganisationProfileAction
    : enableOrganisationProfileAction;

  return (
    <AdminMutationDialog
      title={`${active ? "Disable" : "Enable"} OrganisationProfile`}
      description={active
        ? "Disable semantic participation without deleting profile identity, template history, overrides, or immutable versions."
        : "Re-enable the retained semantic profile for normal profile-aware operations."}
      triggerLabel={active ? "Disable profile" : "Enable profile"}
      submitLabel={active ? "Disable profile" : "Enable profile"}
      action={action}
      dangerous={active}
      triggerVariant={active ? "danger" : "secondary"}
      triggerIcon={active ? "lock" : "check"}
    >
      <input type="hidden" name="tenantId" value={tenantId} />
      <input type="hidden" name="organizationId" value={organizationId} />
      <input type="hidden" name="organisationProfileId" value={profile.organisationProfileId} />
      <input type="hidden" name="expectedRowVersion" value={profile.rowVersion} />
    </AdminMutationDialog>
  );
}
