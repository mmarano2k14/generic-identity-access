import { IdentityAccessClient as LegacyIdentityAccessClient } from "@identity-access/client";
import type {
  FetchTransport as LegacyFetchTransport,
  IdentityAccessClientOptions as LegacyIdentityAccessClientOptions,
} from "@identity-access/client";
import type { GenericIdentityAuthenticationClient } from "./authentication";
import type { GenericIdentityAdministrationClient } from "./administration";
import type { GenericIdentityAuthorizationClient } from "./authorization";
import { createGenericIdentityAccountClient, type GenericIdentityAccountClient } from "./account";
import type { GenericIdentityDirectoryClient } from "./directory";
import type { GenericIdentityOrganizationsClient } from "./organizations";
import type { GenericIdentityAccessControlClient } from "./access-control";
import {
  createGenericIdentityApplicationSecurityClient,
  type GenericIdentityApplicationSecurityClient,
} from "./application-security";
import {
  createGenericIdentitySecurityOperationsClient,
  type GenericIdentitySecurityOperationsClient,
} from "./security-operations";

/** Trusted client configuration for the shared Generic Identity boundary. */
export type GenericIdentityClientOptions = LegacyIdentityAccessClientOptions;

/** Optional injected transport used by tests or controlled hosts. */
export type GenericIdentityFetchTransport = LegacyFetchTransport;

/**
 * Narrow public client surface for the shared authentication/authorization SDK.
 *
 * The proven legacy client remains the runtime implementation during this
 * additive extraction milestone. System and OIDC protocol surfaces are not advertised through this package.
 */
export interface GenericIdentityClient {
  readonly authentication: GenericIdentityAuthenticationClient;
  readonly authorization: GenericIdentityAuthorizationClient;
  readonly administration: GenericIdentityAdministrationClient;
  readonly account: GenericIdentityAccountClient;
  readonly directory: GenericIdentityDirectoryClient;
  readonly organizations: GenericIdentityOrganizationsClient;
  readonly accessControl: GenericIdentityAccessControlClient;
  readonly applicationSecurity: GenericIdentityApplicationSecurityClient;
  readonly security: GenericIdentitySecurityOperationsClient;
}

/**
 * Creates the shared Generic Identity client without introducing a second
 * authentication or authorization implementation.
 */
export function createIdentityClient(options: GenericIdentityClientOptions): GenericIdentityClient {
  const legacy = new LegacyIdentityAccessClient(options);
  return {
    authentication: legacy.authentication,
    authorization: legacy.authorization,
    administration: legacy.administration,
    account: createGenericIdentityAccountClient(legacy.authentication, legacy.administration.credentials),
    directory: {
      users: legacy.administration.users,
      tenants: legacy.administration.tenants,
      tenantUsers: legacy.administration.tenantUsers,
      memberships: legacy.administration.memberships,
      membershipCandidates: legacy.administration.membershipCandidates,
      tenantGroupAssignments: legacy.administration.tenantGroupAssignments,
    },
    organizations: {
      organizations: legacy.administration.organizations,
      memberships: legacy.administration.organizationMemberships,
      resourceScopeLinks: legacy.administration.organizationResourceScopeLinks,
    },
    applicationSecurity: createGenericIdentityApplicationSecurityClient(
      legacy.administration.securityModels,
      legacy.administration.context,
    ),
    security: createGenericIdentitySecurityOperationsClient(
      legacy.administration.sessions,
      legacy.administration.mfa,
      legacy.administration.securityAudit,
    ),
    accessControl: {
      groups: legacy.administration.groups,
      managedPolicies: legacy.administration.managedPolicies,
      managedPolicyBindings: legacy.administration.managedPolicyBindings,
      resourceScopes: legacy.administration.resourceScopes,
      delegatedAuthority: legacy.administration.scopeAuthority,
      authorization: legacy.authorization,
    },
  };
}
