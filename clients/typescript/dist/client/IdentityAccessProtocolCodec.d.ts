import type { IdentityOidcTokenSet, ServiceInfoResponse } from "../contracts.js";
/** Decodes protocol-level responses and OIDC redirects/token errors. */
export declare class IdentityAccessProtocolCodec {
    static serviceInfo(value: unknown): ServiceInfoResponse;
    static parseAuthorizationRedirect(location: string, expectedRedirectUri: string, expectedState: string): {
        readonly code: string;
    };
    static tokenSet(value: unknown, status: number): IdentityOidcTokenSet;
    private static oidcError;
    private static oidcProtocolCode;
}
