import "server-only";

import type { IdentityAdministrationContext } from "@generic-identity/auth";
import type {
  IdentityManagedPolicyRecord,
  IdentityManagedPolicyStatementRecord,
  IdentityManagedPolicyVersionRecord,
} from "@generic-identity/contracts";
import {
  administrationLifecycleStatus,
  positiveAdministrationInteger,
  requiredAdministrationText,
} from "./administration-form";
import { NextIdentityServerSession } from "./session";

export async function createNextManagedPolicyFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentityManagedPolicyRecord> {
  return session.client.accessControl.managedPolicies.create(context, {
    policyKey: managedPolicyKey(form, "policyKey"),
    displayName: requiredAdministrationText(form, "displayName", 200),
    status: administrationLifecycleStatus(form, "status"),
  });
}

export async function updateNextManagedPolicyFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentityManagedPolicyRecord> {
  return session.client.accessControl.managedPolicies.update(
    context,
    requiredAdministrationText(form, "policyId", 64),
    {
      policyKey: managedPolicyKey(form, "policyKey"),
      displayName: requiredAdministrationText(form, "displayName", 200),
      status: administrationLifecycleStatus(form, "status"),
      expectedVersion: positiveAdministrationInteger(form, "expectedVersion"),
    },
  );
}

export async function createNextManagedPolicyVersionFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentityManagedPolicyVersionRecord> {
  const policyId = requiredAdministrationText(form, "policyId", 64);
  const modelVersion = positiveAdministrationInteger(form, "modelVersion");
  const model = await session.client.applicationSecurity.models.get(context, modelVersion);
  if (!model) throw new Error("Invalid administration input: selected security model was not found.");

  return session.client.accessControl.managedPolicies.createVersion(context, policyId, {
    policyVersion: positiveAdministrationInteger(form, "policyVersion"),
    modelVersion,
  });
}

export async function publishNextManagedPolicyVersionFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentityManagedPolicyVersionRecord> {
  return session.client.accessControl.managedPolicies.publishVersion(
    context,
    requiredAdministrationText(form, "policyId", 64),
    positiveAdministrationInteger(form, "policyVersion"),
    { makeDefault: checkbox(form, "makeDefault") },
  );
}

/** Add a draft statement only from the registered capability catalog. */
export async function addNextManagedPolicyStatementFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentityManagedPolicyStatementRecord> {
  const selected = requiredAdministrationText(form, "capability", 512);
  const parts = selected.split("|");
  if (parts.length !== 4 || parts.some((part) => part.trim() === "")) {
    throw new Error("Invalid administration input: capability selection is invalid.");
  }

  const selectedModelVersion = Number(parts[0]);
  if (!Number.isSafeInteger(selectedModelVersion) || selectedModelVersion <= 0) {
    throw new Error("Invalid administration input: capability model version is invalid.");
  }

  const policyId = requiredAdministrationText(form, "policyId", 64);
  const policyVersion = positiveAdministrationInteger(form, "policyVersion");
  const version = await session.client.accessControl.managedPolicies.getVersion(context, policyId, policyVersion);
  if (!version) throw new Error("Invalid administration input: managed policy version was not found.");
  if (version.publishedAt !== undefined) {
    throw new Error("Invalid administration input: published policy versions are immutable.");
  }
  if (version.modelVersion !== selectedModelVersion) {
    throw new Error("Invalid administration input: capability does not belong to the policy version security model.");
  }

  return session.client.accessControl.managedPolicies.addStatement(
    context,
    policyId,
    policyVersion,
    { resource: parts[1]!, feature: parts[2]!, action: parts[3]! },
  );
}

export async function removeNextManagedPolicyStatementFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<boolean> {
  requireLiteralConfirmation(form, "REMOVE");
  return session.client.accessControl.managedPolicies.removeStatement(
    context,
    requiredAdministrationText(form, "policyId", 64),
    positiveAdministrationInteger(form, "policyVersion"),
    requiredAdministrationText(form, "statementId", 64),
  );
}

function managedPolicyKey(form: FormData, field: string): string {
  const value = requiredAdministrationText(form, field, 128).toLowerCase();
  if (!/^[a-z][a-z0-9-]{0,127}$/u.test(value)) {
    throw new Error(
      `Invalid administration input: ${field} must use lower-case letters, digits, and hyphens and must start with a letter.`,
    );
  }
  return value;
}

function checkbox(form: FormData, field: string): boolean {
  return form.getAll(field).some((value) => value === "true" || value === "on" || value === "1");
}

function requireLiteralConfirmation(form: FormData, expected: string): void {
  const value = form.get("confirmation");
  if (value !== expected) throw new Error(`Invalid administration input: confirmation must be ${expected}.`);
}
