import "server-only";
import { IdentityAccessClientError } from "@identity-access/client";
import type { AdminActionFailure } from "../contracts/AdminActionState";
import { IdentityAccessAdminFailurePresentation } from "./IdentityAccessAdminFailurePresentation";

/** Adds manifest-registration-specific recovery guidance without exposing raw server details. */
export class IdentityAccessAdminSecurityModelFailurePresentation {
  public static fromRegistration(error: unknown): AdminActionFailure {
    if (error instanceof IdentityAccessClientError && error.code === "http" && error.httpStatus === 409) {
      return {
        kind: "conflict",
        title: "Security model version already exists",
        message: "This modelVersion is already registered with different normalized semantics. Increment modelVersion in the project-owned manifest and register the new immutable version.",
        recovery: "edit",
      };
    }

    return IdentityAccessAdminFailurePresentation.fromMutation(error);
  }
}
