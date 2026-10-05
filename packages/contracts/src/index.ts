/**
 * Passive public contracts for the shared Generic Identity boundary.
 *
 * The package exposes proven public TypeScript contract shapes through stable
 * Generic Identity names. Every export here is type-only; authentication
 * credentials, secrets and runtime behavior are excluded.
 */
export type * from "./administration";
export type * from "./authorization";
export type * from "./errors";
export type * from "./identity";
export type * from "./mfa";
export type * from "./policies";
export type * from "./security-manifest";
export type * from "./session";

export type * from "./account/index";
export type * from "./directory/index";
export type * from "./organizations/index";
export type * from "./access-control/index";

export type * from "./application-security/index";

export type * from "./security-operations/index";
