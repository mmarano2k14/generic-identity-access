import "server-only";
import { IdentityAccessClientError } from "@identity-access/client";
import type { IdentityAccessAdminAuthorizedTenant } from "./IdentityAccessAdminAuthorizedTenantService";

export interface IdentityAccessAdminTenantAggregateRecord<T> {
  readonly tenant: IdentityAccessAdminAuthorizedTenant;
  readonly record: T;
}

/** Performs bounded tenant fan-out for read-only aggregate views while preserving technical failures. */
export class IdentityAccessAdminTenantAggregateLoader {
  static readonly #concurrency = 8;

  public static async load<T>(
    tenants: readonly IdentityAccessAdminAuthorizedTenant[],
    reader: (tenant: IdentityAccessAdminAuthorizedTenant) => Promise<readonly T[]>,
  ): Promise<readonly IdentityAccessAdminTenantAggregateRecord<T>[]> {
    const result: IdentityAccessAdminTenantAggregateRecord<T>[] = [];
    for (let offset = 0; offset < tenants.length; offset += IdentityAccessAdminTenantAggregateLoader.#concurrency) {
      const slice = tenants.slice(offset, offset + IdentityAccessAdminTenantAggregateLoader.#concurrency);
      const batches = await Promise.all(slice.map(async (tenant) => {
        try {
          const records = await reader(tenant);
          return records.map((record) => ({ tenant, record }));
        } catch (error) {
          if (error instanceof IdentityAccessClientError && error.code === "forbidden") return [];
          throw error;
        }
      }));
      for (const batch of batches) result.push(...batch);
    }
    return result;
  }
}
