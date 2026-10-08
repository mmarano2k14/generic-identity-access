import "server-only";

import {
  GenericIdentityClientError,
  type IdentityAdministrationContext,
  type IdentityTenantAdministrationContext,
} from "@generic-identity/auth";
import type {
  IdentityEffectiveAdministrationContext,
  IdentityEntityReferenceKind,
  IdentityEntityReferenceOption,
  IdentityManagedPolicyRecord,
  IdentityResourceScopeRecord,
  IdentityScopeAuthorityGroupRecord,
  IdentityScopeAuthorityPolicyRecord,
  IdentityTenantRecord,
  IdentityTenantUserRecord,
  IdentityUserRecord,
} from "@generic-identity/contracts";
import { NextIdentityServerSession } from "./session";

export const NEXT_IDENTITY_ENTITY_REFERENCE_MINIMUM_SEARCH_LENGTH = 3;
export const NEXT_IDENTITY_ENTITY_REFERENCE_MAXIMUM_SEARCH_LENGTH = 128;
export const NEXT_IDENTITY_ENTITY_REFERENCE_MAXIMUM_RESULTS = 20;

const kinds = new Set<IdentityEntityReferenceKind>([
  "user",
  "tenant",
  "tenant-membership",
  "managed-policy",
  "resource-scope",
  "authority-group",
  "authority-policy",
]);

/**
 * Performs the bounded server-side lookup used by IdentityEntityAutocomplete.
 *
 * Query/tenant values remain requested context only. Effective tenant visibility
 * is rechecked here and protected Identity Access endpoints remain authoritative.
 */
export async function searchNextIdentityEntityReferences(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  kindValue: string,
  queryValue: string,
  tenantId?: string,
  includeInactive = false,
): Promise<readonly IdentityEntityReferenceOption[]> {
  assertEffectiveContext(administration, effective);
  const kind = parseKind(kindValue);
  const search = normalizeSearch(queryValue);
  const options = { search, limit: NEXT_IDENTITY_ENTITY_REFERENCE_MAXIMUM_RESULTS } as const;

  switch (kind) {
    case "user":
      requireScopeWide(effective);
      return users(await session.client.directory.users.list(administration, options));
    case "tenant":
      requireScopeWide(effective);
      return tenants(await session.client.directory.tenants.list(administration, options));
    case "tenant-membership": {
      const context = tenantContext(administration, effective, tenantId);
      return tenantMemberships(await session.client.directory.tenantUsers.list(context, {
        search,
        limit: NEXT_IDENTITY_ENTITY_REFERENCE_MAXIMUM_RESULTS,
        activeMembershipsOnly: true,
      }));
    }
    case "managed-policy": {
      const context = tenantContext(administration, effective, tenantId);
      return managedPolicies(await session.client.accessControl.managedPolicyBindings.listAvailablePolicies(context, options));
    }
    case "resource-scope": {
      const context = tenantContext(administration, effective, tenantId);
      return resourceScopes(await session.client.accessControl.resourceScopes.list(context, options), includeInactive);
    }
    case "authority-group":
      requireScopeWide(effective);
      return authorityGroups(await session.client.accessControl.delegatedAuthority.listGroups(administration, options));
    case "authority-policy":
      requireScopeWide(effective);
      return authorityPolicies(await session.client.accessControl.delegatedAuthority.listPolicies(administration, options));
  }
}

function parseKind(value: string): IdentityEntityReferenceKind {
  const normalized = value.trim() as IdentityEntityReferenceKind;
  if (!kinds.has(normalized)) throw new RangeError("Unknown Generic Identity entity reference kind.");
  return normalized;
}

function normalizeSearch(value: string): string {
  const search = value.trim();
  if (
    search.length < NEXT_IDENTITY_ENTITY_REFERENCE_MINIMUM_SEARCH_LENGTH
    || search.length > NEXT_IDENTITY_ENTITY_REFERENCE_MAXIMUM_SEARCH_LENGTH
  ) {
    throw new RangeError(
      `Search must contain between ${NEXT_IDENTITY_ENTITY_REFERENCE_MINIMUM_SEARCH_LENGTH} and ${NEXT_IDENTITY_ENTITY_REFERENCE_MAXIMUM_SEARCH_LENGTH} characters.`,
    );
  }
  return search;
}

function assertEffectiveContext(
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
): void {
  if (
    effective.identityScopeId !== administration.identityScopeId
    || effective.applicationKey !== administration.applicationKey
  ) throw new GenericIdentityClientError("configuration");
}

function requireScopeWide(effective: IdentityEffectiveAdministrationContext): void {
  if (effective.tenantVisibility !== "scope-wide") {
    throw new GenericIdentityClientError("forbidden", 403, "scope_wide_reference_search_required");
  }
}

function tenantContext(
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  tenantIdValue: string | undefined,
): IdentityTenantAdministrationContext {
  const tenantId = tenantIdValue?.trim().toLowerCase();
  if (!tenantId) throw new RangeError("A tenant is required for this reference lookup.");

  if (
    effective.tenantVisibility === "membership-limited"
    && !effective.activeTenantMemberships.some((membership) => membership.tenantId === tenantId)
  ) {
    throw new GenericIdentityClientError("forbidden", 403, "tenant_context_outside_visibility");
  }

  return { ...administration, tenantId };
}

function users(records: readonly IdentityUserRecord[]): readonly IdentityEntityReferenceOption[] {
  return records.map((record) => ({
    id: record.userId,
    displayName: record.displayName,
    description: record.status === 1 ? "Active user" : "Inactive user",
  }));
}

function tenants(records: readonly IdentityTenantRecord[]): readonly IdentityEntityReferenceOption[] {
  return records.map((record) => ({
    id: record.tenantId,
    displayName: record.displayName,
    description: record.status === 1 ? "Active tenant" : "Inactive tenant",
  }));
}

function tenantMemberships(records: readonly IdentityTenantUserRecord[]): readonly IdentityEntityReferenceOption[] {
  return records.map((record) => ({
    id: record.membershipId,
    displayName: record.displayName,
    description: `Tenant membership · ${record.userId}`,
    keywords: [record.userId],
  }));
}

function managedPolicies(records: readonly IdentityManagedPolicyRecord[]): readonly IdentityEntityReferenceOption[] {
  return records
    .filter((record) => record.status === 1 && record.defaultVersion !== undefined)
    .map((record) => ({
      id: record.policyId,
      displayName: record.displayName,
      description: `${record.policyKey} · default v${record.defaultVersion}`,
      keywords: [record.policyKey],
    }));
}

function resourceScopes(
  records: readonly IdentityResourceScopeRecord[],
  includeInactive: boolean,
): readonly IdentityEntityReferenceOption[] {
  return records
    .filter((record) => includeInactive || record.status === 1)
    .map((record) => ({
      id: record.resourceScopeId,
      displayName: record.displayName,
      description: `${record.scopeType} · ${record.externalResourceId}`,
      keywords: [record.scopeType, record.externalResourceId],
    }));
}

function authorityGroups(records: readonly IdentityScopeAuthorityGroupRecord[]): readonly IdentityEntityReferenceOption[] {
  return records.map((record) => ({
    id: record.groupId,
    displayName: record.displayName,
    description: record.status === 1 ? "Active authority group" : "Inactive authority group",
  }));
}

function authorityPolicies(records: readonly IdentityScopeAuthorityPolicyRecord[]): readonly IdentityEntityReferenceOption[] {
  return records.map((record) => ({
    id: record.policyId,
    displayName: record.displayName,
    description: record.status === 1 ? "Active authority policy" : "Inactive authority policy",
  }));
}
