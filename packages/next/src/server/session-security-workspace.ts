import "server-only";

import {
  GenericIdentityClientError,
  type IdentityAdministrationContext,
} from "@generic-identity/auth";
import type {
  IdentityEffectiveAdministrationContext,
  IdentitySecurityAuditOutcome,
  IdentitySecurityAuditRecord,
} from "@generic-identity/contracts";
import { isAllowedOnServer } from "./authorization";
import {
  presentNextIdentityMutationFailure,
  type NextIdentityMutationFailure,
} from "./mutation-failure";
import { NextIdentityServerSession } from "./session";

export interface NextSessionSecurityWorkspaceQuery {
  readonly userId?: string;
  readonly clientId?: string;
  readonly outcome?: string;
  readonly limit?: string;
}

export interface NextSessionSecurityNormalizedQuery {
  readonly userId?: string;
  readonly clientId?: string;
  readonly outcome?: IdentitySecurityAuditOutcome;
  readonly limit: 25 | 50 | 100;
  readonly validationMessage?: string;
  readonly hasFilters: boolean;
}

export type NextSessionSecurityEvidenceState =
  | "available"
  | "forbidden"
  | "unavailable";

export interface NextSessionSecuritySummary {
  readonly total: number;
  readonly issued: number;
  readonly revocationEvents: number;
  readonly assuranceEvents: number;
  readonly continuityAlerts: number;
}

export interface NextSessionSecurityWorkspacePermissions {
  readonly canReadAudit: boolean;
  readonly canWriteSessions: boolean;
  readonly canReadUsers: boolean;
  readonly scopeWide: boolean;
}

export interface NextSessionSecurityWorkspace {
  readonly evidenceState: NextSessionSecurityEvidenceState;
  readonly records: readonly IdentitySecurityAuditRecord[];
  readonly query: NextSessionSecurityNormalizedQuery;
  readonly summary: NextSessionSecuritySummary;
  readonly permissions: NextSessionSecurityWorkspacePermissions;
  readonly failure?: NextIdentityMutationFailure;
}

const eventTypes = Object.freeze([
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

const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/iu;

/**
 * Composes bounded session-security evidence and separately authorized
 * containment capabilities.
 *
 * The current administration API does NOT expose an active-session list.
 * Therefore this workspace never infers active/expired/revoked session state
 * from missing audit evidence.
 */
export async function loadNextSessionSecurityWorkspace(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  queryValue: NextSessionSecurityWorkspaceQuery = {},
): Promise<NextSessionSecurityWorkspace> {
  const query = normalizeNextSessionSecurityQuery(queryValue);
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

  const [canReadAudit, canWriteSessions, canReadUsers] = await Promise.all([
    capability("security-audit", "read"),
    capability("session", "write"),
    capability("user", "read"),
  ]);

  const permissions: NextSessionSecurityWorkspacePermissions = {
    canReadAudit,
    canWriteSessions,
    canReadUsers,
    scopeWide: effective.tenantVisibility === "scope-wide",
  };

  if (query.validationMessage !== undefined) {
    return {
      evidenceState: "available",
      records: [],
      query,
      summary: emptySummary(),
      permissions,
    };
  }

  if (!canReadAudit) {
    return {
      evidenceState: "forbidden",
      records: [],
      query,
      summary: emptySummary(),
      permissions,
    };
  }

  try {
    const windows = await Promise.all(
      eventTypes.map((eventType) =>
        session.client.security.audit.list(context, {
          ...(query.userId === undefined ? {} : { userId: query.userId }),
          ...(query.outcome === undefined ? {} : { outcome: query.outcome }),
          eventType,
          limit: query.limit,
        }),
      ),
    );

    const records = windows
      .flat()
      .filter((record) => matchesSessionSecurityQuery(record, query))
      .sort((left, right) => Date.parse(right.occurredAt) - Date.parse(left.occurredAt))
      .slice(0, query.limit);

    return {
      evidenceState: "available",
      records,
      query,
      summary: summarizeSessionSecurity(records),
      permissions,
    };
  } catch (error) {
    if (
      error instanceof GenericIdentityClientError
      && (error.code === "unauthenticated" || error.code === "cancelled")
    ) {
      throw error;
    }

    return {
      evidenceState: "unavailable",
      records: [],
      query,
      summary: emptySummary(),
      permissions,
      failure: presentNextIdentityMutationFailure(error),
    };
  }
}

export function normalizeNextSessionSecurityQuery(
  params: NextSessionSecurityWorkspaceQuery,
): NextSessionSecurityNormalizedQuery {
  const userId = trimmed(params.userId);
  const clientId = trimmed(params.clientId);
  const outcome = parsedOutcome(params.outcome);
  const limit = parsedLimit(params.limit);

  let validationMessage: string | undefined;

  if (userId !== undefined && !uuidPattern.test(userId)) {
    validationMessage = "User ID must be a UUID.";
  } else if (clientId !== undefined && clientId.length > 128) {
    validationMessage = "Client ID must not exceed 128 characters.";
  } else if (
    params.outcome !== undefined
    && params.outcome.trim().length > 0
    && outcome === undefined
  ) {
    validationMessage = "Outcome filter is invalid.";
  }

  return {
    ...(userId === undefined ? {} : { userId }),
    ...(clientId === undefined ? {} : { clientId }),
    ...(outcome === undefined ? {} : { outcome }),
    limit,
    ...(validationMessage === undefined ? {} : { validationMessage }),
    hasFilters: userId !== undefined || clientId !== undefined || outcome !== undefined,
  };
}

export function summarizeSessionSecurity(
  records: readonly IdentitySecurityAuditRecord[],
): NextSessionSecuritySummary {
  return {
    total: records.length,
    issued: records.filter((record) => record.eventType === "PasswordLoginSucceeded").length,
    revocationEvents: records.filter(
      (record) => record.eventType.includes("Revoked") || record.eventType.includes("Revocation"),
    ).length,
    assuranceEvents: records.filter((record) => record.eventType.includes("Assurance")).length,
    continuityAlerts: records.filter(
      (record) => record.eventType === "OidcRefreshTokenReuseDetected",
    ).length,
  };
}

function matchesSessionSecurityQuery(
  record: IdentitySecurityAuditRecord,
  query: NextSessionSecurityNormalizedQuery,
): boolean {
  if (query.userId !== undefined && record.userId !== query.userId) return false;
  if (query.clientId !== undefined && record.clientId !== query.clientId) return false;
  if (query.outcome !== undefined && record.outcome !== query.outcome) return false;
  return true;
}

function emptySummary(): NextSessionSecuritySummary {
  return {
    total: 0,
    issued: 0,
    revocationEvents: 0,
    assuranceEvents: 0,
    continuityAlerts: 0,
  };
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

function parsedLimit(value: string | undefined): 25 | 50 | 100 {
  const parsed = Number(value ?? "50");
  return parsed === 25 || parsed === 50 || parsed === 100 ? parsed : 50;
}
