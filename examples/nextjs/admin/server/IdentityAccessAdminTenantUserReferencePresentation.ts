import type { IdentityTenantUserRecord } from "@identity-access/client";
import type { AdminEntityReferenceOption } from "../contracts/AdminEntityReferenceOption";

/** Maps tenant-constrained user projections to the shared autocomplete model. */
export class IdentityAccessAdminTenantUserReferencePresentation {
  public static tenantUsers(
    records: readonly IdentityTenantUserRecord[],
  ): readonly AdminEntityReferenceOption[] {
    return records.map((record) => ({
      id: record.membershipId,
      displayName: record.displayName,
      description: `Tenant membership · ${record.userId}`,
      keywords: [record.userId],
    }));
  }
}
