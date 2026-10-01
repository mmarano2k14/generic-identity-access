import type {
  IdentityAdministrationContext,
  IdentityTenantAdministrationContext,
} from "@identity-access/client";
import type {
  IdentityAdministrationListOptions,
  IdentityEffectiveAdministrationContext,
  IdentityGroupRecord,
  IdentityManagedPolicyRecord,
  IdentityMfaPolicyRecord,
  IdentityMfaUserSecurityState,
  IdentityTenantRecord,
  IdentityTenantUserListOptions,
  IdentityTenantUserRecord,
  IdentityUserAuthenticatorRecord,
  IdentityUserRecord,
} from "@generic-identity/contracts";

/**
 * Secret-bearing administration contexts remain in the auth package because
 * they carry the trusted session/bearer credential used by the server API.
 */
export type {
  IdentityAdministrationContext,
  IdentityTenantAdministrationContext,
} from "@identity-access/client";

export interface GenericIdentityAdministrationContextClient {
  get(
    context: IdentityAdministrationContext,
    signal?: AbortSignal,
  ): Promise<IdentityEffectiveAdministrationContext>;
}

export interface GenericIdentityUsersClient {
  list(
    context: IdentityAdministrationContext,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityUserRecord[]>;

  get(
    context: IdentityAdministrationContext,
    userId: string,
    signal?: AbortSignal,
  ): Promise<IdentityUserRecord | null>;
}


export interface GenericIdentityTenantsClient {
  list(
    context: IdentityAdministrationContext,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityTenantRecord[]>;

  get(
    context: IdentityAdministrationContext,
    tenantId: string,
    signal?: AbortSignal,
  ): Promise<IdentityTenantRecord | null>;
}

export interface GenericIdentityTenantUsersClient {
  list(
    context: IdentityTenantAdministrationContext,
    options?: IdentityTenantUserListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityTenantUserRecord[]>;
}

export interface GenericIdentityGroupsClient {
  list(
    context: IdentityTenantAdministrationContext,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityGroupRecord[]>;
}

export interface GenericIdentityManagedPoliciesClient {
  list(
    context: IdentityAdministrationContext,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityManagedPolicyRecord[]>;
}

export interface GenericIdentityMfaAdministrationClient {
  getPolicy(
    context: IdentityAdministrationContext,
    signal?: AbortSignal,
  ): Promise<IdentityMfaPolicyRecord | null>;

  listAuthenticators(
    context: IdentityAdministrationContext,
    userId: string,
    signal?: AbortSignal,
  ): Promise<readonly IdentityUserAuthenticatorRecord[]>;

  getUserSecurityState(
    context: IdentityAdministrationContext,
    userId: string,
    signal?: AbortSignal,
  ): Promise<IdentityMfaUserSecurityState>;
}

/**
 * Read-focused administration facade required by reusable Identity pages.
 * Mutating administration operations remain on the proven legacy client until
 * a later public-contract pack explicitly promotes them.
 */
export interface GenericIdentityAdministrationClient {
  readonly context: GenericIdentityAdministrationContextClient;
  readonly users: GenericIdentityUsersClient;
  readonly tenants: GenericIdentityTenantsClient;
  readonly tenantUsers: GenericIdentityTenantUsersClient;
  readonly groups: GenericIdentityGroupsClient;
  readonly managedPolicies: GenericIdentityManagedPoliciesClient;
  readonly mfa: GenericIdentityMfaAdministrationClient;
}
