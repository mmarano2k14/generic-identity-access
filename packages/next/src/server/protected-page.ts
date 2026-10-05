import "server-only";

import { GenericIdentityClientError } from "@generic-identity/auth";
import type { IdentitySessionValidationResult } from "@generic-identity/contracts";
import { NextIdentityServerSession } from "./session";

/**
 * Server-side protected-page helper. It performs no redirect because public
 * routes remain owned by the consuming application.
 */
export async function requireAuthenticatedIdentitySession(
  session: NextIdentityServerSession,
): Promise<IdentitySessionValidationResult> {
  const current = await session.current();
  if (current === null) {
    throw new GenericIdentityClientError("unauthenticated", 401);
  }
  return current;
}
