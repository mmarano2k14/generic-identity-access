import type {
  GenericIdentityAccountClient,
  GenericIdentityDirectoryClient,
} from "@generic-identity/auth";
import type {
  IdentityCreateTenantRequest,
  IdentityCreateUserRequest,
  IdentityTenantMembershipRecord,
} from "@generic-identity/contracts";

export function acceptAccountDirectory(
  account: GenericIdentityAccountClient,
  directory: GenericIdentityDirectoryClient,
  createUser: IdentityCreateUserRequest,
  createTenant: IdentityCreateTenantRequest,
  membership: IdentityTenantMembershipRecord,
): void {
  void account;
  void directory;
  void createUser;
  void createTenant;
  void membership;
}
