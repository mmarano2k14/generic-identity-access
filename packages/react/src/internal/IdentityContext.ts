"use client";

import { createContext } from "react";
import type {
  GenericIdentityAuthorizationContext,
  GenericIdentityClient,
} from "@generic-identity/auth";

/** Internal React context value. Public consumers use hooks instead. */
export interface IdentityReactContextValue {
  readonly client: GenericIdentityClient;
  readonly authorizationContext: GenericIdentityAuthorizationContext | null;
}

export const IdentityReactContext = createContext<IdentityReactContextValue | null>(null);
