export interface LoginActionState {
  readonly status: "idle" | "error";
  readonly message?: string;
}

export const initialLoginActionState: LoginActionState = { status: "idle" };
