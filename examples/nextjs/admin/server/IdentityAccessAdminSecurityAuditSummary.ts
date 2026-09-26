import type { IdentitySecurityAuditRecord } from "@identity-access/client";

/** Computes bounded-window audit metrics without owning data access or formatting. */
export class IdentityAccessAdminSecurityAuditSummary {
  public readonly total: number;
  public readonly succeeded: number;
  public readonly denied: number;
  public readonly failed: number;

  public constructor(records: readonly IdentitySecurityAuditRecord[]) {
    this.total = records.length;
    this.succeeded = records.filter((record) => record.outcome === "Succeeded").length;
    this.denied = records.filter((record) => record.outcome === "Denied").length;
    this.failed = records.filter((record) => record.outcome === "Failed").length;
  }
}
