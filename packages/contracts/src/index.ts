/**
 * Passive public contracts for the shared Generic Identity boundary.
 *
 * Pack 2 deliberately exposes proven existing TypeScript contract shapes without
 * moving their authoritative legacy source yet. Every export in this package is
 * type-only; authentication credentials, secrets and runtime behavior are excluded.
 */
export type * from "./administration";
export type * from "./authorization";
export type * from "./errors";
export type * from "./identity";
export type * from "./mfa";
export type * from "./policies";
export type * from "./security-manifest";
export type * from "./session";
