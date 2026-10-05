import type {
  IdentityGroupRecord,
  IdentityManagedPolicyRecord,
  IdentityResourceScopeRecord,
} from "@generic-identity/contracts/access-control";

export type AccessControlConsumerContracts = Readonly<{
  group: IdentityGroupRecord;
  managedPolicy: IdentityManagedPolicyRecord;
  resourceScope: IdentityResourceScopeRecord;
}>;
