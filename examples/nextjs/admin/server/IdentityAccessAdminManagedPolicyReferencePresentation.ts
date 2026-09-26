import type { IdentityManagedPolicyRecord } from "@identity-access/client";
import type { AdminEntityReferenceOption } from "../contracts/AdminEntityReferenceOption";

/** Maps attachable shared managed policies to the reusable relation-selector model. */
export class IdentityAccessAdminManagedPolicyReferencePresentation {
  public static policies(records: readonly IdentityManagedPolicyRecord[]): readonly AdminEntityReferenceOption[] {
    return records
      .filter((record) => record.status === 1 && record.defaultVersion !== undefined)
      .map((record) => ({
        id: record.policyId,
        displayName: record.displayName,
        description: `${record.policyKey} · default v${record.defaultVersion}`,
        keywords: [record.policyKey],
      }));
  }
}
