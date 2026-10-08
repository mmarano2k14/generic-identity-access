import "server-only";

import {
  GenericIdentityClientError,
  type IdentityAdministrationContext,
  type IdentityTenantAdministrationContext,
} from "@generic-identity/auth";
import type {
  IdentityEffectiveAdministrationContext,
  IdentityResourceScopeRecord,
} from "@generic-identity/contracts";
import { createNextTenantAdministrationContext } from "./administration";
import { isAllowedOnServer } from "./authorization";
import { NextIdentityServerSession } from "./session";

const RESOURCE_SCOPE_LIMIT = 200;
const TENANT_PAGE_SIZE = 200;
const MAXIMUM_AGGREGATE_TENANTS = 1000;

export interface NextResourceScopeWorkspaceQuery {
  readonly tenantId?: string;
  readonly tenantView?: string;
  readonly resourceScopeId?: string;
  readonly defaultTenantId?: string;
}

export interface NextResourceScopeWorkspaceTenant {
  readonly tenantId: string;
  readonly displayName: string;
}

export interface NextResourceScopeAggregateRecord {
  readonly tenant: NextResourceScopeWorkspaceTenant;
  readonly scope: IdentityResourceScopeRecord;
  readonly canWrite: boolean;
}

export interface NextResourceScopeWorkspacePermissions {
  readonly canReadResourceScopes: boolean;
  readonly canWriteResourceScopes: boolean;
  readonly canReadScopeTypes: boolean;
}

export interface NextResourceScopeWorkspace {
  readonly scopeWide: boolean;
  readonly allTenants: boolean;
  readonly tenants: readonly NextResourceScopeWorkspaceTenant[];
  readonly selectedTenantId?: string;
  readonly selectedTenant?: NextResourceScopeWorkspaceTenant;
  readonly tenantContext?: IdentityTenantAdministrationContext;
  readonly aggregateScopes: readonly NextResourceScopeAggregateRecord[];
  readonly resourceScopes: readonly IdentityResourceScopeRecord[];
  readonly selectedResourceScope: IdentityResourceScopeRecord | null;
  readonly parentResourceScope: IdentityResourceScopeRecord | null;
  readonly permissions: NextResourceScopeWorkspacePermissions;
  readonly resourceScopesTruncated: boolean;
  readonly aggregateTenantsTruncated: boolean;
}

const noPermissions: NextResourceScopeWorkspacePermissions = {
  canReadResourceScopes: false,
  canWriteResourceScopes: false,
  canReadScopeTypes: false,
};

/**
 * Composes the Resource Scope GOLDEN workspace.
 *
 * Tenant query values are requested context only. Every selected tenant is
 * revalidated against the effective administration context, every protected
 * read is capability-gated, and "all tenants" remains a read-only fan-out.
 */
export async function loadNextResourceScopeWorkspace(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  query: NextResourceScopeWorkspaceQuery = {},
): Promise<NextResourceScopeWorkspace> {
  assertEffectiveContext(administration, effective);

  const scopeWide = effective.tenantVisibility === "scope-wide";
  const allTenants = query.tenantView === "all";
  const canReadScopeTypesWithoutTenant = await isAllowedOnServer(
    session,
    { identityScopeId: administration.identityScopeId, applicationKey: administration.applicationKey },
    { resource: "identity-access", feature: "scope-type", action: "read" },
  );

  if (allTenants) {
    const { tenants, truncated } = await listAuthorizedTenantTargets(session, administration, effective);
    const aggregateScopes: NextResourceScopeAggregateRecord[] = [];

    for (const tenant of tenants) {
      const tenantContext = createNextTenantAdministrationContext(
        session,
        administration.identityScopeId,
        administration.applicationKey,
        tenant.tenantId,
      );
      const tenantBoundary = {
        identityScopeId: administration.identityScopeId,
        applicationKey: administration.applicationKey,
        tenantId: tenant.tenantId,
      };
      const [canRead, canWrite] = await Promise.all([
        isAllowedOnServer(
          session,
          tenantBoundary,
          { resource: "identity-access", feature: "resource-scope", action: "read" },
        ),
        isAllowedOnServer(
          session,
          tenantBoundary,
          { resource: "identity-access", feature: "resource-scope", action: "write" },
        ),
      ]);
      if (!canRead) continue;

      const records = await session.client.accessControl.resourceScopes.list(
        tenantContext,
        { limit: RESOURCE_SCOPE_LIMIT },
      );
      for (const scope of records) aggregateScopes.push({ tenant, scope, canWrite });
    }

    return {
      scopeWide,
      allTenants: true,
      tenants,
      aggregateScopes,
      resourceScopes: [],
      selectedResourceScope: null,
      parentResourceScope: null,
      permissions: { ...noPermissions, canReadScopeTypes: canReadScopeTypesWithoutTenant },
      resourceScopesTruncated: false,
      aggregateTenantsTruncated: truncated,
    };
  }

  const requestedTenantId = query.tenantId?.trim().toLowerCase()
    || query.defaultTenantId?.trim().toLowerCase();
  const tenantResolution = await resolveTenant(
    session,
    administration,
    effective,
    requestedTenantId,
  );

  const empty: NextResourceScopeWorkspace = {
    scopeWide,
    allTenants: false,
    tenants: tenantResolution.tenants,
    ...(tenantResolution.selectedTenantId === undefined ? {} : { selectedTenantId: tenantResolution.selectedTenantId }),
    ...(tenantResolution.selectedTenant === undefined ? {} : { selectedTenant: tenantResolution.selectedTenant }),
    aggregateScopes: [],
    resourceScopes: [],
    selectedResourceScope: null,
    parentResourceScope: null,
    permissions: { ...noPermissions, canReadScopeTypes: canReadScopeTypesWithoutTenant },
    resourceScopesTruncated: false,
    aggregateTenantsTruncated: false,
  };

  if (!tenantResolution.selectedTenantId || !tenantResolution.selectedTenant) return empty;

  const tenantContext = createNextTenantAdministrationContext(
    session,
    administration.identityScopeId,
    administration.applicationKey,
    tenantResolution.selectedTenantId,
  );
  const tenantBoundary = {
    identityScopeId: administration.identityScopeId,
    applicationKey: administration.applicationKey,
    tenantId: tenantResolution.selectedTenantId,
  };
  const scopeBoundary = {
    identityScopeId: administration.identityScopeId,
    applicationKey: administration.applicationKey,
  };

  const [canReadResourceScopes, canWriteResourceScopes, canReadScopeTypes] = await Promise.all([
    isAllowedOnServer(
      session,
      tenantBoundary,
      { resource: "identity-access", feature: "resource-scope", action: "read" },
    ),
    isAllowedOnServer(
      session,
      tenantBoundary,
      { resource: "identity-access", feature: "resource-scope", action: "write" },
    ),
    isAllowedOnServer(
      session,
      scopeBoundary,
      { resource: "identity-access", feature: "scope-type", action: "read" },
    ),
  ]);
  const permissions = { canReadResourceScopes, canWriteResourceScopes, canReadScopeTypes };

  if (!canReadResourceScopes) {
    if (query.resourceScopeId?.trim()) throw new GenericIdentityClientError("forbidden", 403);
    return { ...empty, tenantContext, permissions };
  }

  const resourceScopes = await session.client.accessControl.resourceScopes.list(
    tenantContext,
    { limit: RESOURCE_SCOPE_LIMIT },
  );
  const requestedResourceScopeId = query.resourceScopeId?.trim().toLowerCase();
  const selectedResourceScope = requestedResourceScopeId
    ? resourceScopes.find((scope) => scope.resourceScopeId === requestedResourceScopeId)
      ?? await session.client.accessControl.resourceScopes.get(tenantContext, requestedResourceScopeId)
    : null;
  if (requestedResourceScopeId && !selectedResourceScope) {
    throw new GenericIdentityClientError("forbidden", 403);
  }

  const parentResourceScope = selectedResourceScope?.parentResourceScopeId
    ? resourceScopes.find((scope) => scope.resourceScopeId === selectedResourceScope.parentResourceScopeId)
      ?? await session.client.accessControl.resourceScopes.get(
        tenantContext,
        selectedResourceScope.parentResourceScopeId,
      )
    : null;

  return {
    scopeWide,
    allTenants: false,
    tenants: tenantResolution.tenants,
    selectedTenantId: tenantResolution.selectedTenantId,
    selectedTenant: tenantResolution.selectedTenant,
    tenantContext,
    aggregateScopes: [],
    resourceScopes,
    selectedResourceScope,
    parentResourceScope,
    permissions,
    resourceScopesTruncated: resourceScopes.length >= RESOURCE_SCOPE_LIMIT,
    aggregateTenantsTruncated: false,
  };
}

async function resolveTenant(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  requestedTenantId?: string,
): Promise<{
  readonly tenants: readonly NextResourceScopeWorkspaceTenant[];
  readonly selectedTenantId?: string;
  readonly selectedTenant?: NextResourceScopeWorkspaceTenant;
}> {
  if (effective.tenantVisibility === "scope-wide") {
    if (!requestedTenantId) return { tenants: [] };
    const record = await session.client.directory.tenants.get(administration, requestedTenantId);
    if (!record) throw new GenericIdentityClientError("forbidden", 403);
    const selectedTenant = { tenantId: record.tenantId, displayName: record.displayName };
    return { tenants: [selectedTenant], selectedTenantId: record.tenantId, selectedTenant };
  }

  const tenants = [...new Map(effective.activeTenantMemberships.map((membership) => [
    membership.tenantId,
    { tenantId: membership.tenantId, displayName: membership.tenantId },
  ])).values()].sort((left, right) => left.tenantId.localeCompare(right.tenantId));

  const selectedTenantId = requestedTenantId || (tenants.length === 1 ? tenants[0]?.tenantId : undefined);
  const selectedTenant = selectedTenantId
    ? tenants.find((tenant) => tenant.tenantId === selectedTenantId)
    : undefined;
  if (selectedTenantId && !selectedTenant) throw new GenericIdentityClientError("forbidden", 403);

  return {
    tenants,
    ...(selectedTenantId === undefined ? {} : { selectedTenantId }),
    ...(selectedTenant === undefined ? {} : { selectedTenant }),
  };
}

async function listAuthorizedTenantTargets(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
): Promise<{ readonly tenants: readonly NextResourceScopeWorkspaceTenant[]; readonly truncated: boolean }> {
  if (effective.tenantVisibility === "membership-limited") {
    const tenants = [...new Map(effective.activeTenantMemberships.map((membership) => [
      membership.tenantId,
      { tenantId: membership.tenantId, displayName: membership.tenantId },
    ])).values()].sort((left, right) => left.tenantId.localeCompare(right.tenantId));
    return { tenants, truncated: false };
  }

  const tenants: NextResourceScopeWorkspaceTenant[] = [];
  for (let offset = 0; offset < MAXIMUM_AGGREGATE_TENANTS; offset += TENANT_PAGE_SIZE) {
    const page = await session.client.directory.tenants.list(administration, {
      offset,
      limit: TENANT_PAGE_SIZE,
    });
    tenants.push(...page.map((tenant) => ({
      tenantId: tenant.tenantId,
      displayName: tenant.displayName,
    })));
    if (page.length < TENANT_PAGE_SIZE) {
      return {
        tenants: tenants.sort((a, b) => a.displayName.localeCompare(b.displayName) || a.tenantId.localeCompare(b.tenantId)),
        truncated: false,
      };
    }
  }

  return {
    tenants: tenants.sort((a, b) => a.displayName.localeCompare(b.displayName) || a.tenantId.localeCompare(b.tenantId)),
    truncated: true,
  };
}

function assertEffectiveContext(
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
): void {
  if (
    administration.identityScopeId !== effective.identityScopeId
    || administration.applicationKey !== effective.applicationKey
  ) throw new GenericIdentityClientError("configuration");
}
