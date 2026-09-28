import "server-only";
import type {
  IdentityGroupRecord,
  IdentityTenantGroupAssignmentRecord,
  IdentityTenantMembershipRecord,
  IdentityTenantRecord,
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

/**
 * Composes the tenant-centric membership administration read model on the server.
 * Tenant collection visibility comes from the trusted effective context. Directory and
 * group detail are additionally capability-gated before their protected APIs are called.
 */
export class IdentityAccessAdminMembershipOverviewService {
  static readonly #tenantLimit = 50;
  static readonly #membershipPageSize = 200;
  static readonly #groupLimit = 100;
  static readonly #maximumCountedMemberships = 10000;

  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async load(requestedTenantId?: string): Promise<IdentityAccessAdminMembershipOverview> {
    if (this.#request.effectiveContext.tenantVisibility === "scope-wide") {
      return this.#loadScopeWide(requestedTenantId);
    }

    return this.#loadMembershipLimited(requestedTenantId);
  }

  async #loadScopeWide(requestedTenantId?: string): Promise<IdentityAccessAdminMembershipOverview> {
    const tenants = await this.#request.client.administration.tenants.list(
      this.#request.administrationContext,
      { limit: IdentityAccessAdminMembershipOverviewService.#tenantLimit },
    );

    const summaries = await Promise.all(tenants.map(async (tenant) => ({
      tenantId: tenant.tenantId,
      displayName: tenant.displayName,
      status: tenant.status,
      version: tenant.version,
      memberCount: await this.#countMemberships(this.#request.tenantContextFor(tenant.tenantId)),
    })));

    const requested = requestedTenantId?.trim().toLowerCase();
    const selectedTenant = requested
      ? summaries.find((tenant) => tenant.tenantId === requested) ?? null
      : null;

    if (requested && selectedTenant === null) {
      throw new Error("The requested tenant is outside the loaded authorized tenant collection.");
    }

    if (!selectedTenant) {
      return {
        scopeWide: true,
        tenants: summaries,
        selectedTenant: null,
        members: [],
        ownMembership: null,
        groups: [],
        groupAssignments: [],
        permissions: noPermissions,
      };
    }

    const detail = await this.#loadTenantDetail(selectedTenant.tenantId, false);
    return {
      scopeWide: true,
      tenants: summaries,
      selectedTenant,
      ...detail,
    };
  }

  async #loadMembershipLimited(requestedTenantId?: string): Promise<IdentityAccessAdminMembershipOverview> {
    const activeMemberships = this.#request.effectiveContext.activeTenantMemberships;
    const requested = requestedTenantId?.trim().toLowerCase();
    const selectedReference = requested
      ? activeMemberships.find((membership) => membership.tenantId === requested)
      : activeMemberships.length === 1
        ? activeMemberships[0]
        : undefined;

    const tenants: IdentityAccessAdminMembershipTenantSummary[] = activeMemberships.map((membership) => ({
      tenantId: membership.tenantId,
      ownMembershipId: membership.membershipId,
    }));

    if (!selectedReference) {
      return {
        scopeWide: false,
        tenants,
        selectedTenant: null,
        members: [],
        ownMembership: null,
        groups: [],
        groupAssignments: [],
        permissions: noPermissions,
      };
    }

    const detail = await this.#loadTenantDetail(selectedReference.tenantId, true);
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

  async #loadTenantDetail(tenantId: string, membershipLimited: boolean) {
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
      authorization.isAllowed("identity-access", "tenant-membership", "read"),
      authorization.isAllowed("identity-access", "tenant-membership", "write"),
      authorization.isAllowed("identity-access", "group", "read"),
      authorization.isAllowed("identity-access", "group", "write"),
      authorization.isAllowed("identity-access", "group-membership", "read"),
      authorization.isAllowed("identity-access", "group-membership", "write"),
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
        ? this.#request.client.administration.tenantUsers.list(
            context,
            { limit: IdentityAccessAdminMembershipOverviewService.#membershipPageSize },
          )
        : Promise.resolve([]),
      canReadGroups
        ? this.#request.client.administration.groups.list(
            context,
            { limit: IdentityAccessAdminMembershipOverviewService.#groupLimit },
          )
        : Promise.resolve([]),
      canReadGroupMemberships
        ? this.#request.client.administration.tenantGroupAssignments.list(context)
        : Promise.resolve([]),
    ]);

    return {
      members,
      ownMembership,
      groups,
      groupAssignments,
      permissions,
    };
  }

  async #countMemberships(context: import("@identity-access/client").IdentityTenantAdministrationContext): Promise<number> {
    let count = 0;
    for (let offset = 0; offset < IdentityAccessAdminMembershipOverviewService.#maximumCountedMemberships; offset += IdentityAccessAdminMembershipOverviewService.#membershipPageSize) {
      const page = await this.#request.client.administration.memberships.list(context, {
        offset,
        limit: IdentityAccessAdminMembershipOverviewService.#membershipPageSize,
      });
      count += page.length;
      if (page.length < IdentityAccessAdminMembershipOverviewService.#membershipPageSize) return count;
    }

    throw new Error("Tenant membership count exceeds the supported administration UI boundary.");
  }
}
