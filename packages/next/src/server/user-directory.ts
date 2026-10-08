import "server-only";

import {
  GenericIdentityClientError,
  type IdentityAdministrationContext,
  type IdentityTenantAdministrationContext,
} from "@generic-identity/auth";
import type {
  IdentityEffectiveAdministrationContext,
  IdentityPasswordCredentialMetadataRecord,
  IdentityTenantLinkedUserRow,
  IdentityTenantUserRecord,
  IdentityUserRecord,
} from "@generic-identity/contracts";
import { presentNextIdentityMutationFailure, type NextIdentityMutationFailure } from "./mutation-failure";
import { createNextTenantAdministrationContext } from "./administration";
import { NextIdentityServerSession } from "./session";

export interface NextUsersDirectoryQuery {
  readonly tenantId?: string;
  readonly defaultTenantId?: string;
  readonly userId?: string;
  readonly tenantView?: "all";
  readonly tenantSearch?: string;
}

export interface NextUserTenantChoice {
  readonly tenantId: string;
  readonly displayName: string;
}

export interface NextUsersDirectory {
  readonly mode: "scope" | "tenant" | "all" | "tenant-required";
  readonly scopeWide: boolean;
  readonly tenantChoices: readonly NextUserTenantChoice[];
  readonly selectedTenantId?: string;
  /** Contains a credential: use in server code only; never serialize into Client Components. */
  readonly selectedTenantContext?: IdentityTenantAdministrationContext;
  readonly users: readonly IdentityUserRecord[];
  readonly selectedUser: IdentityUserRecord | null;
  readonly linkedUsers: readonly IdentityTenantLinkedUserRow[];
  /** List windows are bounded; this is not a full directory export. */
  readonly scanBoundReached: boolean;
}

const LIST_LIMIT = 50;
const AGGREGATE_TENANT_LIMIT = 1000;
const AGGREGATE_PAGE_SIZE = 200;
const AGGREGATE_CONCURRENCY = 8;

/**
 * Trusted, bounded admin-side user directory. Mirrors Identity Admin's three
 * genuinely different views: scope-wide, tenant-bounded, and membership-backed
 * cross-tenant aggregate. The aggregate never upgrades the subject's authority.
 */
export async function loadNextUsersDirectory(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  query: NextUsersDirectoryQuery = {},
): Promise<NextUsersDirectory> {
  if (effective.identityScopeId !== administration.identityScopeId ||
      effective.applicationKey !== administration.applicationKey) {
    throw new GenericIdentityClientError("configuration");
  }

  const scopeWide = effective.tenantVisibility === "scope-wide";
  const memberships = [...new Set(effective.activeTenantMemberships.map((item) => item.tenantId))];

  const requestedTenant = optionalId(query.tenantId);
  const fallbackTenant = optionalId(query.defaultTenantId);
  const selectedTenantId = requestedTenant ?? fallbackTenant ??
    (memberships.length === 1 ? memberships[0] : undefined);

  // An untrusted query/host default is a REQUEST for a tenant context, never
  // evidence of membership or authorization.
  if (!scopeWide && selectedTenantId !== undefined && !memberships.includes(selectedTenantId)) {
    throw new GenericIdentityClientError("forbidden", 403);
  }

  // A scope-wide user-directory read does not imply tenant-directory READ.
  // Discover tenant choices only when a tenant search was explicitly requested.
  // This mirrors the GOLDEN's on-demand tenant autocomplete boundary.
  const tenantSearch = typeof query.tenantSearch === "string"
    ? query.tenantSearch.trim().slice(0, 200)
    : "";
  const tenantChoices = scopeWide
    ? tenantSearch
      ? (await session.client.directory.tenants.list(administration, {
        search: tenantSearch,
        limit: LIST_LIMIT,
      })).map(({ tenantId, displayName }) => ({ tenantId, displayName }))
      : []
    : memberships.map((tenantId) => ({ tenantId, displayName: tenantId }));

  if (query.tenantView === "all") {
    const targets = scopeWide
      ? await enumerateAllTenants(session, administration)
      : tenantChoices;
    const rows: IdentityTenantLinkedUserRow[] = [];
    let scanBoundReached = false;

    for (let offset = 0; offset < targets.length; offset += AGGREGATE_CONCURRENCY) {
      const slice = targets.slice(offset, offset + AGGREGATE_CONCURRENCY);
      const batches = await Promise.all(slice.map(async (tenant) => {
        const context = makeTenantContext(session, administration, tenant.tenantId);
        try {
          const records = await session.client.directory.tenantUsers.list(context, { limit: LIST_LIMIT });
          return { tenant, records };
        } catch (error) {
          // A tenant read can have stricter policy than tenant discovery. Only
          // an explicit forbidden is skipped; a technical failure fails closed.
          if (isForbidden(error)) return { tenant, records: [] as readonly IdentityTenantUserRecord[] };
          throw error;
        }
      }));

      for (const { tenant, records } of batches) {
        if (records.length >= LIST_LIMIT) scanBoundReached = true;
        for (const record of records) {
          rows.push({
            tenantId: tenant.tenantId,
            tenantDisplayName: tenant.displayName,
            membershipId: record.membershipId,
            userId: record.userId,
            displayName: record.displayName,
            status: record.userStatus,
            version: record.userVersion,
          });
        }
      }
    }

    return {
      mode: "all", scopeWide, tenantChoices,
      users: [], selectedUser: null, linkedUsers: rows, scanBoundReached,
    };
  }

  const context = selectedTenantId === undefined
    ? undefined
    : makeTenantContext(session, administration, selectedTenantId);

  if (scopeWide) {
    const users = await session.client.directory.users.list(administration, { limit: LIST_LIMIT });
    const selectedUser = query.userId
      ? users.find((user) => user.userId === query.userId) ??
        await session.client.directory.users.get(administration, query.userId)
      : null;
    return {
      mode: "scope", scopeWide, tenantChoices, users, selectedUser,
      linkedUsers: [], scanBoundReached: users.length >= LIST_LIMIT,
      ...(context ? { selectedTenantId: context.tenantId, selectedTenantContext: context } : {}),
    };
  }

  if (context === undefined) {
    return {
      mode: "tenant-required", scopeWide, tenantChoices, users: [],
      selectedUser: null, linkedUsers: [], scanBoundReached: false,
    };
  }

  // Membership-limited subjects MUST NEVER call the scope-wide users API.
  const records = await session.client.directory.tenantUsers.list(context, { limit: LIST_LIMIT });
  const users = records.map(mapTenantUser);
  let selectedUser = query.userId
    ? users.find((user) => user.userId === query.userId) ?? null
    : null;

  if (query.userId && selectedUser === null) {
    const matches = await session.client.directory.tenantUsers.list(context, {
      search: query.userId, limit: 20,
    });
    const match = matches.find((record) => record.userId === query.userId);
    selectedUser = match ? mapTenantUser(match) : null;
  }

  return {
    mode: "tenant", scopeWide, tenantChoices, users, selectedUser,
    linkedUsers: [], scanBoundReached: records.length >= LIST_LIMIT,
    selectedTenantId: context.tenantId,
    selectedTenantContext: context,
  };
}

/** Read only non-secret metadata. Never turn a forbidden read into "not configured". */
export async function loadNextUserCredentialMetadata(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  userId: string,
): Promise<{
  readonly credential: IdentityPasswordCredentialMetadataRecord | null;
  readonly failure?: NextIdentityMutationFailure;
}> {
  try {
    return { credential: await session.client.account.passwordCredentials.get(administration, userId) };
  } catch (error) {
    const presented = presentNextIdentityMutationFailure(error);
    if (presented.kind === "unauthenticated" || presented.kind === "cancelled") throw error;
    return { credential: null, failure: presented };
  }
}

function mapTenantUser(record: IdentityTenantUserRecord): IdentityUserRecord {
  return {
    userId: record.userId,
    displayName: record.displayName,
    status: record.userStatus,
    version: record.userVersion,
  };
}

async function enumerateAllTenants(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
): Promise<readonly NextUserTenantChoice[]> {
  const targets: NextUserTenantChoice[] = [];
  for (let offset = 0; offset < AGGREGATE_TENANT_LIMIT; offset += AGGREGATE_PAGE_SIZE) {
    const page = await session.client.directory.tenants.list(administration, {
      offset, limit: AGGREGATE_PAGE_SIZE,
    });
    targets.push(...page.map(({ tenantId, displayName }) => ({ tenantId, displayName })));
    if (page.length < AGGREGATE_PAGE_SIZE) {
      return targets.sort(compareTenants);
    }
  }
  const overflow = await session.client.directory.tenants.list(administration, {
    offset: AGGREGATE_TENANT_LIMIT,
    limit: 1,
  });
  if (overflow.length > 0) {
    throw new Error("The aggregate tenant view exceeds the supported administration boundary. Select a tenant explicitly.");
  }
  return targets.sort(compareTenants);
}

function compareTenants(left: NextUserTenantChoice, right: NextUserTenantChoice): number {
  return left.displayName.localeCompare(right.displayName) || left.tenantId.localeCompare(right.tenantId);
}

function makeTenantContext(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  tenantId: string,
): IdentityTenantAdministrationContext {
  return createNextTenantAdministrationContext(
    session, administration.identityScopeId, administration.applicationKey, tenantId,
  );
}

function optionalId(value?: string): string | undefined {
  const normalized = value?.trim().toLowerCase();
  if (!normalized) return undefined;
  if (normalized.length > 64) {
    throw new Error("Invalid administration input: tenantId is too long.");
  }
  return normalized;
}

function isForbidden(error: unknown): boolean {
  return error instanceof GenericIdentityClientError &&
    (error.code === "forbidden" || (error.code === "http" && error.httpStatus === 403));
}
