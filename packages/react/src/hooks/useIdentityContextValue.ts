"use client";

import { useContext } from "react";
import { IdentityReactContext, type IdentityReactContextValue } from "../internal/IdentityContext";

export function useIdentityContextValue(): IdentityReactContextValue {
  const value = useContext(IdentityReactContext);
  if (value === null) {
    throw new Error("IdentityProvider is required for Generic Identity React hooks.");
  }

  return value;
}
