import type { IdentitySecurityAuditRecord } from "@identity-access/client";

/** Computes bounded session-security metrics without owning data access or formatting. */
export class IdentityAccessAdminSessionSummary {
  public readonly total: number;
  public readonly issued: number;
  public readonly revocationEvents: number;
  public readonly assuranceEvents: number;
  public readonly continuityAlerts: number;

  public constructor(records: readonly IdentitySecurityAuditRecord[]) {
    this.total = records.length;
    this.issued = records.filter((record) => record.eventType === "PasswordLoginSucceeded").length;
    this.revocationEvents = records.filter((record) => record.eventType.includes("Revoked") || record.eventType.includes("Revocation")).length;
    this.assuranceEvents = records.filter((record) => record.eventType.includes("Assurance")).length;
    this.continuityAlerts = records.filter((record) => record.eventType === "OidcRefreshTokenReuseDetected").length;
  }
}
