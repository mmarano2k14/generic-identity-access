"use server";

import { redirect } from "next/navigation";
import type { LoginActionState } from "../../contracts/LoginActionState";
import { IdentityAccessHostSessionService } from "../../server/IdentityAccessHostSessionService";

export async function loginAction(_state: LoginActionState, formData: FormData): Promise<LoginActionState> {
  const loginIdentifier = formData.get("loginIdentifier");
  const password = formData.get("password");

  if (typeof loginIdentifier !== "string" || typeof password !== "string") {
    return { status: "error", message: "Login identifier and password are required." };
  }

  try {
    const session = await IdentityAccessHostSessionService.fromCurrentRequest();
    await session.signIn(loginIdentifier, password);
  } catch (error) {
    return { status: "error", message: IdentityAccessHostSessionService.publicLoginErrorMessage(error) };
  }

  redirect("/identity");
}

export async function logoutAction(): Promise<void> {
  const session = await IdentityAccessHostSessionService.fromCurrentRequest();
  await session.signOut();
  redirect("/login");
}
