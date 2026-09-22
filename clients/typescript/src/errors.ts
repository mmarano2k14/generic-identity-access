export type IdentityAccessErrorCode =
  | "configuration"
  | "http"
  | "protocol"
  | "unauthenticated"
  | "forbidden"
  | "unavailable"
  | "timeout"
  | "cancelled"
  | "transport";

const messages: Record<IdentityAccessErrorCode, string> = {
  configuration: "Invalid Identity & Access client configuration.",
  http: "The Identity & Access API returned an unexpected HTTP status.",
  protocol: "The Identity & Access API returned an invalid response.",
  unauthenticated: "The Identity & Access request is not authenticated.",
  forbidden: "The Identity & Access request is forbidden.",
  unavailable: "The Identity & Access security dependency is unavailable.",
  timeout: "The Identity & Access request timed out.",
  cancelled: "The Identity & Access request was cancelled.",
  transport: "The Identity & Access API could not be reached.",
};

/** No raw response body, credentials, URL, or transport cause are retained in the public error. */
export class IdentityAccessClientError extends Error {
  public readonly code: IdentityAccessErrorCode;
  public readonly httpStatus: number | undefined;

  public constructor(code: IdentityAccessErrorCode, httpStatus?: number) {
    super(messages[code]);
    this.name = "IdentityAccessClientError";
    this.code = code;
    this.httpStatus = httpStatus;
  }
}
