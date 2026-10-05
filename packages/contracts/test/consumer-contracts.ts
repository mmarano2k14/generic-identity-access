import type {
  AuthorizationEvaluationResponse,
  IdentityAccessErrorCode,
  IdentityApplicationSecurityManifestRequest,
  IdentityAuthorizationBoundary,
  IdentityCapabilityRequirement,
  IdentityGroupRecord,
  IdentityManagedGroupPolicyBindingRecord,
  IdentityManagedPolicyRecord,
  IdentityMfaProviderRecord,
  IdentitySessionValidationResult,
  IdentityTenantMembershipRecord,
  IdentityTenantRecord,
  IdentityUserRecord,
} from "@generic-identity/contracts";

/** Compile-only consumer probe. It intentionally imports only the public contracts package. */
export interface SharedIdentityContractsConsumerProbe {
  readonly user: IdentityUserRecord;
  readonly tenant: IdentityTenantRecord;
  readonly membership: IdentityTenantMembershipRecord;
  readonly group: IdentityGroupRecord;
  readonly policy: IdentityManagedPolicyRecord;
  readonly binding: IdentityManagedGroupPolicyBindingRecord;
  readonly capability: IdentityCapabilityRequirement;
  readonly manifest: IdentityApplicationSecurityManifestRequest;
  readonly accessContext: IdentityAuthorizationBoundary;
  readonly session: IdentitySessionValidationResult;
  readonly mfaProvider: IdentityMfaProviderRecord;
  readonly decision: AuthorizationEvaluationResponse;
  readonly errorCode: IdentityAccessErrorCode;
}
