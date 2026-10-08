import "server-only";

import type { IdentityAdministrationContext } from "@generic-identity/auth";
import type {
  IdentityApplicationSecurityModelRecord,
  IdentityScopeTypeRecord,
} from "@generic-identity/contracts/application-security";
import { requiredAdministrationText } from "./administration-form";
import { parseNextApplicationSecurityManifestFile } from "./security-manifest-parser";
import { NextIdentityServerSession } from "./session";

/**
 * Registers one immutable project-authored manifest against the trusted
 * application context. The JSON is a file upload, never a UI-created model.
 */
export async function registerNextApplicationSecurityManifestFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  formData: FormData,
): Promise<IdentityApplicationSecurityModelRecord> {
  const manifest = await parseNextApplicationSecurityManifestFile(formData);
  const configuredKey = context.applicationKey.trim().toLowerCase();
  if (manifest.applicationKey !== configuredKey) {
    throw new Error(
      `Invalid administration input: manifest applicationKey must match the configured application context (${configuredKey}).`,
    );
  }

  return session.client.applicationSecurity.manifests.register(context, manifest);
}

/**
 * Registers a root or child scope type for an existing security model.
 * canAttachToTenant is derived from the parent, not controlled by the browser.
 */
export async function addNextApplicationScopeTypeFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  modelVersion: number,
  formData: FormData,
): Promise<IdentityScopeTypeRecord> {
  if (!Number.isSafeInteger(modelVersion) || modelVersion <= 0) {
    throw new Error("Invalid administration input: modelVersion must be a positive integer.");
  }

  const key = scopeTypeKey(requiredAdministrationText(formData, "key", 64), "key");
  const displayName = requiredAdministrationText(formData, "displayName", 200);
  const parentKey = optionalScopeTypeKey(formData, "parentKey");
  if (parentKey === key) {
    throw new Error("Invalid administration input: a scope type cannot be its own parent.");
  }

  return session.client.applicationSecurity.scopeTypes.add(context, modelVersion, {
    key,
    displayName,
    ...(parentKey === undefined ? {} : { parentKey }),
    canAttachToTenant: parentKey === undefined,
  });
}

function scopeTypeKey(value: string, name: string): string {
  const normalized = value.toLowerCase();
  if (!/^[a-z][a-z0-9-]{0,63}$/u.test(normalized)) {
    throw new Error(
      `Invalid administration input: ${name} must use lower-case letters, digits, and hyphens and must start with a letter.`,
    );
  }
  return normalized;
}

function optionalScopeTypeKey(formData: FormData, name: string): string | undefined {
  const value = formData.get(name);
  if (value === null || value === undefined || value === "") return undefined;
  if (typeof value !== "string") {
    throw new Error(`Invalid administration input: ${name} is invalid.`);
  }
  const normalized = value.trim().toLowerCase();
  if (normalized.length === 0) return undefined;
  if (normalized.length > 64 || !/^[a-z][a-z0-9-]{0,63}$/u.test(normalized)) {
    throw new Error(`Invalid administration input: ${name} must be a registered scope-type key.`);
  }
  return normalized;
}
