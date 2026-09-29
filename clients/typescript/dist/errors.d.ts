export type IdentityAccessErrorCode = "configuration" | "http" | "protocol" | "oidc" | "unauthenticated" | "forbidden" | "unavailable" | "timeout" | "cancelled" | "transport";
/** No raw response body, credentials, URL, or transport cause are retained in the public error. */
export declare class IdentityAccessClientError extends Error {
    readonly code: IdentityAccessErrorCode;
    readonly httpStatus: number | undefined;
    readonly protocolCode: string | undefined;
    constructor(code: IdentityAccessErrorCode, httpStatus?: number, protocolCode?: string);
}
