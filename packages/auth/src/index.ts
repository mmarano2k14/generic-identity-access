/**
 * Framework-neutral Shared Identity authentication and authorization SDK.
 *
 * This package is an additive facade over the proven TypeScript client. It does
 * not implement a second authentication system, authorization engine or TRN parser.
 */
export * from "./administration";
export * from "./authentication";
export * from "./authorization";
export * from "./authorization-context";
export * from "./client";
export * from "./errors";

export * from "./account";
export * from "./directory";
export * from "./organizations";
export * from "./access-control";

export * from "./application-security";

export * from "./security-operations";
