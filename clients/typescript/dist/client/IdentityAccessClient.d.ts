import { IdentityAccessAdministrationClient } from "./administration/IdentityAccessAdministrationClient.js";
import { IdentityAccessAuthenticationClient } from "./IdentityAccessAuthenticationClient.js";
import { IdentityAccessAuthorizationClient } from "./IdentityAccessAuthorizationClient.js";
import { type FetchTransport, type IdentityAccessClientOptions } from "./IdentityAccessHttpTransport.js";
import { IdentityAccessOidcClient } from "./IdentityAccessOidcClient.js";
import { IdentityAccessSystemClient } from "./IdentityAccessSystemClient.js";
/**
 * Root class-based Identity Access client.
 *
 * The root owns only composition. Transport, authentication, OIDC, authorization,
 * and administration behavior live in focused classes under `src/client/`.
 */
export declare class IdentityAccessClient {
    readonly system: IdentityAccessSystemClient;
    readonly authentication: IdentityAccessAuthenticationClient;
    readonly oidc: IdentityAccessOidcClient;
    readonly authorization: IdentityAccessAuthorizationClient;
    readonly administration: IdentityAccessAdministrationClient;
    constructor(options: IdentityAccessClientOptions);
}
export type { FetchTransport, IdentityAccessClientOptions };
