import "server-only";

import {
  createAuthorizationContext,
  type GenericIdentityAuthorizationContext,
} from "@generic-identity/auth";
import type {
  IdentityAuthorizationBoundary,
  IdentityCapabilityRequirement,
} from "@generic-identity/contracts";
import { NextIdentityServerSession } from "./session";

export class NextIdentityPermissionDeniedError extends Error {
  public constructor() {
    super("The Generic Identity capability check was denied.");
    this.name = "NextIdentityPermissionDeniedError";
  }
}

export async function createNextServerAuthorizationContext(
  session: NextIdentityServerSession,
  boundary: IdentityAuthorizationBoundary,
): Promise<GenericIdentityAuthorizationContext> {
  const credential = session.requireCredential();
  return createAuthorizationContext(session.client, {
    identityScopeId: boundary.identityScopeId,
    applicationKey: boundary.applicationKey,
    ...(boundary.tenantId === undefined ? {} : { tenantId: boundary.tenantId }),
    ...(boundary.resourceScopeId === undefined ? {} : { resourceScopeId: boundary.resourceScopeId }),
    credential,
  });
}

export async function isAllowedOnServer(
  session: NextIdentityServerSession,
  boundary: IdentityAuthorizationBoundary,
  requirement: IdentityCapabilityRequirement,
  signal?: AbortSignal,
): Promise<boolean> {
  const authorization = await createNextServerAuthorizationContext(session, boundary);
  return authorization.isAllowedRequirement(requirement, signal);
}

/**
 * Throws only on explicit authorization DENY. Technical failures from the
 * authorization service are deliberately allowed to propagate unchanged.
 */
export async function requireServerCapability(
  session: NextIdentityServerSession,
  boundary: IdentityAuthorizationBoundary,
  requirement: IdentityCapabilityRequirement,
  signal?: AbortSignal,
): Promise<void> {
  const allowed = await isAllowedOnServer(session, boundary, requirement, signal);
  if (!allowed) throw new NextIdentityPermissionDeniedError();
}
