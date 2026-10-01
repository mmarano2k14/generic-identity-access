"use client";

import { useMemo, type ReactNode } from "react";
import type {
  GenericIdentityAuthorizationContext,
  GenericIdentityClient,
} from "@generic-identity/auth";
import {
  IdentityReactContext,
  type IdentityReactContextValue,
} from "../internal/IdentityContext";
import { IdentityVisualContext } from "../internal/IdentityVisualContext";
import {
  emptyIdentityComponentOverrides,
  type IdentityComponentOverrides,
} from "../visual/types";

export interface IdentityProviderProps {
  readonly client: GenericIdentityClient;
  readonly authorizationContext?: GenericIdentityAuthorizationContext | null;
  readonly components?: IdentityComponentOverrides;
  readonly children: ReactNode;
}

/**
 * Supplies one already-configured Generic Identity client to a React subtree.
 *
 * The provider owns no authentication/session persistence and performs no
 * permission evaluation locally. A request/session-scoped authorization
 * context can be supplied by the host when protected UI is required.
 *
 * `components` may replace visual rendering primitives only. It cannot change
 * authentication, authorization, session, MFA, policy or TRN semantics.
 */
export function IdentityProvider({
  client,
  authorizationContext = null,
  components = emptyIdentityComponentOverrides,
  children,
}: IdentityProviderProps) {
  const value = useMemo<IdentityReactContextValue>(
    () => ({ client, authorizationContext }),
    [client, authorizationContext],
  );

  return (
    <IdentityVisualContext.Provider value={components}>
      <IdentityReactContext.Provider value={value}>{children}</IdentityReactContext.Provider>
    </IdentityVisualContext.Provider>
  );
}
