"use client";

import type { ReactNode } from "react";
import type {
  GenericIdentityAuthorizationContext,
  GenericIdentityClient,
} from "@generic-identity/auth";
import { IdentityProvider } from "@generic-identity/react/providers";
import type { IdentityComponentOverrides } from "@generic-identity/react/visual";

export interface NextIdentityProviderProps {
  readonly client: GenericIdentityClient;
  readonly authorizationContext?: GenericIdentityAuthorizationContext | null;
  readonly components?: IdentityComponentOverrides;
  readonly children: ReactNode;
}

/**
 * Establishes the explicit Next.js client-component boundary for the shared
 * React provider. Session credentials remain server-only and are never read by
 * this component.
 */
export function NextIdentityProvider({
  client,
  authorizationContext = null,
  components,
  children,
}: NextIdentityProviderProps) {
  return (
    <IdentityProvider
      client={client}
      authorizationContext={authorizationContext}
      {...(components === undefined ? {} : { components })}
    >
      {children}
    </IdentityProvider>
  );
}
