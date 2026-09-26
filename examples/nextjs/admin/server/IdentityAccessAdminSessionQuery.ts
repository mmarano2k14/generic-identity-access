import type { IdentitySecurityAuditOutcome, IdentitySecurityAuditQuery, IdentitySecurityAuditRecord } from "@identity-access/client";

export interface IdentityAccessAdminSessionSearchParams {
  readonly userId?: string;
  readonly clientId?: string;
  readonly outcome?: string;
  readonly limit?: string;
}

/** Normalizes session-investigation query parameters without performing API access or authorization. */
export class IdentityAccessAdminSessionQuery {
  static readonly #uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/iu;
  public readonly userId: string | undefined;
  public readonly clientId: string | undefined;
  public readonly outcome: IdentitySecurityAuditOutcome | undefined;
  public readonly limit: number;
  public readonly validationMessage: string | undefined;

  private constructor(
    userId: string | undefined,
    clientId: string | undefined,
    outcome: IdentitySecurityAuditOutcome | undefined,
    limit: number,
    validationMessage: string | undefined,
  ) {
    this.userId = userId;
    this.clientId = clientId;
    this.outcome = outcome;
    this.limit = limit;
    this.validationMessage = validationMessage;
  }

  public static fromSearchParams(params: IdentityAccessAdminSessionSearchParams): IdentityAccessAdminSessionQuery {
    const userId = IdentityAccessAdminSessionQuery.#trim(params.userId);
    const clientId = IdentityAccessAdminSessionQuery.#trim(params.clientId);
    const outcome = IdentityAccessAdminSessionQuery.#outcome(params.outcome);
    const limit = IdentityAccessAdminSessionQuery.#limit(params.limit);

    if (userId !== undefined && !IdentityAccessAdminSessionQuery.#uuidPattern.test(userId)) {
      return new IdentityAccessAdminSessionQuery(userId, clientId, outcome, limit, "User ID must be a UUID.");
    }
    if (clientId !== undefined && clientId.length > 128) {
      return new IdentityAccessAdminSessionQuery(userId, clientId, outcome, limit, "Client ID must not exceed 128 characters.");
    }
    if (params.outcome !== undefined && params.outcome.trim().length > 0 && outcome === undefined) {
      return new IdentityAccessAdminSessionQuery(userId, clientId, undefined, limit, "Outcome filter is invalid.");
    }

    return new IdentityAccessAdminSessionQuery(userId, clientId, outcome, limit, undefined);
  }

  public toAuditQuery(eventType: string): IdentitySecurityAuditQuery {
    if (this.validationMessage !== undefined) throw new Error(this.validationMessage);
    return {
      ...(this.userId === undefined ? {} : { userId: this.userId }),
      ...(this.outcome === undefined ? {} : { outcome: this.outcome }),
      eventType,
      limit: this.limit,
    };
  }

  public matches(record: IdentitySecurityAuditRecord): boolean {
    if (this.userId !== undefined && record.userId !== this.userId) return false;
    if (this.clientId !== undefined && record.clientId !== this.clientId) return false;
    if (this.outcome !== undefined && record.outcome !== this.outcome) return false;
    return true;
  }

  public get hasFilters(): boolean {
    return this.userId !== undefined || this.clientId !== undefined || this.outcome !== undefined;
  }

  static #trim(value: string | undefined): string | undefined {
    const normalized = value?.trim();
    return normalized ? normalized : undefined;
  }

  static #outcome(value: string | undefined): IdentitySecurityAuditOutcome | undefined {
    const normalized = value?.trim();
    if (normalized === "Succeeded" || normalized === "Denied" || normalized === "Failed") return normalized;
    return undefined;
  }

  static #limit(value: string | undefined): number {
    const parsed = Number(value ?? "50");
    return parsed === 25 || parsed === 50 || parsed === 100 ? parsed : 50;
  }
}
