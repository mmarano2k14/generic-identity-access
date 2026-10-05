import type { GenericIdentityOrganizationsClient } from "@generic-identity/auth";
import type {
  IdentityCreateOrganizationRequest,
  IdentityOrganizationMembershipRecord,
  IdentityOrganizationRecord,
  IdentityOrganizationResourceScopeLinkRecord,
} from "@generic-identity/contracts/organizations";

export function acceptOrganizations(
  client: GenericIdentityOrganizationsClient,
  create: IdentityCreateOrganizationRequest,
  organization: IdentityOrganizationRecord,
  membership: IdentityOrganizationMembershipRecord,
  link: IdentityOrganizationResourceScopeLinkRecord,
): void {
  void client;
  void create;
  void organization;
  void membership;
  void link;
}
