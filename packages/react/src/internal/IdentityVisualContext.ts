"use client";

import { createContext } from "react";
import {
  emptyIdentityComponentOverrides,
  type IdentityComponentOverrides,
} from "../visual/types";

/**
 * Visual overrides intentionally have a safe empty default so presentation-only
 * shared pages can render without requiring an IdentityProvider.
 */
export const IdentityVisualContext = createContext<IdentityComponentOverrides>(
  emptyIdentityComponentOverrides,
);
