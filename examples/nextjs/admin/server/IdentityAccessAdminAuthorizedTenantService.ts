import "server-only";
import type { IdentityTenantAdministrationContext } from "@identity-access/client";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";

export interface IdentityAccessAdminAuthorizedTenant {
  readonly tenantId: string;
  readonly displayName?: string;
  readonly context: IdentityTenantAdministrationContext;
}

/** Resolves the bounded tenant targets that may participate in an aggregate administration read. */
export class IdentityAccessAdminAuthorizedTenantService {
  static readonly #pageSize = 200;
  static readonly #maximumAggregateTenants = 1000;

  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async list(): Promise<readonly IdentityAccessAdminAuthorizedTenant[]> {
    if (this.#request.effectiveContext.tenantVisibility === "membership-limited") {
      const unique = new Map<string, IdentityAccessAdminAuthorizedTenant>();
      for (const membership of this.#request.effectiveContext.activeTenantMemberships) {
        if (unique.has(membership.tenantId)) continue;
        unique.set(membership.tenantId, {
          tenantId: membership.tenantId,
          context: this.#request.tenantContextFor(membership.tenantId),
        });
      }
      return [...unique.values()].sort((left, right) => left.tenantId.localeCompare(right.tenantId));
    }

    const targets: IdentityAccessAdminAuthorizedTenant[] = [];
    for (let offset = 0; offset < IdentityAccessAdminAuthorizedTenantService.#maximumAggregateTenants; offset += IdentityAccessAdminAuthorizedTenantService.#pageSize) {
      const tenants = await this.#request.client.administration.tenants.list(this.#request.administrationContext, {
        offset,
        limit: IdentityAccessAdminAuthorizedTenantService.#pageSize,
      });
      for (const tenant of tenants) {
        targets.push({
          tenantId: tenant.tenantId,
          displayName: tenant.displayName,
          context: this.#request.tenantContextFor(tenant.tenantId),
        });
      }
      if (tenants.length < IdentityAccessAdminAuthorizedTenantService.#pageSize) {
        return targets.sort(IdentityAccessAdminAuthorizedTenantService.#compare);
      }
    }

    const overflow = await this.#request.client.administration.tenants.list(this.#request.administrationContext, {
      offset: IdentityAccessAdminAuthorizedTenantService.#maximumAggregateTenants,
      limit: 1,
    });
    if (overflow.length > 0) {
      throw new Error("The aggregate tenant view exceeds the supported administration UI boundary. Select a tenant explicitly.");
    }
    return targets.sort(IdentityAccessAdminAuthorizedTenantService.#compare);
  }

  static #compare(left: IdentityAccessAdminAuthorizedTenant, right: IdentityAccessAdminAuthorizedTenant): number {
    return (left.displayName ?? left.tenantId).localeCompare(right.displayName ?? right.tenantId)
      || left.tenantId.localeCompare(right.tenantId);
  }
}
