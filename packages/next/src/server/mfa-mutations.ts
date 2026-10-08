import "server-only";

import type { IdentityAdministrationContext } from "@generic-identity/auth";
import type {
  IdentityMfaPolicyMode,
  IdentityMfaPolicyRecord,
  IdentityUserAuthenticatorRecord,
} from "@generic-identity/contracts";
import {
  positiveAdministrationInteger,
  requiredAdministrationText,
} from "./administration-form";
import { NextIdentityServerSession } from "./session";

export async function createNextMfaPolicyFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentityMfaPolicyRecord> {
  return session.client.security.mfa.createPolicy(context, {
    mode: mfaPolicyMode(form, "mode"),
    allowedProviders: mfaProviderKeys(form),
  });
}

export async function updateNextMfaPolicyFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentityMfaPolicyRecord> {
  return session.client.security.mfa.updatePolicy(context, {
    mode: mfaPolicyMode(form, "mode"),
    allowedProviders: mfaProviderKeys(form),
    expectedVersion: positiveAdministrationInteger(form, "expectedVersion"),
  });
}

export async function revokeNextMfaAuthenticatorFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentityUserAuthenticatorRecord> {
  requireLiteralConfirmation(form, "REVOKE");
  return session.client.security.mfa.revokeAuthenticator(
    context,
    requiredAdministrationText(form, "userId", 64),
    requiredAdministrationText(form, "authenticatorId", 64),
    positiveAdministrationInteger(form, "expectedVersion"),
  );
}

export async function recoveryRevokeNextMfaAuthenticatorFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentityUserAuthenticatorRecord> {
  requireLiteralConfirmation(form, "REVOKE");
  return session.client.security.mfa.revokeAuthenticatorForRecovery(
    context,
    requiredAdministrationText(form, "userId", 64),
    requiredAdministrationText(form, "authenticatorId", 64),
    positiveAdministrationInteger(form, "expectedVersion"),
  );
}

function mfaPolicyMode(form: FormData, field: string): IdentityMfaPolicyMode {
  const value = form.get(field);
  if (value === "1") return 1;
  if (value === "2") return 2;
  if (value === "3") return 3;
  throw new Error(`Invalid administration input: ${field} must be Disabled, Optional, or Required.`);
}

function mfaProviderKeys(form: FormData): readonly string[] {
  const values = form.getAll("allowedProviders").map((value) => {
    if (typeof value !== "string") {
      throw new Error("Invalid administration input: allowedProviders contains an invalid value.");
    }
    const normalized = value.trim().toLowerCase();
    if (!/^[a-z][a-z0-9-]{0,127}$/.test(normalized)) {
      throw new Error("Invalid administration input: allowedProviders contains an invalid provider key.");
    }
    return normalized;
  });

  return [...new Set(values)];
}

function requireLiteralConfirmation(form: FormData, expected: string): void {
  if (form.get("confirmation") !== expected) {
    throw new Error(`Invalid administration input: confirmation must be ${expected}.`);
  }
}
