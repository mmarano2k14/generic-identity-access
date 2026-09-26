import type { IdentitySecurityAuditOutcome, IdentitySecurityAuditRecord } from "@identity-access/client";
import { IdentityAccessAdminSecurityAuditSummary } from "./IdentityAccessAdminSecurityAuditSummary";

/** Presentation-only classification for secret-safe audit metadata. */
export class IdentityAccessAdminSecurityAuditPresentation {
  public static outcomeLabel(outcome: IdentitySecurityAuditOutcome): string {
    return outcome;
  }

  public static outcomeClassName(outcome: IdentitySecurityAuditOutcome): string {
    return outcome === "Succeeded"
      ? "ia-audit-outcome ia-audit-outcome-success"
      : outcome === "Denied"
        ? "ia-audit-outcome ia-audit-outcome-denied"
        : "ia-audit-outcome ia-audit-outcome-failed";
  }

  public static eventLabel(eventType: string): string {
    return eventType.replace(/([a-z0-9])([A-Z])/gu, "$1 $2");
  }

  public static category(eventType: string): string {
    if (eventType.startsWith("Oidc")) return "OIDC";
    if (eventType.includes("Authenticator") || eventType.includes("AuthenticationFactor") || eventType.includes("Mfa")) return "MFA";
    if (eventType.includes("Password") || eventType.includes("Login")) return "Authentication";
    if (eventType.includes("Session")) return "Sessions";
    if (eventType.includes("Policy") || eventType.includes("Group") || eventType.includes("Scope") || eventType.includes("Tenant") || eventType.includes("User")) return "Administration";
    return "Security";
  }

  public static summary(records: readonly IdentitySecurityAuditRecord[]): IdentityAccessAdminSecurityAuditSummary {
    return new IdentityAccessAdminSecurityAuditSummary(records);
  }
}
