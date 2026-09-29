import "server-only";
import type {
  IdentityOrganizationMembershipRecord,
  IdentityOrganizationRecord,
  IdentityOrganizationResourceScopeLinkRecord,
} from "@identity-access/client";
import type { IdentityAccessAdminSelectedMembershipSummary } from "./IdentityAccessAdminMembershipOverviewService";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";

export interface IdentityAccessAdminOrganizationPermissions {
  readonly canReadOrganizations: boolean;
  readonly canManageOrganizations: boolean;
  readonly canReadOrganizationMemberships: boolean;
  readonly canManageOrganizationMemberships: boolean;
  readonly canReadOrganizationScopeLinks: boolean;
  readonly canManageOrganizationScopeLinks: boolean;
  readonly canReadResourceScopes: boolean;
}

export interface IdentityAccessAdminOrganizationOverview {
  readonly organizations: readonly IdentityOrganizationRecord[];
  readonly selectedMemberOrganizationMemberships:
    readonly IdentityOrganizationMembershipRecord[];
  readonly selectedOrganization: IdentityOrganizationRecord | null;
  readonly selectedOrganizationScopeLink:
    IdentityOrganizationResourceScopeLinkRecord | null;
  readonly permissions: IdentityAccessAdminOrganizationPermissions;
}

const noPermissions: IdentityAccessAdminOrganizationPermissions = {
  canReadOrganizations: false,
  canManageOrganizations: false,
  canReadOrganizationMemberships: false,
  canManageOrganizationMemberships: false,
  canReadOrganizationScopeLinks: false,
  canManageOrganizationScopeLinks: false,
  canReadResourceScopes: false,
};

/**
 * Composes only Organization Directory data for the existing Identity Membership workspace.
 * It does not own tenant-member/group loading and does not create another navigation root.
 */
export class IdentityAccessAdminOrganizationOverviewService {
  static readonly #organizationLimit = 200;

  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async load(
    tenantId: string,
    selectedMembership: IdentityAccessAdminSelectedMembershipSummary | null,
    requestedOrganizationId?: string,
  ): Promise<IdentityAccessAdminOrganizationOverview> {
    const context = this.#request.tenantContextFor(tenantId);
    const authorization = this.#request.tenantAuthorizationFor(tenantId);

    const [
      canReadOrganizations,
      canManageOrganizations,
      canReadOrganizationMemberships,
      canManageOrganizationMemberships,
      canReadOrganizationScopeLinks,
      canManageOrganizationScopeLinks,
      canReadResourceScopes,
    ] = await Promise.all([
      authorization.isAllowed("identity-access", "organization", "read"),
      authorization.isAllowed("identity-access", "organization", "write"),
      authorization.isAllowed(
        "identity-access",
        "organization-membership",
        "read",
      ),
      authorization.isAllowed(
        "identity-access",
        "organization-membership",
        "write",
      ),
      authorization.isAllowed(
        "identity-access",
        "organization-scope-link",
        "read",
      ),
      authorization.isAllowed(
        "identity-access",
        "organization-scope-link",
        "write",
      ),
      authorization.isAllowed("identity-access", "resource-scope", "read"),
    ]);

    const permissions: IdentityAccessAdminOrganizationPermissions = {
      canReadOrganizations,
      canManageOrganizations,
      canReadOrganizationMemberships,
      canManageOrganizationMemberships,
      canReadOrganizationScopeLinks,
      canManageOrganizationScopeLinks,
      canReadResourceScopes,
    };

    if (!canReadOrganizations) {
      return {
        ...IdentityAccessAdminOrganizationOverviewService.empty(),
        permissions,
      };
    }

    const organizations =
      await this.#request.client.administration.organizations.list(context, {
        limit:
          IdentityAccessAdminOrganizationOverviewService.#organizationLimit,
      });

    const requestedOrganization =
      requestedOrganizationId?.trim().toLowerCase();

    const selectedOrganization = requestedOrganization
      ? organizations.find(
          (organization) =>
            organization.organizationId === requestedOrganization,
        ) ?? null
      : null;

    if (requestedOrganization && selectedOrganization === null) {
      throw new Error(
        "The requested organization is outside the loaded authorized tenant organization collection.",
      );
    }

    const [
      selectedMemberOrganizationMemberships,
      selectedOrganizationScopeLink,
    ] = await Promise.all([
      selectedMembership && canReadOrganizationMemberships
        ? this.#request.client.administration.organizationMemberships.listForTenantMembership(
            context,
            selectedMembership.membershipId,
            {
              limit:
                IdentityAccessAdminOrganizationOverviewService.#organizationLimit,
            },
          )
        : Promise.resolve([]),
      selectedOrganization && canReadOrganizationScopeLinks
        ? this.#request.client.administration.organizationResourceScopeLinks.get(
            context,
            selectedOrganization.organizationId,
          )
        : Promise.resolve(null),
    ]);

    return {
      organizations,
      selectedMemberOrganizationMemberships,
      selectedOrganization,
      selectedOrganizationScopeLink,
      permissions,
    };
  }

  public static empty(): IdentityAccessAdminOrganizationOverview {
    return {
      organizations: [],
      selectedMemberOrganizationMemberships: [],
      selectedOrganization: null,
      selectedOrganizationScopeLink: null,
      permissions: noPermissions,
    };
  }
}
