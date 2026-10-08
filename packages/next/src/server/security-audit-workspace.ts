import "server-only";

import type { IdentityAdministrationContext } from "@generic-identity/auth";
import type {
  IdentityEffectiveAdministrationContext,
  IdentitySecurityAuditOutcome,
  IdentitySecurityAuditRecord,
} from "@generic-identity/contracts";
import { isAllowedOnServer } from "./authorization";
import { NextIdentityServerSession } from "./session";

export interface NextSecurityAuditWorkspaceQuery {
  readonly tenantId?: string;
  readonly userId?: string;
  readonly outcome?: string;
  readonly correlationId?: string;
  readonly limit?: string;
}

export interface NextSecurityAuditNormalizedQuery {
  readonly tenantId?: string;
  readonly userId?: string;
  readonly outcome?: IdentitySecurityAuditOutcome;
  readonly correlationId?: string;
  readonly limit: 25 | 50 | 100 | 200;
  readonly validationMessage?: string;
  readonly hasFilters: boolean;
}

export interface NextSecurityAuditSummary {
  readonly total: number;
  readonly succeeded: number;
  readonly denied: number;
  readonly failed: number;
}

export interface NextSecurityAuditWorkspacePermissions {
  readonly canReadAudit: boolean;
  readonly canReadUsers: boolean;
  readonly canReadTenants: boolean;
  readonly scopeWide: boolean;
}

export interface NextSecurityAuditWorkspace {
  readonly records: readonly IdentitySecurityAuditRecord[];
  readonly query: NextSecurityAuditNormalizedQuery;
  readonly summary: NextSecurityAuditSummary;
  readonly permissions: NextSecurityAuditWorkspacePermissions;
}

const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/iu;

/**
 * Composes the read-only, bounded, application-scoped Security Audit workspace.
 *
 * Query values only filter already-authorized evidence. They never change
 * application identity, database placement, or authorization scope.
 */
export async function loadNextSecurityAuditWorkspace(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  queryValue: NextSecurityAuditWorkspaceQuery = {},
): Promise<NextSecurityAuditWorkspace> {
  const query = normalizeNextSecurityAuditQuery(queryValue);
  const boundary = {
    identityScopeId: context.identityScopeId,
    applicationKey: context.applicationKey,
  };
  const capability = (feature: string, action: string) =>
    isAllowedOnServer(session, boundary, {
      resource: "identity-access",
      feature,
      action,
    });

  const [canReadAudit, canReadUsers, canReadTenants] = await Promise.all([
    capability("security-audit", "read"),
    capability("user", "read"),
    capability("tenant", "read"),
  ]);

  const permissions: NextSecurityAuditWorkspacePermissions = {
    canReadAudit,
    canReadUsers,
    canReadTenants,
    scopeWide: effective.tenantVisibility === "scope-wide",
  };

  if (!canReadAudit || query.validationMessage !== undefined) {
    return {
      records: [],
      query,
      summary: emptySummary(),
      permissions,
    };
  }

  const records = await session.client.security.audit.list(context, {
    ...(query.tenantId === undefined ? {} : { tenantId: query.tenantId }),
    ...(query.userId === undefined ? {} : { userId: query.userId }),
    ...(query.outcome === undefined ? {} : { outcome: query.outcome }),
    ...(query.correlationId === undefined ? {} : { correlationId: query.correlationId }),
    limit: query.limit,
  });

  return {
    records,
    query,
    summary: summarizeSecurityAudit(records),
    permissions,
  };
}

export function normalizeNextSecurityAuditQuery(
  params: NextSecurityAuditWorkspaceQuery,
): NextSecurityAuditNormalizedQuery {
  const tenantId = trimmed(params.tenantId);
  const userId = trimmed(params.userId);
  const correlationId = trimmed(params.correlationId);
  const outcome = parsedOutcome(params.outcome);
  const limit = parsedLimit(params.limit);

  let validationMessage: string | undefined;

  if (tenantId !== undefined && !uuidPattern.test(tenantId)) {
    validationMessage = "Tenant ID must be a UUID.";
  } else if (userId !== undefined && !uuidPattern.test(userId)) {
    validationMessage = "User ID must be a UUID.";
  } else if (correlationId !== undefined && correlationId.length > 64) {
    validationMessage = "Correlation ID must not exceed 64 characters.";
  } else if (
    params.outcome !== undefined
    && params.outcome.trim().length > 0
    && outcome === undefined
  ) {
    validationMessage = "Outcome filter is invalid.";
  }

  return {
    ...(tenantId === undefined ? {} : { tenantId }),
    ...(userId === undefined ? {} : { userId }),
    ...(outcome === undefined ? {} : { outcome }),
    ...(correlationId === undefined ? {} : { correlationId }),
    limit,
    ...(validationMessage === undefined ? {} : { validationMessage }),
    hasFilters: tenantId !== undefined
      || userId !== undefined
      || outcome !== undefined
      || correlationId !== undefined,
  };
}

export function summarizeSecurityAudit(
  records: readonly IdentitySecurityAuditRecord[],
): NextSecurityAuditSummary {
  return {
    total: records.length,
    succeeded: records.filter((record) => record.outcome === "Succeeded").length,
    denied: records.filter((record) => record.outcome === "Denied").length,
    failed: records.filter((record) => record.outcome === "Failed").length,
  };
}

function emptySummary(): NextSecurityAuditSummary {
  return { total: 0, succeeded: 0, denied: 0, failed: 0 };
}

function trimmed(value: string | undefined): string | undefined {
  const normalized = value?.trim();
  return normalized ? normalized : undefined;
}

function parsedOutcome(value: string | undefined): IdentitySecurityAuditOutcome | undefined {
  const normalized = value?.trim();
  return normalized === "Succeeded" || normalized === "Denied" || normalized === "Failed"
    ? normalized
    : undefined;
}

function parsedLimit(value: string | undefined): 25 | 50 | 100 | 200 {
  const parsed = Number(value ?? "50");
  return parsed === 25 || parsed === 50 || parsed === 100 || parsed === 200
    ? parsed
    : 50;
}
