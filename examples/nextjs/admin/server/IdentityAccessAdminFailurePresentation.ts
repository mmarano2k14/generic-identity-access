import "server-only";
import { IdentityAccessClientError } from "@identity-access/client";
import type { AdminActionFailure } from "../contracts/AdminActionState";

/** Maps server-side administration failures into bounded, secret-safe UI guidance only. */
export class IdentityAccessAdminFailurePresentation {
  public static fromMutation(error: unknown): AdminActionFailure {
    const validation = IdentityAccessAdminFailurePresentation.#validationFailure(error);
    if (validation !== undefined) return validation;

    if (error instanceof IdentityAccessClientError) {
      return IdentityAccessAdminFailurePresentation.#clientFailure(error, true);
    }

    return {
      kind: "unexpected",
      title: "Operation not confirmed",
      message: "The administration operation failed unexpectedly. No successful change is assumed; reload current state before retrying.",
      recovery: "reload",
    };
  }

  public static fromRead(error: unknown): AdminActionFailure {
    if (error instanceof IdentityAccessClientError) {
      return IdentityAccessAdminFailurePresentation.#clientFailure(error, false);
    }

    return {
      kind: "unexpected",
      title: "Data could not be loaded",
      message: "The protected read failed unexpectedly. No security state is inferred from the missing response.",
      recovery: "retry",
    };
  }

  static #validationFailure(error: unknown): AdminActionFailure | undefined {
    if (!(error instanceof Error)) return undefined;
    const prefixes = ["Invalid administration input:", "Invalid session administration input:"];
    const prefix = prefixes.find((candidate) => error.message.startsWith(candidate));
    if (prefix === undefined) return undefined;

    const detail = error.message.slice(prefix.length).trim();
    return {
      kind: "validation",
      title: "Check the submitted values",
      message: detail || "One or more submitted values are invalid.",
      recovery: "edit",
    };
  }

  static #clientFailure(error: IdentityAccessClientError, mutation: boolean): AdminActionFailure {
    if (error.code === "http" && error.httpStatus === 409) {
      return {
        kind: "conflict",
        title: "The record changed",
        message: mutation
          ? "A newer version was accepted after this form was loaded. Reload the current state before applying another change."
          : "The requested state changed while it was being read. Reload the current state before continuing.",
        recovery: "reload",
      };
    }

    if (error.code === "http" && error.httpStatus === 404) {
      return {
        kind: "conflict",
        title: "The record is no longer available",
        message: mutation
          ? "The target record or relationship is no longer present. Reload current state before deciding whether another action is needed."
          : "The requested record is no longer available in the current authorized context.",
        recovery: "reload",
      };
    }

    if (error.code === "http" && error.httpStatus === 400) {
      return {
        kind: "validation",
        title: "The request was rejected",
        message: "The API rejected the submitted request. Review the current values and retry only after correcting the request.",
        recovery: "edit",
      };
    }

    switch (error.code) {
      case "unauthenticated":
        return {
          kind: "unauthenticated",
          title: "Administrative session ended",
          message: mutation
            ? "Authentication is no longer valid. No successful change is assumed; sign in again before continuing."
            : "Authentication is no longer valid. Sign in again to reload this protected administration view.",
          recovery: "sign-in",
        };
      case "forbidden":
        return {
          kind: "forbidden",
          title: mutation ? "Operation not permitted" : "Access denied",
          message: mutation
            ? "The current administrator is authenticated but is not authorized for this operation. No change was applied by this response."
            : "You are authenticated, but your current access context does not allow this protected information to be read.",
          recovery: "none",
        };
      case "unavailable":
        return {
          kind: "unavailable",
          title: "Security dependency unavailable",
          message: mutation
            ? "Identity Access or one of its required security dependencies is temporarily unavailable. The operation is not treated as confirmed."
            : "Identity Access or one of its required security dependencies is temporarily unavailable. No state is inferred from the missing response.",
          recovery: "retry",
        };
      case "timeout":
        return {
          kind: "timeout",
          title: "Request timed out",
          message: mutation
            ? "The request did not complete within the client timeout. Its final server outcome is unknown here; reload current state before retrying."
            : "The protected read timed out. Retry the read; no state is inferred from the missing response.",
          recovery: mutation ? "reload" : "retry",
        };
      case "transport":
        return {
          kind: "transport",
          title: "Identity Access could not be reached",
          message: mutation
            ? "The transport failed before a usable response was received. The final server outcome is unknown here; reload current state before retrying."
            : "The Identity Access API could not be reached. Retry the read when connectivity is restored.",
          recovery: mutation ? "reload" : "retry",
        };
      case "cancelled":
        return {
          kind: "cancelled",
          title: "Request cancelled",
          message: mutation
            ? "The request was cancelled before a confirmed response was received. Reload current state before deciding whether to retry."
            : "The protected read was cancelled. Retry when the request can complete normally.",
          recovery: mutation ? "reload" : "retry",
        };
      case "protocol":
      case "oidc":
        return {
          kind: "protocol",
          title: "Unexpected security response",
          message: mutation
            ? "Identity Access returned a response that could not be safely accepted. No successful change is assumed; reload current state and escalate if the problem persists."
            : "Identity Access returned a response that could not be safely accepted. No security state is inferred from it.",
          recovery: mutation ? "reload" : "operator",
        };
      case "configuration":
        return {
          kind: "configuration",
          title: "Administration host configuration error",
          message: "The server-side administration host configuration is incomplete or invalid. A deployment operator must correct it before this operation can continue.",
          recovery: "operator",
        };
      case "http":
      default:
        return {
          kind: "unexpected",
          title: mutation ? "Operation not confirmed" : "Protected read failed",
          message: mutation
            ? "Identity Access returned an unexpected status. No successful change is assumed; reload current state before retrying."
            : "Identity Access returned an unexpected status. No security state is inferred from the missing result.",
          recovery: mutation ? "reload" : "retry",
        };
    }
  }
}
