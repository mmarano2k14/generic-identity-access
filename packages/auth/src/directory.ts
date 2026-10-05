import type {
  IdentityAdministrationContext,
  IdentityTenantAdministrationContext,
} from "@identity-access/client";
import type {
  IdentityAdministrationListOptions,
  IdentityCreateTenantMembershipRequest,
  IdentityCreateTenantRequest,
  IdentityCreateUserRequest,
  IdentityMembershipStatus,
  IdentityTenantGroupAssignmentRecord,
  IdentityTenantMembershipCandidateRecord,
  IdentityTenantMembershipRecord,
  IdentityTenantRecord,
  IdentityTenantUserListOptions,
  IdentityTenantUserRecord,
  IdentityUpdateTenantMembershipRequest,
  IdentityUpdateTenantRequest,
  IdentityUpdateUserRequest,
  IdentityUserRecord,
} from "@generic-identity/contracts";

export interface GenericIdentityDirectoryUsersClient {
  list(context: IdentityAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityUserRecord[]>;
  get(context: IdentityAdministrationContext, userId: string, signal?: AbortSignal): Promise<IdentityUserRecord | null>;
  create(context: IdentityAdministrationContext, request: IdentityCreateUserRequest, signal?: AbortSignal): Promise<IdentityUserRecord>;
  update(context: IdentityAdministrationContext, userId: string, request: IdentityUpdateUserRequest, signal?: AbortSignal): Promise<IdentityUserRecord>;
}

export interface GenericIdentityDirectoryTenantsClient {
  list(context: IdentityAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityTenantRecord[]>;
  get(context: IdentityAdministrationContext, tenantId: string, signal?: AbortSignal): Promise<IdentityTenantRecord | null>;
  create(context: IdentityAdministrationContext, request: IdentityCreateTenantRequest, signal?: AbortSignal): Promise<IdentityTenantRecord>;
  update(context: IdentityAdministrationContext, tenantId: string, request: IdentityUpdateTenantRequest, signal?: AbortSignal): Promise<IdentityTenantRecord>;
}

export interface GenericIdentityDirectoryTenantUsersClient {
  list(context: IdentityTenantAdministrationContext, options?: IdentityTenantUserListOptions, signal?: AbortSignal): Promise<readonly IdentityTenantUserRecord[]>;
}

export interface GenericIdentityDirectoryMembershipsClient {
  list(context: IdentityTenantAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityTenantMembershipRecord[]>;
  get(context: IdentityTenantAdministrationContext, membershipId: string, signal?: AbortSignal): Promise<IdentityTenantMembershipRecord | null>;
  findByUser(context: IdentityTenantAdministrationContext, userId: string, signal?: AbortSignal): Promise<IdentityTenantMembershipRecord | null>;
  create(context: IdentityTenantAdministrationContext, request: IdentityCreateTenantMembershipRequest, signal?: AbortSignal): Promise<IdentityTenantMembershipRecord>;
  update(context: IdentityTenantAdministrationContext, membershipId: string, request: IdentityUpdateTenantMembershipRequest, signal?: AbortSignal): Promise<IdentityTenantMembershipRecord>;
}

export interface GenericIdentityDirectoryMembershipCandidatesClient {
  findByLogin(context: IdentityTenantAdministrationContext, loginIdentifier: string, signal?: AbortSignal): Promise<IdentityTenantMembershipCandidateRecord | null>;
  createMembershipByLogin(context: IdentityTenantAdministrationContext, loginIdentifier: string, status?: IdentityMembershipStatus, signal?: AbortSignal): Promise<IdentityTenantMembershipRecord>;
}

export interface GenericIdentityDirectoryTenantGroupAssignmentsClient {
  list(context: IdentityTenantAdministrationContext, signal?: AbortSignal): Promise<readonly IdentityTenantGroupAssignmentRecord[]>;
}

export interface GenericIdentityDirectoryClient {
  readonly users: GenericIdentityDirectoryUsersClient;
  readonly tenants: GenericIdentityDirectoryTenantsClient;
  readonly tenantUsers: GenericIdentityDirectoryTenantUsersClient;
  readonly memberships: GenericIdentityDirectoryMembershipsClient;
  readonly membershipCandidates: GenericIdentityDirectoryMembershipCandidatesClient;
  readonly tenantGroupAssignments: GenericIdentityDirectoryTenantGroupAssignmentsClient;
}
