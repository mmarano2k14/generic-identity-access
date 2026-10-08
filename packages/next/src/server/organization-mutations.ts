import "server-only";

import type { IdentityTenantAdministrationContext } from "@generic-identity/auth";
import type {
  IdentityOrganizationMembershipRecord,
  IdentityOrganizationRecord,
  IdentityOrganizationResourceScopeLinkRecord,
} from "@generic-identity/contracts";
import {
  positiveAdministrationInteger,
  requiredAdministrationText,
} from "./administration-form";
import { NextIdentityServerSession } from "./session";

/** Creates one tenant-local Organization using GOLDEN field bounds. */
export async function createNextOrganizationFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<IdentityOrganizationRecord> {
  const parentOrganizationId = optionalText(form, "parentOrganizationId", 64);
  return session.client.organizations.organizations.create(context, {
    organizationKey: slug(form, "organizationKey", 64),
    displayName: requiredAdministrationText(form, "displayName", 200),
    organizationType: slug(form, "organizationType", 64),
    ...(parentOrganizationId === undefined ? {} : { parentOrganizationId }),
  });
}

/** Updates mutable Organization definition fields with optimistic concurrency. */
export async function updateNextOrganizationFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<IdentityOrganizationRecord> {
  const organizationId = requiredAdministrationText(form, "organizationId", 64);
  const parentOrganizationId = optionalText(form, "parentOrganizationId", 64);
  if (parentOrganizationId === organizationId) {
    throw new Error("Invalid administration input: an Organization cannot be its own parent.");
  }
  return session.client.organizations.organizations.update(context, organizationId, {
    displayName: requiredAdministrationText(form, "displayName", 200),
    organizationType: slug(form, "organizationType", 64),
    ...(parentOrganizationId === undefined ? {} : { parentOrganizationId }),
    expectedRowVersion: positiveAdministrationInteger(form, "expectedRowVersion"),
  });
}

export async function enableNextOrganizationFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<IdentityOrganizationRecord> {
  return session.client.organizations.organizations.enable(
    context,
    requiredAdministrationText(form, "organizationId", 64),
    positiveAdministrationInteger(form, "expectedRowVersion"),
  );
}

export async function disableNextOrganizationFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<IdentityOrganizationRecord> {
  return session.client.organizations.organizations.disable(
    context,
    requiredAdministrationText(form, "organizationId", 64),
    positiveAdministrationInteger(form, "expectedRowVersion"),
  );
}

/** Adds an active tenant member to an Organization. */
export async function addNextOrganizationMembershipFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<IdentityOrganizationMembershipRecord> {
  const organizationId = requiredAdministrationText(form, "organizationId", 64);
  const tenantMembershipId = requiredAdministrationText(form, "tenantMembershipId", 64);
  const membership = await session.client.directory.memberships.get(context, tenantMembershipId);
  if (!membership || membership.tenantId !== context.tenantId || membership.status !== 1) {
    throw new Error("Invalid administration input: select an active tenant membership from this tenant.");
  }
  return session.client.organizations.memberships.add(context, organizationId, tenantMembershipId);
}

export async function activateNextOrganizationMembershipFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<IdentityOrganizationMembershipRecord> {
  return session.client.organizations.memberships.activate(
    context,
    requiredAdministrationText(form, "organizationId", 64),
    requiredAdministrationText(form, "tenantMembershipId", 64),
    positiveAdministrationInteger(form, "expectedRowVersion"),
  );
}

export async function suspendNextOrganizationMembershipFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<IdentityOrganizationMembershipRecord> {
  return session.client.organizations.memberships.suspend(
    context,
    requiredAdministrationText(form, "organizationId", 64),
    requiredAdministrationText(form, "tenantMembershipId", 64),
    positiveAdministrationInteger(form, "expectedRowVersion"),
  );
}

export async function removeNextOrganizationMembershipFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<boolean> {
  return session.client.organizations.memberships.remove(
    context,
    requiredAdministrationText(form, "organizationId", 64),
    requiredAdministrationText(form, "tenantMembershipId", 64),
    positiveAdministrationInteger(form, "expectedRowVersion"),
  );
}

/** Links an Organization only to an active ResourceScope in the same trusted context. */
export async function linkNextOrganizationResourceScopeFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<IdentityOrganizationResourceScopeLinkRecord> {
  const organizationId = requiredAdministrationText(form, "organizationId", 64);
  const resourceScopeId = requiredAdministrationText(form, "resourceScopeId", 64);
  await requireActiveResourceScope(session, context, resourceScopeId);
  return session.client.organizations.resourceScopeLinks.create(context, organizationId, resourceScopeId);
}

export async function relinkNextOrganizationResourceScopeFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<IdentityOrganizationResourceScopeLinkRecord> {
  const organizationId = requiredAdministrationText(form, "organizationId", 64);
  const resourceScopeId = requiredAdministrationText(form, "resourceScopeId", 64);
  await requireActiveResourceScope(session, context, resourceScopeId);
  return session.client.organizations.resourceScopeLinks.update(
    context,
    organizationId,
    resourceScopeId,
    positiveAdministrationInteger(form, "expectedRowVersion"),
  );
}

export async function unlinkNextOrganizationResourceScopeFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<boolean> {
  if (form.get("confirmation") !== "REMOVE") {
    throw new Error("Invalid administration input: type REMOVE to confirm this security-sensitive operation.");
  }
  return session.client.organizations.resourceScopeLinks.remove(
    context,
    requiredAdministrationText(form, "organizationId", 64),
    positiveAdministrationInteger(form, "expectedRowVersion"),
  );
}

async function requireActiveResourceScope(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  resourceScopeId: string,
): Promise<void> {
  const scope = await session.client.accessControl.resourceScopes.get(context, resourceScopeId);
  if (!scope || scope.status !== 1) {
    throw new Error("Invalid administration input: select an active ResourceScope in this tenant and application.");
  }
}

function optionalText(form: FormData, field: string, maxLength: number): string | undefined {
  const value = form.get(field);
  if (value === null || value === "") return undefined;
  if (typeof value !== "string") throw new Error(`Invalid administration input: ${field} is invalid.`);
  const normalized = value.trim();
  if (!normalized) return undefined;
  if (normalized.length > maxLength) throw new Error(`Invalid administration input: ${field} is too long.`);
  return normalized;
}

function slug(form: FormData, field: string, maxLength: number): string {
  const value = requiredAdministrationText(form, field, maxLength);
  if (!/^[a-z][a-z0-9-]{0,63}$/.test(value)) {
    throw new Error(`Invalid administration input: ${field} must be a lowercase stable key.`);
  }
  return value;
}
