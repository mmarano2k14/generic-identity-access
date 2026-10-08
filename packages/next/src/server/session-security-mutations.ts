import "server-only";

import type { IdentityAdministrationContext } from "@generic-identity/auth";
import type { IdentitySessionRevocationResult } from "@generic-identity/contracts";
import { requiredAdministrationText } from "./administration-form";
import { NextIdentityServerSession } from "./session";

export async function revokeNextUserSessionsFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentitySessionRevocationResult> {
  requireLiteralConfirmation(form, "REVOKE");
  return session.client.security.sessions.revokeUser(
    context,
    requiredAdministrationText(form, "userId", 64),
  );
}

export async function revokeNextClientSessionsFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  form: FormData,
): Promise<IdentitySessionRevocationResult> {
  requireLiteralConfirmation(form, "REVOKE");
  return session.client.security.sessions.revokeClient(
    context,
    requiredAdministrationText(form, "clientId", 128),
  );
}

function requireLiteralConfirmation(form: FormData, expected: string): void {
  if (form.get("confirmation") !== expected) {
    throw new Error(`Invalid administration input: confirmation must be ${expected}.`);
  }
}
