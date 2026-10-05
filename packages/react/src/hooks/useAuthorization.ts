"use client";

import type { GenericIdentityAuthorizationContext } from "@generic-identity/auth";
import { useIdentityContextValue } from "./useIdentityContextValue";

/**
 * Returns the request/session-scoped server-backed authorization context.
 *
 * Hosts that render protected UI must provide this context explicitly through
 * IdentityProvider. Missing configuration is a technical/configuration error,
 * never an implicit permission denial.
 */
export function useAuthorization(): GenericIdentityAuthorizationContext {
  const authorizationContext = useIdentityContextValue().authorizationContext;
  if (authorizationContext === null) {
    throw new Error("IdentityProvider does not have an authorization context.");
  }

  return authorizationContext;
}
