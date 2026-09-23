/** Serializable state returned by Next.js administration server actions. */
export interface AdminActionState {
  readonly status: "idle" | "success" | "error";
  readonly message?: string;
}

export const INITIAL_ADMIN_ACTION_STATE: AdminActionState = Object.freeze({ status: "idle" });
