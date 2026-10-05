"use client";

import type { GenericIdentityClient } from "@generic-identity/auth";
import { useIdentityContextValue } from "./useIdentityContextValue";

/** Returns the shared framework-neutral Generic Identity client. */
export function useIdentityClient(): GenericIdentityClient {
  return useIdentityContextValue().client;
}
