import type { IdentitySecurityAuditOutcome, IdentitySecurityAuditQuery } from "@identity-access/client";

export interface IdentityAccessAdminSecurityAuditSearchParams {
  readonly tenantId?: string;
  readonly userId?: string;
  readonly outcome?: string;
  readonly correlationId?: string;
  readonly limit?: string;
}

/** Normalizes browser query parameters without performing API access or authorization. */
export class IdentityAccessAdminSecurityAuditQuery {
  static readonly #uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/iu;
  public readonly tenantId: string | undefined;
  public readonly userId: string | undefined;
  public readonly outcome: IdentitySecurityAuditOutcome | undefined;
  public readonly correlationId: string | undefined;
  public readonly limit: number;
  public readonly validationMessage: string | undefined;

  private constructor(
    tenantId: string | undefined,
    userId: string | undefined,
    outcome: IdentitySecurityAuditOutcome | undefined,
    correlationId: string | undefined,
    limit: number,
    validationMessage: string | undefined,
  ) {
    this.tenantId = tenantId;
    this.userId = userId;
    this.outcome = outcome;
    this.correlationId = correlationId;
    this.limit = limit;
    this.validationMessage = validationMessage;
  }

  public static fromSearchParams(params: IdentityAccessAdminSecurityAuditSearchParams): IdentityAccessAdminSecurityAuditQuery {
    const tenantId = IdentityAccessAdminSecurityAuditQuery.#trim(params.tenantId);
    const userId = IdentityAccessAdminSecurityAuditQuery.#trim(params.userId);
    const correlationId = IdentityAccessAdminSecurityAuditQuery.#trim(params.correlationId);
    const outcome = IdentityAccessAdminSecurityAuditQuery.#outcome(params.outcome);
    const limit = IdentityAccessAdminSecurityAuditQuery.#limit(params.limit);

    if (tenantId !== undefined && !IdentityAccessAdminSecurityAuditQuery.#uuidPattern.test(tenantId)) {
      return new IdentityAccessAdminSecurityAuditQuery(tenantId, userId, outcome, correlationId, limit, "Tenant ID must be a UUID.");
    }
    if (userId !== undefined && !IdentityAccessAdminSecurityAuditQuery.#uuidPattern.test(userId)) {
      return new IdentityAccessAdminSecurityAuditQuery(tenantId, userId, outcome, correlationId, limit, "User ID must be a UUID.");
    }
    if (correlationId !== undefined && correlationId.length > 64) {
      return new IdentityAccessAdminSecurityAuditQuery(tenantId, userId, outcome, correlationId, limit, "Correlation ID must not exceed 64 characters.");
    }
    if (params.outcome !== undefined && params.outcome.trim().length > 0 && outcome === undefined) {
      return new IdentityAccessAdminSecurityAuditQuery(tenantId, userId, undefined, correlationId, limit, "Outcome filter is invalid.");
    }

    return new IdentityAccessAdminSecurityAuditQuery(tenantId, userId, outcome, correlationId, limit, undefined);
  }

  public toClientQuery(): IdentitySecurityAuditQuery {
    if (this.validationMessage !== undefined) throw new Error(this.validationMessage);
    return {
      ...(this.tenantId === undefined ? {} : { tenantId: this.tenantId }),
      ...(this.userId === undefined ? {} : { userId: this.userId }),
      ...(this.outcome === undefined ? {} : { outcome: this.outcome }),
      ...(this.correlationId === undefined ? {} : { correlationId: this.correlationId }),
      limit: this.limit,
    };
  }

  public get hasFilters(): boolean {
    return this.tenantId !== undefined || this.userId !== undefined || this.outcome !== undefined || this.correlationId !== undefined;
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
    return parsed === 25 || parsed === 50 || parsed === 100 || parsed === 200 ? parsed : 50;
  }
}
