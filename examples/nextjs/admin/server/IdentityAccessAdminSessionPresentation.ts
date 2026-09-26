import type { IdentitySecurityAuditOutcome, IdentitySecurityAuditRecord } from "@identity-access/client";
import { IdentityAccessAdminSessionSummary } from "./IdentityAccessAdminSessionSummary";

/** Presentation-only mapping for bounded session/security evidence. */
export class IdentityAccessAdminSessionPresentation {
  public static eventLabel(eventType: string): string {
    switch (eventType) {
      case "PasswordLoginSucceeded": return "Session issued";
      case "SessionRevoked": return "Session revoked";
      case "SessionRevocationFailed": return "Session revocation rejected";
      case "UserSessionsRevoked": return "User sessions contained";
      case "ClientSessionsRevoked": return "Client sessions contained";
      case "SessionAssuranceUpgraded": return "Session assurance upgraded";
      case "SessionAssuranceUpgradeFailed": return "Session assurance upgrade rejected";
      case "OidcRefreshTokenReuseDetected": return "Refresh-token reuse detected";
      case "OidcRefreshTokenFamilyRevoked": return "Refresh-token family revoked";
      default: return eventType.replace(/([a-z0-9])([A-Z])/gu, "$1 $2");
    }
  }

  public static category(eventType: string): string {
    if (eventType === "PasswordLoginSucceeded") return "Issuance";
    if (eventType.includes("Assurance")) return "Assurance";
    if (eventType.startsWith("OidcRefreshToken")) return "Token continuity";
    return "Revocation";
  }

  public static outcomeClassName(outcome: IdentitySecurityAuditOutcome): string {
    return outcome === "Succeeded"
      ? "ia-audit-outcome ia-audit-outcome-success"
      : outcome === "Denied"
        ? "ia-audit-outcome ia-audit-outcome-denied"
        : "ia-audit-outcome ia-audit-outcome-failed";
  }

  public static targetLabel(eventType: string): string {
    if (eventType === "UserSessionsRevoked") return "User target";
    if (eventType === "ClientSessionsRevoked") return "Client target";
    if (eventType.startsWith("OidcRefreshToken")) return "Token reference";
    return "Session reference";
  }

  public static summary(records: readonly IdentitySecurityAuditRecord[]): IdentityAccessAdminSessionSummary {
    return new IdentityAccessAdminSessionSummary(records);
  }
}
