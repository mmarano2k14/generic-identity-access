export type IdentityAccessErrorCode =
  | "configuration"
  | "http"
  | "protocol"
  | "timeout"
  | "cancelled"
  | "transport";

const messages: Record<IdentityAccessErrorCode, string> = {
  configuration: "Invalid Identity & Access client configuration.",
  http: "The Identity & Access API returned an unexpected HTTP status.",
  protocol: "The Identity & Access API returned an invalid diagnostic response.",
  timeout: "The Identity & Access request timed out.",
  cancelled: "The Identity & Access request was cancelled.",
  transport: "The Identity & Access API could not be reached.",
};

/** No raw response body, credentials or URL are retained in the public error. */
export class IdentityAccessClientError extends Error {
  readonly code: IdentityAccessErrorCode;
  readonly httpStatus: number | undefined;

  constructor(code: IdentityAccessErrorCode, httpStatus?: number) {
    super(messages[code]);
    this.name = "IdentityAccessClientError";
    this.code = code;
    this.httpStatus = httpStatus;
  }
}
