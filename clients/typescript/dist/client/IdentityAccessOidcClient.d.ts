import type { IdentityOidcAuthorizationCode, IdentityOidcAuthorizationOptions, IdentityOidcTokenSet, IdentitySessionCredential } from "../contracts.js";
import { IdentityAccessHttpTransport } from "./IdentityAccessHttpTransport.js";
/** OIDC Authorization Code + PKCE and refresh-token rotation. */
export declare class IdentityAccessOidcClient {
    #private;
    constructor(transport: IdentityAccessHttpTransport);
    authorize(credential: IdentitySessionCredential, options: IdentityOidcAuthorizationOptions, signal?: AbortSignal): Promise<IdentityOidcAuthorizationCode>;
    exchangeAuthorizationCode(authorization: IdentityOidcAuthorizationCode, signal?: AbortSignal): Promise<IdentityOidcTokenSet>;
    refreshTokens(clientIdValue: string, refreshTokenValue: string, signal?: AbortSignal): Promise<IdentityOidcTokenSet>;
}
