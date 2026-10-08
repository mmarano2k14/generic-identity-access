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
import {
  administrationLifecycleStatus,
  positiveAdministrationInteger,
  requiredAdministrationText,
} from "./administration-form";
import { createNextTenantAdministrationContext } from "./administration";
import { isAllowedOnServer } from "./authorization";
import { NextIdentityServerSession } from "./session";

export async function createNextResourceScopeFromForm(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  form: FormData,
): Promise<IdentityResourceScopeRecord> {
  const context = await resolveMutationTenantContext(session, administration, effective, form);
  await requireResourceScopeWrite(session, context);

  const modelVersion = positiveAdministrationInteger(form, "modelVersion");
  const scopeType = requiredAdministrationText(form, "scopeType", 128);
  await requireRegisteredScopeType(session, administration, modelVersion, scopeType);

  const parentResourceScopeId = optionalText(form, "parentResourceScopeId", 64);
  await requireValidParent(session, context, parentResourceScopeId);

  return session.client.accessControl.resourceScopes.create(context, {
    modelVersion,
    scopeType,
    externalResourceId: requiredAdministrationText(form, "externalResourceId", 256),
    displayName: requiredAdministrationText(form, "displayName", 200),
    ...(parentResourceScopeId === undefined ? {} : { parentResourceScopeId }),
    status: administrationLifecycleStatus(form, "status"),
  });
}

export async function updateNextResourceScopeFromForm(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  form: FormData,
): Promise<IdentityResourceScopeRecord> {
  const context = await resolveMutationTenantContext(session, administration, effective, form);
  await requireResourceScopeWrite(session, context);

  const resourceScopeId = requiredAdministrationText(form, "resourceScopeId", 64);
  const current = await session.client.accessControl.resourceScopes.get(context, resourceScopeId);
  if (!current) throw new GenericIdentityClientError("forbidden", 403);

  const modelVersion = positiveAdministrationInteger(form, "modelVersion");
  const scopeType = requiredAdministrationText(form, "scopeType", 128);
  await requireRegisteredScopeType(session, administration, modelVersion, scopeType);

  const parentResourceScopeId = optionalText(form, "parentResourceScopeId", 64);
  if (parentResourceScopeId === resourceScopeId) {
    throw new Error("Invalid administration input: a resource scope cannot be its own parent.");
  }
  await requireValidParent(session, context, parentResourceScopeId);

  return session.client.accessControl.resourceScopes.update(context, resourceScopeId, {
    modelVersion,
    scopeType,
    externalResourceId: requiredAdministrationText(form, "externalResourceId", 256),
    displayName: requiredAdministrationText(form, "displayName", 200),
    ...(parentResourceScopeId === undefined ? {} : { parentResourceScopeId }),
    status: administrationLifecycleStatus(form, "status"),
    expectedVersion: positiveAdministrationInteger(form, "expectedVersion"),
  });
}

async function resolveMutationTenantContext(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  form: FormData,
): Promise<IdentityTenantAdministrationContext> {
  assertEffectiveContext(administration, effective);
  const tenantId = requiredAdministrationText(form, "tenantId", 64).toLowerCase();

  if (effective.tenantVisibility === "scope-wide") {
    const tenant = await session.client.directory.tenants.get(administration, tenantId);
    if (!tenant) throw new GenericIdentityClientError("forbidden", 403);
  } else if (!effective.activeTenantMemberships.some((membership) => membership.tenantId === tenantId)) {
    throw new GenericIdentityClientError("forbidden", 403);
  }

  return createNextTenantAdministrationContext(
    session,
    administration.identityScopeId,
    administration.applicationKey,
    tenantId,
  );
}

async function requireResourceScopeWrite(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
): Promise<void> {
  const allowed = await isAllowedOnServer(
    session,
    {
      identityScopeId: context.identityScopeId,
      applicationKey: context.applicationKey,
      tenantId: context.tenantId,
    },
    { resource: "identity-access", feature: "resource-scope", action: "write" },
  );
  if (!allowed) throw new GenericIdentityClientError("forbidden", 403);
}

async function requireRegisteredScopeType(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  modelVersion: number,
  scopeType: string,
): Promise<void> {
  const types = await session.client.applicationSecurity.scopeTypes.list(administration, modelVersion);
  if (!types.some((candidate) => candidate.key === scopeType)) {
    throw new Error("Invalid administration input: selected scope type is not registered for the security-model version.");
  }
}

async function requireValidParent(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  parentResourceScopeId?: string,
): Promise<void> {
  if (parentResourceScopeId === undefined) return;
  const parent = await session.client.accessControl.resourceScopes.get(context, parentResourceScopeId);
  if (!parent) {
    throw new Error("Invalid administration input: selected parent resource scope was not found in the owning tenant.");
  }
}

function optionalText(form: FormData, field: string, maxLength: number): string | undefined {
  const value = form.get(field);
  if (value === null || value === "") return undefined;
  if (typeof value !== "string") throw new Error(`Invalid administration input: ${field} is invalid.`);
  const normalized = value.trim();
  if (!normalized) return undefined;
  if (normalized.length > maxLength) {
    throw new Error(`Invalid administration input: ${field} is too long.`);
  }
  return normalized;
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
