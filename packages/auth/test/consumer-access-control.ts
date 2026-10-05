import type { GenericIdentityAccessControlClient } from "@generic-identity/auth/access-control";
import type {
  IdentityGroupRecord,
  IdentityManagedPolicyRecord,
  IdentityResourceScopeRecord,
} from "@generic-identity/contracts/access-control";

export function acceptAccessControl(
  client: GenericIdentityAccessControlClient,
  group: IdentityGroupRecord,
  managedPolicy: IdentityManagedPolicyRecord,
  resourceScope: IdentityResourceScopeRecord,
): void {
  void client;
  void group;
  void managedPolicy;
  void resourceScope;
}
