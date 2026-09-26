import "server-only";
import { IdentityAccessClientError, type IdentitySecurityAuditRecord } from "@identity-access/client";
import type { AdminActionFailure } from "../contracts/AdminActionState";
import { IdentityAccessAdminFailurePresentation } from "./IdentityAccessAdminFailurePresentation";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";
import { IdentityAccessAdminSessionQuery } from "./IdentityAccessAdminSessionQuery";

export type IdentityAccessAdminSessionEvidenceState = "available" | "forbidden" | "unavailable";

export interface IdentityAccessAdminSessionActivity {
  readonly evidenceState: IdentityAccessAdminSessionEvidenceState;
  readonly records: readonly IdentitySecurityAuditRecord[];
  readonly failure?: AdminActionFailure;
}

/** Loads bounded session/security activity through the existing read-only security-audit contract. */
export class IdentityAccessAdminSessionService {
  static readonly #eventTypes = Object.freeze([
    "PasswordLoginSucceeded",
    "SessionRevoked",
    "SessionRevocationFailed",
    "UserSessionsRevoked",
    "ClientSessionsRevoked",
    "SessionAssuranceUpgraded",
    "SessionAssuranceUpgradeFailed",
    "OidcRefreshTokenReuseDetected",
    "OidcRefreshTokenFamilyRevoked",
  ] as const);

  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async listActivity(query: IdentityAccessAdminSessionQuery): Promise<IdentityAccessAdminSessionActivity> {
    if (query.validationMessage !== undefined) return { evidenceState: "available", records: [] };

    try {
      const windows = await Promise.all(
        IdentityAccessAdminSessionService.#eventTypes.map((eventType) =>
          this.#request.client.administration.securityAudit.list(
            this.#request.administrationContext,
            query.toAuditQuery(eventType),
          ),
        ),
      );

      const records = windows
        .flat()
        .filter((record) => query.matches(record))
        .sort((left, right) => Date.parse(right.occurredAt) - Date.parse(left.occurredAt))
        .slice(0, query.limit);

      return { evidenceState: "available", records };
    } catch (error) {
      if (error instanceof IdentityAccessClientError) {
        if (error.code === "forbidden") return { evidenceState: "forbidden", records: [] };
        if (error.code === "unauthenticated" || error.code === "cancelled") throw error;
      }

      return {
        evidenceState: "unavailable",
        records: [],
        failure: IdentityAccessAdminFailurePresentation.fromRead(error),
      };
    }
  }
}
