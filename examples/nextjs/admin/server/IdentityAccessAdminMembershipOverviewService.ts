import "server-only";
import type {
  IdentityGroupRecord,
  IdentityTenantGroupAssignmentRecord,
  IdentityTenantMembershipRecord,
  IdentityTenantUserRecord,
} from "@identity-access/client";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";

export interface IdentityAccessAdminMembershipTenantSummary {
  readonly tenantId: string;
  readonly displayName?: string;
  readonly status?: 1 | 2;
  readonly version?: number;
  readonly memberCount?: number;
  readonly ownMembershipId?: string;
}

export interface IdentityAccessAdminSelectedMembershipSummary {
  readonly membershipId: string;
  readonly userId: string;
  readonly displayName: string;
}

export interface IdentityAccessAdminMembershipPermissions {
  readonly canListMembers: boolean;
  readonly canAddMembers: boolean;
  readonly canReadGroups: boolean;
  readonly canCreateTenantGroups: boolean;
  readonly canReadGroupMemberships: boolean;
  readonly canManageGroupMemberships: boolean;
}

export interface IdentityAccessAdminMembershipOverview {
  readonly scopeWide: boolean;
  readonly tenants: readonly IdentityAccessAdminMembershipTenantSummary[];
  readonly selectedTenant: IdentityAccessAdminMembershipTenantSummary | null;
  readonly members: readonly IdentityTenantUserRecord[];
  readonly ownMembership: IdentityTenantMembershipRecord | null;
  readonly groups: readonly IdentityGroupRecord[];
  readonly groupAssignments: readonly IdentityTenantGroupAssignmentRecord[];
  readonly selectedMembership: IdentityAccessAdminSelectedMembershipSummary | null;
  readonly permissions: IdentityAccessAdminMembershipPermissions;
}

const noPermissions: IdentityAccessAdminMembershipPermissions = {
  canListMembers: false,
  canAddMembers: false,
  canReadGroups: false,
  canCreateTenantGroups: false,
  canReadGroupMemberships: false,
  canManageGroupMemberships: false,
};

/** Composes only tenant membership and group data for the Memberships workspace. */
export class IdentityAccessAdminMembershipOverviewService {
  static readonly #tenantLimit = 50;
  static readonly #membershipPageSize = 200;
  static readonly #groupLimit = 100;
  static readonly #maximumCountedMemberships = 10000;

  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async load(
    requestedTenantId?: string,
    requestedMembershipId?: string,
  ): Promise<IdentityAccessAdminMembershipOverview> {
    if (this.#request.effectiveContext.tenantVisibility === "scope-wide") {
      return this.#loadScopeWide(requestedTenantId, requestedMembershipId);
    }

    return this.#loadMembershipLimited(requestedTenantId, requestedMembershipId);
  }

  async #loadScopeWide(
    requestedTenantId?: string,
    requestedMembershipId?: string,
  ): Promise<IdentityAccessAdminMembershipOverview> {
    const tenants = await this.#request.client.administration.tenants.list(
      this.#request.administrationContext,
      { limit: IdentityAccessAdminMembershipOverviewService.#tenantLimit },
    );

    const summaries = await Promise.all(
      tenants.map(async (tenant) => ({
        tenantId: tenant.tenantId,
        displayName: tenant.displayName,
        status: tenant.status,
        version: tenant.version,
        memberCount: await this.#countMemberships(
          this.#request.tenantContextFor(tenant.tenantId),
        ),
      })),
    );

    const requested = requestedTenantId?.trim().toLowerCase();
    const selectedTenant = requested
      ? summaries.find((tenant) => tenant.tenantId === requested) ?? null
      : null;

    if (requested && selectedTenant === null) {
      throw new Error(
        "The requested tenant is outside the loaded authorized tenant collection.",
      );
    }

    if (!selectedTenant) {
      return IdentityAccessAdminMembershipOverviewService.#empty(true, summaries);
    }

    const detail = await this.#loadTenantDetail(
      selectedTenant.tenantId,
      false,
      requestedMembershipId,
    );

    return {
      scopeWide: true,
      tenants: summaries,
      selectedTenant,
      ...detail,
    };
  }

  async #loadMembershipLimited(
    requestedTenantId?: string,
    requestedMembershipId?: string,
  ): Promise<IdentityAccessAdminMembershipOverview> {
    const activeMemberships =
      this.#request.effectiveContext.activeTenantMemberships;
    const requested = requestedTenantId?.trim().toLowerCase();

    const selectedReference = requested
      ? activeMemberships.find(
          (membership) => membership.tenantId === requested,
        )
      : activeMemberships.length === 1
        ? activeMemberships[0]
        : undefined;

    const tenants: IdentityAccessAdminMembershipTenantSummary[] =
      activeMemberships.map((membership) => ({
        tenantId: membership.tenantId,
        ownMembershipId: membership.membershipId,
      }));

    if (!selectedReference) {
      return IdentityAccessAdminMembershipOverviewService.#empty(false, tenants);
    }

    const detail = await this.#loadTenantDetail(
      selectedReference.tenantId,
      true,
      requestedMembershipId,
    );

    return {
      scopeWide: false,
      tenants,
      selectedTenant: {
        tenantId: selectedReference.tenantId,
        ownMembershipId: selectedReference.membershipId,
      },
      ...detail,
    };
  }

  async #loadTenantDetail(
    tenantId: string,
    membershipLimited: boolean,
    requestedMembershipId?: string,
  ) {
    const context = this.#request.tenantContextFor(tenantId);
    const authorization = this.#request.tenantAuthorizationFor(tenantId);

    const [
      canListMembers,
      canAddMembers,
      canReadGroups,
      canCreateTenantGroups,
      canReadGroupMemberships,
      canManageGroupMemberships,
    ] = await Promise.all([
      authorization.isAllowed(
        "identity-access",
        "tenant-membership",
        "read",
      ),
      authorization.isAllowed(
        "identity-access",
        "tenant-membership",
        "write",
      ),
      authorization.isAllowed("identity-access", "group", "read"),
      authorization.isAllowed("identity-access", "group", "write"),
      authorization.isAllowed(
        "identity-access",
        "group-membership",
        "read",
      ),
      authorization.isAllowed(
        "identity-access",
        "group-membership",
        "write",
      ),
    ]);

    const permissions: IdentityAccessAdminMembershipPermissions = {
      canListMembers,
      canAddMembers,
      canReadGroups,
      canCreateTenantGroups,
      canReadGroupMemberships,
      canManageGroupMemberships,
    };

    const ownMembership = membershipLimited
      ? await this.#request.client.administration.memberships.findByUser(
          context,
          this.#request.effectiveContext.userId,
        )
      : null;

    const [members, groups, groupAssignments] = await Promise.all([
      canListMembers
        ? this.#request.client.administration.tenantUsers.list(context, {
            limit:
              IdentityAccessAdminMembershipOverviewService.#membershipPageSize,
          })
        : Promise.resolve([]),
      canReadGroups
        ? this.#request.client.administration.groups.list(context, {
            limit: IdentityAccessAdminMembershipOverviewService.#groupLimit,
          })
        : Promise.resolve([]),
      canReadGroupMemberships
        ? this.#request.client.administration.tenantGroupAssignments.list(
            context,
          )
        : Promise.resolve([]),
    ]);

    const selectedMembership =
      IdentityAccessAdminMembershipOverviewService.#resolveSelectedMembership(
        requestedMembershipId,
        members,
        ownMembership,
      );

    return {
      members,
      ownMembership,
      groups,
      groupAssignments,
      selectedMembership,
      permissions,
    };
  }

  async #countMemberships(
    context: import("@identity-access/client").IdentityTenantAdministrationContext,
  ): Promise<number> {
    let count = 0;

    for (
      let offset = 0;
      offset <
      IdentityAccessAdminMembershipOverviewService.#maximumCountedMemberships;
      offset += IdentityAccessAdminMembershipOverviewService.#membershipPageSize
    ) {
      const page =
        await this.#request.client.administration.memberships.list(context, {
          offset,
          limit:
            IdentityAccessAdminMembershipOverviewService.#membershipPageSize,
        });

      count += page.length;

      if (
        page.length <
        IdentityAccessAdminMembershipOverviewService.#membershipPageSize
      ) {
        return count;
      }
    }

    throw new Error(
      "Tenant membership count exceeds the supported administration UI boundary.",
    );
  }

  static #resolveSelectedMembership(
    requestedMembershipId: string | undefined,
    members: readonly IdentityTenantUserRecord[],
    ownMembership: IdentityTenantMembershipRecord | null,
  ): IdentityAccessAdminSelectedMembershipSummary | null {
    const requested = requestedMembershipId?.trim().toLowerCase();
    if (!requested) return null;

    const member = members.find(
      (candidate) => candidate.membershipId === requested,
    );

    if (member) {
      return {
        membershipId: member.membershipId,
        userId: member.userId,
        displayName: member.displayName,
      };
    }

    if (ownMembership?.membershipId === requested) {
      return {
        membershipId: ownMembership.membershipId,
        userId: ownMembership.userId,
        displayName: "Current user",
      };
    }

    throw new Error(
      "The requested membership is outside the loaded authorized tenant membership collection.",
    );
  }

  static #empty(
    scopeWide: boolean,
    tenants: readonly IdentityAccessAdminMembershipTenantSummary[],
  ): IdentityAccessAdminMembershipOverview {
    return {
      scopeWide,
      tenants,
      selectedTenant: null,
      members: [],
      ownMembership: null,
      groups: [],
      groupAssignments: [],
      selectedMembership: null,
      permissions: noPermissions,
    };
  }
}
