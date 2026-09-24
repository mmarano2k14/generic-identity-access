"use server";

import { redirect } from "next/navigation";
import type { RecoveryActionState } from "../../contracts/RecoveryActionState";
import { IdentityAccessHostSessionService } from "../../server/IdentityAccessHostSessionService";

export async function recoverPasswordAction(_state: RecoveryActionState, formData: FormData): Promise<RecoveryActionState> {
  const loginIdentifier = formData.get("loginIdentifier");
  const recoveryCode = formData.get("recoveryCode");
  const newPassword = formData.get("newPassword");
  const confirmPassword = formData.get("confirmPassword");

  if (
    typeof loginIdentifier !== "string" ||
    typeof recoveryCode !== "string" ||
    typeof newPassword !== "string" ||
    typeof confirmPassword !== "string"
  ) {
    return { status: "error", message: "All recovery fields are required." };
  }

  if (newPassword !== confirmPassword) {
    return { status: "error", message: "The new password confirmation does not match." };
  }

  if (newPassword.length < 12 || newPassword.length > 256) {
    return { status: "error", message: "The new password must contain between 12 and 256 characters." };
  }

  try {
    const session = await IdentityAccessHostSessionService.fromCurrentRequest();
    await session.recoverPassword(loginIdentifier, recoveryCode, newPassword);
  } catch (error) {
    return { status: "error", message: IdentityAccessHostSessionService.publicRecoveryErrorMessage(error) };
  }

  redirect("/login?recovered=1");
}
