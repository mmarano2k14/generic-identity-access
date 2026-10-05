import type { IdentityTenantAdministrationContext } from "@identity-access/client";
import type {
  IdentityAdministrationListOptions,
  IdentityCreateOrganizationRequest,
  IdentityOrganizationMembershipRecord,
  IdentityOrganizationRecord,
  IdentityOrganizationResourceScopeLinkRecord,
  IdentityOrganizationTreeNodeRecord,
  IdentityUpdateOrganizationRequest,
} from "@generic-identity/contracts/organizations";

/** CRUD, hierarchy and lifecycle operations for tenant-local Organizations. */
export interface GenericIdentityOrganizationRecordsClient {
  list(context: IdentityTenantAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityOrganizationRecord[]>;
  get(context: IdentityTenantAdministrationContext, organizationId: string, signal?: AbortSignal): Promise<IdentityOrganizationRecord | null>;
  tree(context: IdentityTenantAdministrationContext, signal?: AbortSignal): Promise<readonly IdentityOrganizationTreeNodeRecord[]>;
  children(context: IdentityTenantAdministrationContext, organizationId: string, signal?: AbortSignal): Promise<readonly IdentityOrganizationRecord[]>;
  create(context: IdentityTenantAdministrationContext, request: IdentityCreateOrganizationRequest, signal?: AbortSignal): Promise<IdentityOrganizationRecord>;
  update(context: IdentityTenantAdministrationContext, organizationId: string, request: IdentityUpdateOrganizationRequest, signal?: AbortSignal): Promise<IdentityOrganizationRecord>;
  enable(context: IdentityTenantAdministrationContext, organizationId: string, expectedRowVersion: number, signal?: AbortSignal): Promise<IdentityOrganizationRecord>;
  disable(context: IdentityTenantAdministrationContext, organizationId: string, expectedRowVersion: number, signal?: AbortSignal): Promise<IdentityOrganizationRecord>;
}

/** Explicit organization belonging operations. Belonging never grants authorization by itself. */
export interface GenericIdentityOrganizationMembershipsClient {
  listForOrganization(context: IdentityTenantAdministrationContext, organizationId: string, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityOrganizationMembershipRecord[]>;
  listForTenantMembership(context: IdentityTenantAdministrationContext, tenantMembershipId: string, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityOrganizationMembershipRecord[]>;
  get(context: IdentityTenantAdministrationContext, organizationId: string, tenantMembershipId: string, signal?: AbortSignal): Promise<IdentityOrganizationMembershipRecord | null>;
  add(context: IdentityTenantAdministrationContext, organizationId: string, tenantMembershipId: string, signal?: AbortSignal): Promise<IdentityOrganizationMembershipRecord>;
  activate(context: IdentityTenantAdministrationContext, organizationId: string, tenantMembershipId: string, expectedRowVersion: number, signal?: AbortSignal): Promise<IdentityOrganizationMembershipRecord>;
  suspend(context: IdentityTenantAdministrationContext, organizationId: string, tenantMembershipId: string, expectedRowVersion: number, signal?: AbortSignal): Promise<IdentityOrganizationMembershipRecord>;
  remove(context: IdentityTenantAdministrationContext, organizationId: string, tenantMembershipId: string, expectedRowVersion: number, signal?: AbortSignal): Promise<boolean>;
}

/** Application-aware ResourceScope linkage for an Organization. */
export interface GenericIdentityOrganizationResourceScopeLinksClient {
  get(context: IdentityTenantAdministrationContext, organizationId: string, signal?: AbortSignal): Promise<IdentityOrganizationResourceScopeLinkRecord | null>;
  create(context: IdentityTenantAdministrationContext, organizationId: string, resourceScopeId: string, signal?: AbortSignal): Promise<IdentityOrganizationResourceScopeLinkRecord>;
  update(context: IdentityTenantAdministrationContext, organizationId: string, resourceScopeId: string, expectedRowVersion: number, signal?: AbortSignal): Promise<IdentityOrganizationResourceScopeLinkRecord>;
  remove(context: IdentityTenantAdministrationContext, organizationId: string, expectedRowVersion: number, signal?: AbortSignal): Promise<boolean>;
}

/** Complete Generic Organization Directory SDK category. */
export interface GenericIdentityOrganizationsClient {
  readonly organizations: GenericIdentityOrganizationRecordsClient;
  readonly memberships: GenericIdentityOrganizationMembershipsClient;
  readonly resourceScopeLinks: GenericIdentityOrganizationResourceScopeLinksClient;
}
