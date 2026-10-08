import "server-only";

import type { IdentityAdministrationContext } from "@generic-identity/auth";
import type {
  IdentityScopeAuthorityGroupRecord,
  IdentityScopeAuthorityMemberRecord,
  IdentityScopeAuthorityPolicyBindingRecord,
  IdentityScopeAuthorityPolicyRecord,
  IdentityScopeAuthorityPolicyStatementRecord,
} from "@generic-identity/contracts";
import {
  administrationLifecycleStatus,
  positiveAdministrationInteger,
  requiredAdministrationText,
} from "./administration-form";
import { NextIdentityServerSession } from "./session";

export async function createNextScopeAuthorityGroupFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentityScopeAuthorityGroupRecord> {
  return session.client.accessControl.delegatedAuthority.createGroup(context, {
    displayName: requiredAdministrationText(form, "displayName", 200),
    status: administrationLifecycleStatus(form, "status"),
  });
}

export async function updateNextScopeAuthorityGroupFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentityScopeAuthorityGroupRecord> {
  return session.client.accessControl.delegatedAuthority.updateGroup(
    context,
    requiredAdministrationText(form, "groupId", 64),
    {
      displayName: requiredAdministrationText(form, "displayName", 200),
      status: administrationLifecycleStatus(form, "status"),
      expectedVersion: positiveAdministrationInteger(form, "expectedVersion"),
    },
  );
}

export async function createNextScopeAuthorityPolicyFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentityScopeAuthorityPolicyRecord> {
  return session.client.accessControl.delegatedAuthority.createPolicy(context, {
    displayName: requiredAdministrationText(form, "displayName", 200),
    status: administrationLifecycleStatus(form, "status"),
  });
}

export async function updateNextScopeAuthorityPolicyFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentityScopeAuthorityPolicyRecord> {
  return session.client.accessControl.delegatedAuthority.updatePolicy(
    context,
    requiredAdministrationText(form, "policyId", 64),
    {
      displayName: requiredAdministrationText(form, "displayName", 200),
      status: administrationLifecycleStatus(form, "status"),
      expectedVersion: positiveAdministrationInteger(form, "expectedVersion"),
    },
  );
}

export async function addNextScopeAuthorityMemberFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentityScopeAuthorityMemberRecord> {
  const groupId = requiredAdministrationText(form, "groupId", 64);
  const userId = requiredAdministrationText(form, "userId", 64);
  const [group, user] = await Promise.all([
    session.client.accessControl.delegatedAuthority.getGroup(context, groupId),
    session.client.directory.users.get(context, userId),
  ]);
  if (!group || group.status !== 1) throw new Error("Invalid administration input: authority group is unavailable or inactive.");
  if (!user || user.status !== 1) throw new Error("Invalid administration input: selected user is unavailable or inactive.");
  return session.client.accessControl.delegatedAuthority.addMember(context, groupId, userId);
}

export async function removeNextScopeAuthorityMemberFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<boolean> {
  requireLiteralConfirmation(form, "REMOVE");
  return session.client.accessControl.delegatedAuthority.removeMember(
    context,
    requiredAdministrationText(form, "groupId", 64),
    requiredAdministrationText(form, "userId", 64),
  );
}

export async function addNextScopeAuthorityPolicyStatementFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentityScopeAuthorityPolicyStatementRecord> {
  const policyId = requiredAdministrationText(form, "policyId", 64);
  const policy = await session.client.accessControl.delegatedAuthority.getPolicy(context, policyId);
  if (!policy || policy.status !== 1) throw new Error("Invalid administration input: authority policy is unavailable or inactive.");

  return session.client.accessControl.delegatedAuthority.addPolicyStatement(context, policyId, {
    modelVersion: positiveAdministrationInteger(form, "modelVersion"),
    resource: requiredAdministrationText(form, "resource", 128),
    feature: requiredAdministrationText(form, "feature", 128),
    action: requiredAdministrationText(form, "action", 128),
  });
}

export async function removeNextScopeAuthorityPolicyStatementFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<boolean> {
  requireLiteralConfirmation(form, "REMOVE");
  return session.client.accessControl.delegatedAuthority.removePolicyStatement(
    context,
    requiredAdministrationText(form, "policyId", 64),
    requiredAdministrationText(form, "statementId", 64),
  );
}

export async function addNextScopeAuthorityPolicyBindingFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentityScopeAuthorityPolicyBindingRecord> {
  const groupId = requiredAdministrationText(form, "groupId", 64);
  const policyId = requiredAdministrationText(form, "policyId", 64);
  const [group, policy] = await Promise.all([
    session.client.accessControl.delegatedAuthority.getGroup(context, groupId),
    session.client.accessControl.delegatedAuthority.getPolicy(context, policyId),
  ]);
  if (!group || group.status !== 1) throw new Error("Invalid administration input: authority group is unavailable or inactive.");
  if (!policy || policy.status !== 1) throw new Error("Invalid administration input: authority policy is unavailable or inactive.");
  return session.client.accessControl.delegatedAuthority.addPolicyBinding(context, groupId, policyId);
}

export async function removeNextScopeAuthorityPolicyBindingFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<boolean> {
  requireLiteralConfirmation(form, "REMOVE");
  return session.client.accessControl.delegatedAuthority.removePolicyBinding(
    context,
    requiredAdministrationText(form, "groupId", 64),
    requiredAdministrationText(form, "policyId", 64),
  );
}

function requireLiteralConfirmation(form: FormData, expected: string): void {
  const value = form.get("confirmation");
  if (value !== expected) throw new Error(`Invalid administration input: confirmation must be ${expected}.`);
}
