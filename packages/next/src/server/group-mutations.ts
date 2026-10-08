import "server-only";

import {
  GenericIdentityClientError,
  type IdentityAdministrationContext,
  type IdentityTenantAdministrationContext,
} from "@generic-identity/auth";
import type {
  IdentityEffectiveAdministrationContext,
  IdentityGroupMemberRecord,
  IdentityGroupRecord,
  IdentityManagedGroupPolicyBindingRecord,
} from "@generic-identity/contracts";
import {
  administrationLifecycleStatus,
  positiveAdministrationInteger,
  requiredAdministrationText,
} from "./administration-form";
import { isAllowedOnServer } from "./authorization";
import { NextIdentityServerSession } from "./session";

const TEMPLATE_LIMIT = 100;
const RESOURCE_SCOPE_LIMIT = 200;

export async function createNextGroupFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<IdentityGroupRecord> {
  return session.client.accessControl.groups.create(context, {
    displayName: requiredAdministrationText(form, "displayName", 200),
    status: administrationLifecycleStatus(form, "status"),
  });
}

/** Tenant-local edit. Reusable source groups stay scope-admin controlled. */
export async function updateNextGroupFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  form: FormData,
): Promise<IdentityGroupRecord> {
  assertTenantContext(context, effective);
  const groupId = requiredAdministrationText(form, "groupId", 64);
  const current = await session.client.accessControl.groups.get(context, groupId);
  if (!current || current.tenantId !== context.tenantId) throw new GenericIdentityClientError("forbidden", 403);
  if (current.isTemplate && effective.tenantVisibility !== "scope-wide") {
    throw new GenericIdentityClientError("forbidden", 403);
  }
  return session.client.accessControl.groups.update(context, groupId, {
    displayName: requiredAdministrationText(form, "displayName", 200),
    status: administrationLifecycleStatus(form, "status"),
    expectedVersion: positiveAdministrationInteger(form, "expectedVersion"),
  });
}

/** Scope authority may promote/demote a real tenant group as reusable. */
export async function updateNextReusableGroupFromForm(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  form: FormData,
): Promise<IdentityGroupRecord> {
  assertEffectiveContext(administration, effective);
  if (effective.tenantVisibility !== "scope-wide") throw new GenericIdentityClientError("forbidden", 403);
  await requireScopeCapability(session, administration, "group", "write");
  return session.client.accessControl.groups.updateReusable(
    administration,
    requiredAdministrationText(form, "tenantId", 64),
    requiredAdministrationText(form, "groupId", 64),
    {
      displayName: requiredAdministrationText(form, "displayName", 200),
      status: administrationLifecycleStatus(form, "status"),
      isTemplate: checkbox(form, "isTemplate"),
      expectedVersion: positiveAdministrationInteger(form, "expectedVersion"),
    },
  );
}

/** Clone reusable definition only. Members are never copied. */
export async function createNextGroupFromTemplateFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<IdentityGroupRecord> {
  const source = requiredAdministrationText(form, "sourceGroup", 160);
  const separator = source.indexOf(":");
  if (separator <= 0 || separator === source.length - 1) {
    throw new Error("Invalid administration input: reusable group selection is invalid.");
  }
  const sourceTenantId = source.slice(0, separator);
  const sourceGroupId = source.slice(separator + 1);

  const templates = await session.client.accessControl.groups.listTemplates(context, { limit: TEMPLATE_LIMIT });
  const template = templates.find((candidate) =>
    candidate.tenantId === sourceTenantId && candidate.groupId === sourceGroupId && candidate.status === 1 && candidate.isTemplate);
  if (!template) throw new Error("Invalid administration input: selected reusable group is unavailable.");

  const requirements = await session.client.accessControl.groups.listTemplateScopeRequirements(
    context,
    sourceTenantId,
    sourceGroupId,
  );
  const scopes = requirements.length > 0
    ? await session.client.accessControl.resourceScopes.list(context, { limit: RESOURCE_SCOPE_LIMIT })
    : [];
  if (requirements.length > 0 && scopes.length >= RESOURCE_SCOPE_LIMIT) {
    throw new Error("Invalid administration input: too many resource scopes to safely map this template.");
  }
  const scopesById = new Map(scopes.map((scope) => [scope.resourceScopeId, scope]));
  const resourceScopeMappings = requirements.map((requirement) => {
    const targetResourceScopeId = requiredAdministrationText(
      form,
      `resourceScopeMapping:${requirement.sourceResourceScopeId}`,
      64,
    );
    const target = scopesById.get(targetResourceScopeId);
    if (!target || target.status !== 1 || target.modelVersion !== requirement.modelVersion || target.scopeType !== requirement.scopeType) {
      throw new Error("Invalid administration input: selected target resource scope is incompatible with the reusable group binding.");
    }
    return {
      sourceResourceScopeId: requirement.sourceResourceScopeId,
      targetResourceScopeId,
    };
  });

  return session.client.accessControl.groups.createFromTemplate(context, {
    sourceTenantId,
    sourceGroupId,
    resourceScopeMappings,
  });
}

/** Add only one active membership from the same tenant. */
export async function addNextGroupMemberFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<IdentityGroupMemberRecord> {
  const groupId = requiredAdministrationText(form, "groupId", 64);
  const tenantMembershipId = requiredAdministrationText(form, "tenantMembershipId", 64);
  const [group, membership] = await Promise.all([
    session.client.accessControl.groups.get(context, groupId),
    session.client.directory.memberships.get(context, tenantMembershipId),
  ]);
  if (!group || group.tenantId !== context.tenantId || group.status !== 1) {
    throw new Error("Invalid administration input: selected group is unavailable or inactive.");
  }
  if (!membership || membership.tenantId !== context.tenantId || membership.status !== 1) {
    throw new Error("Invalid administration input: selected tenant member is unavailable or inactive.");
  }
  return session.client.accessControl.groups.addMember(context, groupId, tenantMembershipId);
}

export async function removeNextGroupMemberFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<boolean> {
  requireLiteralConfirmation(form, "REMOVE");
  return session.client.accessControl.groups.removeMember(
    context,
    requiredAdministrationText(form, "groupId", 64),
    requiredAdministrationText(form, "tenantMembershipId", 64),
  );
}

/** Bind only an active managed policy with a published default version. */
export async function addNextManagedGroupPolicyBindingFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<IdentityManagedGroupPolicyBindingRecord> {
  const groupId = requiredAdministrationText(form, "groupId", 64);
  const policyId = requiredAdministrationText(form, "managedPolicyId", 64);
  const resourceScopeId = optionalText(form, "resourceScopeId", 64);

  const [group, policies, resourceScope] = await Promise.all([
    session.client.accessControl.groups.get(context, groupId),
    session.client.accessControl.managedPolicyBindings.listAvailablePolicies(context, { search: policyId, limit: 20 }),
    resourceScopeId
      ? session.client.accessControl.resourceScopes.get(context, resourceScopeId)
      : Promise.resolve(null),
  ]);
  const policy = policies.find((candidate) => candidate.policyId === policyId);
  if (!group || group.tenantId !== context.tenantId || group.status !== 1) {
    throw new Error("Invalid administration input: selected group is unavailable or inactive.");
  }
  if (!policy || policy.status !== 1 || policy.defaultVersion === undefined) {
    throw new Error("Invalid administration input: selected managed policy has no active published default version.");
  }
  if (resourceScopeId && (!resourceScope || resourceScope.status !== 1)) {
    throw new Error("Invalid administration input: selected resource scope is unavailable or inactive.");
  }

  return session.client.accessControl.managedPolicyBindings.add(context, groupId, {
    policyId,
    ...(resourceScopeId ? { resourceScopeId } : {}),
    includeDescendants: checkbox(form, "includeDescendants"),
  });
}

export async function removeNextManagedGroupPolicyBindingFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<boolean> {
  requireLiteralConfirmation(form, "REMOVE");
  return session.client.accessControl.managedPolicyBindings.remove(
    context,
    requiredAdministrationText(form, "groupId", 64),
    requiredAdministrationText(form, "managedPolicyId", 64),
    positiveAdministrationInteger(form, "policyVersion"),
    optionalText(form, "resourceScopeId", 64),
  );
}

function assertTenantContext(
  context: IdentityTenantAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
): void {
  if (context.identityScopeId !== effective.identityScopeId || context.applicationKey !== effective.applicationKey) {
    throw new GenericIdentityClientError("configuration");
  }
  if (
    effective.tenantVisibility !== "scope-wide"
    && !effective.activeTenantMemberships.some((membership) => membership.tenantId === context.tenantId)
  ) throw new GenericIdentityClientError("forbidden", 403);
}

function assertEffectiveContext(
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
): void {
  if (administration.identityScopeId !== effective.identityScopeId || administration.applicationKey !== effective.applicationKey) {
    throw new GenericIdentityClientError("configuration");
  }
}

async function requireScopeCapability(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  feature: string,
  action: string,
): Promise<void> {
  const allowed = await isAllowedOnServer(
    session,
    { identityScopeId: context.identityScopeId, applicationKey: context.applicationKey },
    { resource: "identity-access", feature, action },
  );
  if (!allowed) throw new GenericIdentityClientError("forbidden", 403);
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

function checkbox(form: FormData, field: string): boolean {
  return form.getAll(field).some((value) => value === "true" || value === "on" || value === "1");
}

function requireLiteralConfirmation(form: FormData, expected: string): void {
  const value = form.get("confirmation");
  if (value !== expected) throw new Error(`Invalid administration input: confirmation must be ${expected}.`);
}
