import "server-only";

import type { IdentityAdministrationContext } from "@generic-identity/auth";
import type { IdentityUserRecord } from "@generic-identity/contracts/directory";
import type { IdentityPasswordCredentialMetadataRecord } from "@generic-identity/contracts/account";
import {
  administrationLifecycleStatus,
  positiveAdministrationInteger,
  requiredAdministrationText,
} from "./administration-form";
import { NextIdentityServerSession } from "./session";

/**
 * Scope-wide user creation. The Identity API remains the authorization authority.
 * Creating an identity does not create any tenant membership or grant.
 */
export async function createNextUserFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  formData: FormData,
): Promise<IdentityUserRecord> {
  return session.client.directory.users.create(context, {
    displayName: requiredAdministrationText(formData, "displayName", 200),
    status: administrationLifecycleStatus(formData, "status"),
  });
}

/** Preserve optimistic concurrency; the API validates the scope-wide capability. */
export async function updateNextUserFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  formData: FormData,
): Promise<IdentityUserRecord> {
  return session.client.directory.users.update(
    context,
    requiredAdministrationText(formData, "userId", 64),
    {
      displayName: requiredAdministrationText(formData, "displayName", 200),
      status: administrationLifecycleStatus(formData, "status"),
      expectedVersion: positiveAdministrationInteger(formData, "expectedVersion"),
    },
  );
}

/**
 * Creates a password credential for an existing identity. Never logs or returns
 * submitted password material. No implicit tenant/group membership is granted.
 */
export async function createNextPasswordCredentialFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  formData: FormData,
): Promise<IdentityPasswordCredentialMetadataRecord> {
  return session.client.account.passwordCredentials.create(
    context,
    requiredAdministrationText(formData, "userId", 64),
    {
      loginIdentifier: requiredAdministrationText(formData, "loginIdentifier", 320),
      password: requiredAdminPassword(formData),
    },
  );
}

/** Credential replacement revokes existing session/refresh-token continuity at the API. */
export async function changeNextPasswordCredentialFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  formData: FormData,
): Promise<IdentityPasswordCredentialMetadataRecord> {
  return session.client.account.passwordCredentials.changePassword(
    context,
    requiredAdministrationText(formData, "userId", 64),
    {
      loginIdentifier: requiredAdministrationText(formData, "loginIdentifier", 320),
      password: requiredAdminPassword(formData),
      expectedVersion: positiveAdministrationInteger(formData, "expectedVersion"),
    },
  );
}

function requiredAdminPassword(formData: FormData): string {
  const secret = formData.get("password");
  // Keep validation failures generic: never echo secret material into errors.
  if (typeof secret !== "string" || secret.length < 12 || secret.length > 256) {
    throw new Error("Invalid administration input: password must contain 12 to 256 characters.");
  }
  return secret;
}
