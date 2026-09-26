import "server-only";
import type { IdentityTenantAdministrationContext, IdentityTenantUserRecord, IdentityUserRecord } from "@identity-access/client";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";

export interface IdentityAccessAdminUserReadResult {
  readonly users: readonly IdentityUserRecord[];
  readonly selectedUser: IdentityUserRecord | null;
}

/** Reads either the authorized identity-scope directory or a tenant-membership-constrained user projection. */
export class IdentityAccessAdminUserReadService {
  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async load(
    context: IdentityTenantAdministrationContext | undefined,
    userId?: string,
  ): Promise<IdentityAccessAdminUserReadResult> {
    if (this.#request.effectiveContext.tenantVisibility === "scope-wide") {
      const users = await this.#request.client.administration.users.list(this.#request.administrationContext, { limit: 50 });
      const selectedUser = userId
        ? users.find((user) => user.userId === userId)
          ?? await this.#request.client.administration.users.get(this.#request.administrationContext, userId)
        : null;
      return { users, selectedUser };
    }

    if (context === undefined) return { users: [], selectedUser: null };

    const tenantUsers = await this.#request.client.administration.tenantUsers.list(context, { limit: 50 });
    const users = tenantUsers.map(IdentityAccessAdminUserReadService.#mapTenantUser);
    if (!userId) return { users, selectedUser: null };

    const loaded = users.find((user) => user.userId === userId);
    if (loaded) return { users, selectedUser: loaded };

    const matches = await this.#request.client.administration.tenantUsers.list(context, { search: userId, limit: 20 });
    const match = matches.find((record) => record.userId === userId);
    return { users, selectedUser: match ? IdentityAccessAdminUserReadService.#mapTenantUser(match) : null };
  }

  static #mapTenantUser(record: IdentityTenantUserRecord): IdentityUserRecord {
    return {
      userId: record.userId,
      displayName: record.displayName,
      status: record.userStatus,
      version: record.userVersion,
    };
  }
}
