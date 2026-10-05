"use client";

import { useCallback, useEffect, useState } from "react";
import type { IdentityCapabilityRequirement } from "@generic-identity/contracts";
import { useAuthorization } from "./useAuthorization";

export type CapabilityCheckState =
  | Readonly<{ status: "idle"; allowed: null; error: null }>
  | Readonly<{ status: "checking"; allowed: null; error: null }>
  | Readonly<{ status: "allowed"; allowed: true; error: null }>
  | Readonly<{ status: "denied"; allowed: false; error: null }>
  | Readonly<{ status: "error"; allowed: null; error: unknown }>;

export interface UseCapabilityOptions {
  readonly enabled?: boolean;
}

export type UseCapabilityResult = CapabilityCheckState & Readonly<{
  refresh: () => void;
}>;

/**
 * Evaluates one capability through the server-backed authorization context.
 *
 * Technical failures remain `error`; they are never collapsed into `denied`.
 */
export function useCapability(
  requirement: IdentityCapabilityRequirement,
  options: UseCapabilityOptions = {},
): UseCapabilityResult {
  const authorization = useAuthorization();
  const enabled = options.enabled ?? true;
  const [revision, setRevision] = useState(0);
  const [state, setState] = useState<CapabilityCheckState>(() =>
    enabled
      ? { status: "checking", allowed: null, error: null }
      : { status: "idle", allowed: null, error: null },
  );

  const refresh = useCallback(() => {
    setRevision((current: number) => current + 1);
  }, []);

  useEffect(() => {
    if (!enabled) {
      setState({ status: "idle", allowed: null, error: null });
      return undefined;
    }

    const controller = new AbortController();
    setState({ status: "checking", allowed: null, error: null });

    void authorization
      .isAllowedRequirement(
        {
          resource: requirement.resource,
          feature: requirement.feature,
          action: requirement.action,
        },
        controller.signal,
      )
      .then((allowed) => {
        if (!controller.signal.aborted) {
          setState(
            allowed
              ? { status: "allowed", allowed: true, error: null }
              : { status: "denied", allowed: false, error: null },
          );
        }
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          setState({ status: "error", allowed: null, error });
        }
      });

    return () => {
      controller.abort();
    };
  }, [
    authorization,
    enabled,
    requirement.action,
    requirement.feature,
    requirement.resource,
    revision,
  ]);

  return { ...state, refresh };
}
