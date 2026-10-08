import "server-only";

import { GenericIdentityClientError } from "@generic-identity/auth";

export type NextIdentityMutationFailureKind =
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

export type NextIdentityMutationRecovery =
  | "edit"
  | "retry"
  | "reload"
  | "sign-in"
  | "operator"
  | "none";

export interface NextIdentityMutationFailure {
  readonly kind: NextIdentityMutationFailureKind;
  readonly title: string;
  readonly message: string;
  readonly recovery: NextIdentityMutationRecovery;
}

/**
 * Maps a failed protected administration mutation to bounded, secret-safe UI
 * guidance. No raw response body, credential, URL, or transport cause is
 * surfaced to the consuming application.
 */
export function presentNextIdentityMutationFailure(
  error: unknown,
): NextIdentityMutationFailure {
  const validation = validationFailure(error);
  if (validation !== undefined) return validation;

  if (error instanceof GenericIdentityClientError) {
    if (error.code === "http" && error.httpStatus === 409) {
      return {
        kind: "conflict",
        title: "The record changed",
        message:
          "A newer version was accepted after this form was loaded. Reload the current state before applying another change.",
        recovery: "reload",
      };
    }

    if (error.code === "http" && error.httpStatus === 404) {
      return {
        kind: "conflict",
        title: "The record is no longer available",
        message:
          "The target record or relationship is no longer present. Reload current state before deciding whether another action is needed.",
        recovery: "reload",
      };
    }

    if (error.code === "http" && error.httpStatus === 400) {
      return {
        kind: "validation",
        title: "The request was rejected",
        message:
          "The API rejected the submitted request. Review the current values and retry only after correcting the request.",
        recovery: "edit",
      };
    }

    switch (error.code) {
      case "unauthenticated":
        return {
          kind: "unauthenticated",
          title: "Administrative session ended",
          message:
            "Authentication is no longer valid. No successful change is assumed; sign in again before continuing.",
          recovery: "sign-in",
        };
      case "forbidden":
        return {
          kind: "forbidden",
          title: "Operation not permitted",
          message:
            "The current administrator is authenticated but is not authorized for this operation. No change was applied by this response.",
          recovery: "none",
        };
      case "unavailable":
        return {
          kind: "unavailable",
          title: "Security dependency unavailable",
          message:
            "Identity Access or one of its required security dependencies is temporarily unavailable. The operation is not treated as confirmed.",
          recovery: "retry",
        };
      case "timeout":
        return {
          kind: "timeout",
          title: "Request timed out",
          message:
            "The request did not complete within the client timeout. Its final server outcome is unknown here; reload current state before retrying.",
          recovery: "reload",
        };
      case "transport":
        return {
          kind: "transport",
          title: "Identity Access could not be reached",
          message:
            "The transport failed before a usable response was received. The final server outcome is unknown here; reload current state before retrying.",
          recovery: "reload",
        };
      case "cancelled":
        return {
          kind: "cancelled",
          title: "Request cancelled",
          message:
            "The request was cancelled before a confirmed response was received. Reload current state before deciding whether to retry.",
          recovery: "reload",
        };
      case "protocol":
      case "oidc":
        return {
          kind: "protocol",
          title: "Unexpected security response",
          message:
            "Identity Access returned a response that could not be safely accepted. No successful change is assumed; reload current state and escalate if the problem persists.",
          recovery: "reload",
        };
      case "configuration":
        return {
          kind: "configuration",
          title: "Administration host configuration error",
          message:
            "The server-side administration host configuration is incomplete or invalid. A deployment operator must correct it before this operation can continue.",
          recovery: "operator",
        };
      case "http":
      default:
        return unexpectedFailure();
    }
  }

  return unexpectedFailure();
}

function validationFailure(error: unknown): NextIdentityMutationFailure | undefined {
  if (!(error instanceof Error)) return undefined;
  const prefix = "Invalid administration input:";
  if (!error.message.startsWith(prefix)) return undefined;

  const detail = error.message.slice(prefix.length).trim();
  return {
    kind: "validation",
    title: "Check the submitted values",
    message: detail || "One or more submitted values are invalid.",
    recovery: "edit",
  };
}

function unexpectedFailure(): NextIdentityMutationFailure {
  return {
    kind: "unexpected",
    title: "Operation not confirmed",
    message:
      "The administration operation failed unexpectedly. No successful change is assumed; reload current state before retrying.",
    recovery: "reload",
  };
}
