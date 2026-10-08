import "server-only";

import { GenericIdentityClientError, type IdentityTenantAdministrationContext } from "@generic-identity/auth";
import type { IdentityEffectiveAdministrationContext, IdentityTenantMembershipCandidateRecord, IdentityTenantMembershipRecord } from "@generic-identity/contracts";
import { administrationLifecycleStatus, positiveAdministrationInteger, requiredAdministrationText } from "./administration-form";
import { isAllowedOnServer } from "./authorization";
import { NextIdentityServerSession } from "./session";

const GROUP_LIMIT = 100;
const ORGANIZATION_LIMIT = 200;

/**
 * GOLDEN membership creation paths are distinct: scope-wide administrators may
 * name a user UUID; membership-limited administrators must resolve an existing
 * active identity through the exact login before adding that member.
 */
export async function createNextTenantMembershipFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  form: FormData,
): Promise<IdentityTenantMembershipRecord> {
  assertTenantContext(context, effective);
  const status = administrationLifecycleStatus(form, "status");

  if (effective.tenantVisibility === "scope-wide") {
    return session.client.directory.memberships.create(context, {
      userId: requiredAdministrationText(form, "userId", 64), status,
    });
  }

  const login = requiredAdministrationText(form, "loginIdentifier", 320);
  const candidate = await session.client.directory.membershipCandidates.findByLogin(context, login);
  if (!candidate || candidate.userStatus !== 1 || candidate.existingMembershipId !== undefined) {
    throw new Error("Invalid administration input: the login is not eligible for a new membership in this tenant.");
  }
  return session.client.directory.membershipCandidates.createMembershipByLogin(context, login, status);
}

/** Candidate preview: never accept an arbitrary user ID from a delegated user. */
export async function findNextTenantMembershipCandidateFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<IdentityTenantMembershipCandidateRecord | null> {
  await requireTenantCapability(session, context, "tenant-membership", "write");
  return session.client.directory.membershipCandidates.findByLogin(
    context, requiredAdministrationText(form, "loginIdentifier", 320),
  );
}

/** Preserve tenant-local optimistic concurrency for membership lifecycle. */
export async function updateNextTenantMembershipFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<IdentityTenantMembershipRecord> {
  return session.client.directory.memberships.update(
    context, requiredAdministrationText(form, "membershipId", 64), {
      status: administrationLifecycleStatus(form, "status"),
      expectedVersion: positiveAdministrationInteger(form, "expectedVersion"),
    },
  );
}

/** Reconcile a member's group selections against trusted tenant group records. */
export async function replaceNextTenantMemberGroupsFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<void> {
  await requireTenantCapability(session, context, "group", "read");
  await requireTenantCapability(session, context, "group-membership", "read");
  await requireTenantCapability(session, context, "group-membership", "write");

  const membershipId = requiredAdministrationText(form, "tenantMembershipId", 64);
  const membership = await session.client.directory.memberships.get(context, membershipId);
  if (membership?.tenantId !== context.tenantId) throw new GenericIdentityClientError("forbidden", 403);

  const [groups, assignments] = await Promise.all([
    session.client.accessControl.groups.list(context, { limit: GROUP_LIMIT }),
    session.client.directory.tenantGroupAssignments.list(context),
  ]);
  // A truncated view cannot safely be used for replacement.
  if (groups.length >= GROUP_LIMIT) throw new Error("Invalid administration input: too many groups to safely replace assignments.");
  const byId = new Map(groups.map((group) => [group.groupId, group]));
  const current = new Set(assignments
    .filter((assignment) => assignment.tenantMembershipId === membershipId)
    .map((assignment) => assignment.groupId));
  const desired = parseSelections(form, "groupSelection", "group");
  for (const groupId of desired) {
    const group = byId.get(groupId);
    // Preserve a previously assigned inactive group if unchanged; never allow
    // a *new* grant to an inactive group.
    if (!group || (group.status !== 1 && !current.has(groupId))) {
      throw new Error("Invalid administration input: selected tenant group is unavailable.");
    }
  }

  for (const groupId of current) {
    if (!desired.has(groupId)) await session.client.accessControl.groups.removeMember(context, groupId, membershipId);
  }
  for (const groupId of desired) {
    if (!current.has(groupId)) await session.client.accessControl.groups.addMember(context, groupId, membershipId);
  }
}

/** Reconcile belonging, including reactivation with the server's row version. */
export async function replaceNextTenantMemberOrganizationsFromForm(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  form: FormData,
): Promise<void> {
  await requireTenantCapability(session, context, "organization", "read");
  await requireTenantCapability(session, context, "organization-membership", "read");
  await requireTenantCapability(session, context, "organization-membership", "write");

  const membershipId = requiredAdministrationText(form, "tenantMembershipId", 64);
  const membership = await session.client.directory.memberships.get(context, membershipId);
  if (membership?.tenantId !== context.tenantId) throw new GenericIdentityClientError("forbidden", 403);

  const [organizations, currentMemberships] = await Promise.all([
    session.client.organizations.organizations.list(context, { limit: ORGANIZATION_LIMIT }),
    session.client.organizations.memberships.listForTenantMembership(context, membershipId, { limit: ORGANIZATION_LIMIT }),
  ]);
  if (organizations.length >= ORGANIZATION_LIMIT || currentMemberships.length >= ORGANIZATION_LIMIT) {
    throw new Error("Invalid administration input: too many Organizations to safely replace memberships.");
  }
  const byId = new Map(organizations.map((organization) => [organization.organizationId, organization]));
  const currentById = new Map(currentMemberships.map((item) => [item.organizationId, item]));
  const desired = parseSelections(form, "organizationSelection", "organization");

  for (const organizationId of desired) {
    const organization = byId.get(organizationId);
    const current = currentById.get(organizationId);
    if (!organization || (organization.status !== 1 && current?.status !== 1)) {
      throw new Error("Invalid administration input: selected Organization is unavailable or inactive.");
    }
  }

  for (const membershipRecord of currentMemberships) {
    if (!desired.has(membershipRecord.organizationId)) {
      await session.client.organizations.memberships.remove(
        context, membershipRecord.organizationId, membershipId, membershipRecord.rowVersion,
      );
    } else if (membershipRecord.status !== 1) {
      await session.client.organizations.memberships.activate(
        context, membershipRecord.organizationId, membershipId, membershipRecord.rowVersion,
      );
    }
  }
  for (const organizationId of desired) {
    if (!currentById.has(organizationId)) {
      await session.client.organizations.memberships.add(context, organizationId, membershipId);
    }
  }
}

function assertTenantContext(
  context: IdentityTenantAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
): void {
  if (context.identityScopeId !== effective.identityScopeId || context.applicationKey !== effective.applicationKey) {
    throw new GenericIdentityClientError("configuration");
  }
  if (effective.tenantVisibility !== "scope-wide" &&
      !effective.activeTenantMemberships.some((membership) => membership.tenantId === context.tenantId)) {
    throw new GenericIdentityClientError("forbidden", 403);
  }
}

async function requireTenantCapability(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  feature: string,
  action: string,
): Promise<void> {
  const allowed = await isAllowedOnServer(
    session,
    {
      identityScopeId: context.identityScopeId,
      applicationKey: context.applicationKey,
      tenantId: context.tenantId,
    },
    { resource: "identity-access", feature, action },
  );
  if (!allowed) throw new GenericIdentityClientError("forbidden", 403);
}

function parseSelections(form: FormData, field: string, prefix: string): Set<string> {
  const selected = new Set<string>();
  for (const value of form.getAll(field)) {
    if (typeof value !== "string" || value.length > 160 || !value.startsWith(`${prefix}:`)) {
      throw new Error(`Invalid administration input: ${field} contains an invalid selection.`);
    }
    const id = value.slice(prefix.length + 1);
    if (!id || id.length > 64) throw new Error(`Invalid administration input: ${field} contains an invalid selection.`);
    selected.add(id);
  }
  return selected;
}
