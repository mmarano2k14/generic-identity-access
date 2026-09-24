export interface RecoveryActionState {
  readonly status: "idle" | "error";
  readonly message?: string;
}

export const initialRecoveryActionState: RecoveryActionState = { status: "idle" };
