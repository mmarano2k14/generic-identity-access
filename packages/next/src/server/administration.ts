import "server-only";

import type {
  IdentityAdministrationContext,
  IdentityTenantAdministrationContext,
} from "@generic-identity/auth";
import { NextIdentityServerSession } from "./session";

/** Builds the trusted administration context from the current server session. */
export function createNextAdministrationContext(
  session: NextIdentityServerSession,
  identityScopeId: string,
  applicationKey: string,
): IdentityAdministrationContext {
  return {
    identityScopeId: required(identityScopeId, "identityScopeId"),
    applicationKey: required(applicationKey, "applicationKey"),
    credential: session.requireCredential(),
  };
}

/** Builds a tenant-scoped administration context without exposing credentials. */
export function createNextTenantAdministrationContext(
  session: NextIdentityServerSession,
  identityScopeId: string,
  applicationKey: string,
  tenantId: string,
): IdentityTenantAdministrationContext {
  return {
    ...createNextAdministrationContext(session, identityScopeId, applicationKey),
    tenantId: required(tenantId, "tenantId"),
  };
}

function required(value: string, name: string): string {
  const normalized = value.trim();
  if (!normalized) throw new Error(`Generic Identity '${name}' is required.`);
  return normalized;
}
