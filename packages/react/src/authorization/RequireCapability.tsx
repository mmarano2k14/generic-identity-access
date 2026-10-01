"use client";

import type { ReactNode } from "react";
import type { IdentityCapabilityRequirement } from "@generic-identity/contracts";
import { useCapability } from "../hooks/useCapability";

export interface RequireCapabilityProps {
  readonly requirement: IdentityCapabilityRequirement;
  readonly children: ReactNode;
  readonly loadingFallback?: ReactNode;
  readonly deniedFallback?: ReactNode;
  readonly errorFallback?: ReactNode | ((error: unknown) => ReactNode);
}

/**
 * React authorization gate backed by the server-side authorization authority.
 *
 * This component is an UX gate only. Protected backend operations must still
 * perform their own authorization check. Technical failures are surfaced as
 * errors rather than being silently converted into permission denials.
 */
export function RequireCapability({
  requirement,
  children,
  loadingFallback = null,
  deniedFallback = null,
  errorFallback,
}: RequireCapabilityProps) {
  const state = useCapability(requirement);

  switch (state.status) {
    case "allowed":
      return <>{children}</>;
    case "denied":
      return <>{deniedFallback}</>;
    case "idle":
    case "checking":
      return <>{loadingFallback}</>;
    case "error":
      if (typeof errorFallback === "function") {
        return <>{errorFallback(state.error)}</>;
      }
      if (errorFallback !== undefined) {
        return <>{errorFallback}</>;
      }
      throw state.error;
  }
}
