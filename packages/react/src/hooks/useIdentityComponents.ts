"use client";

import { useContext } from "react";
import { IdentityVisualContext } from "../internal/IdentityVisualContext";
import type { IdentityComponentOverrides } from "../visual/types";

/** Returns the optional visual overrides supplied by the nearest IdentityProvider. */
export function useIdentityComponents(): IdentityComponentOverrides {
  return useContext(IdentityVisualContext);
}
