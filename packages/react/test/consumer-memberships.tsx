import type { ComponentProps } from "react";
import type {
  IdentityGroupRecord,
  IdentityOrganizationMembershipRecord,
  IdentityOrganizationRecord,
  IdentityTenantMembershipRecord,
} from "@generic-identity/contracts";
import {
  MembershipForm,
  MembershipCandidateForm,
  MembershipCandidateLookupPanel,
  MemberGroupAssignmentsForm,
  MemberOrganizationAssignmentsForm,
} from "@generic-identity/react/directory";
import { IdentityEntityAutocomplete } from "@generic-identity/react/components";

/** Public React 19 Server Action form composition probe. */
export function membershipReactProbe(
  membership: IdentityTenantMembershipRecord,
  groups: readonly IdentityGroupRecord[],
  organizations: readonly IdentityOrganizationRecord[],
  organizationMemberships: readonly IdentityOrganizationMembershipRecord[],
  formAction: Exclude<ComponentProps<"form">["action"], string | undefined>,
) {
  return <>
    <IdentityEntityAutocomplete label="User" name="userId" kind="user" required />
    <MembershipForm tenantId={membership.tenantId} formAction={formAction} />
    <MembershipForm tenantId={membership.tenantId} membership={membership} formAction={formAction} />
    <MembershipCandidateForm tenantId={membership.tenantId} formAction={formAction} />
    <MembershipCandidateLookupPanel tenantId={membership.tenantId} lookupAction={async () => ({kind: "idle" as const, loginIdentifier: ""})} createAction={formAction} />
    <MemberGroupAssignmentsForm
      tenantId={membership.tenantId} tenantMembershipId={membership.membershipId}
      groups={groups} assignedGroupIds={[]} formAction={formAction}
    />
    <MemberOrganizationAssignmentsForm
      tenantId={membership.tenantId} tenantMembershipId={membership.membershipId}
      organizations={organizations} memberships={organizationMemberships} formAction={formAction}
    />
  </>;
}
