/** Safe failure categories exposed to administration Client Components. */
export type AdminActionFailureKind =
  | "validation"
  | "unauthenticated"
  | "forbidden"
  | "conflict"
  | "unavailable"
  | "timeout"
  | "transport"
  | "protocol"
  | "configuration"
  | "cancelled"
  | "unexpected";

/** Presentation-only recovery guidance. Authorization and mutation decisions remain server-owned. */
export type AdminActionRecovery = "edit" | "retry" | "reload" | "sign-in" | "operator" | "none";

export interface AdminActionFailure {
  readonly kind: AdminActionFailureKind;
  readonly title: string;
  readonly message: string;
  readonly recovery: AdminActionRecovery;
}

/** Serializable state returned by Next.js administration server actions. */
export interface AdminActionState {
  readonly status: "idle" | "success" | "error";
  readonly message?: string;
  readonly failure?: AdminActionFailure;
}

export const INITIAL_ADMIN_ACTION_STATE: AdminActionState = Object.freeze({ status: "idle" });
