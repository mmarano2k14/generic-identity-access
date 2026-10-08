/** Selectable Generic Identity entity kinds exposed by the shared server-backed autocomplete. */
export type IdentityEntityReferenceKind =
  | "user"
  | "tenant"
  | "tenant-membership"
  | "managed-policy"
  | "resource-scope"
  | "authority-group"
  | "authority-policy";

/** Stable identifier/display projection returned by bounded entity-reference searches. */
export interface IdentityEntityReferenceOption {
  readonly id: string;
  readonly displayName: string;
  readonly description?: string;
  readonly keywords?: readonly string[];
}
