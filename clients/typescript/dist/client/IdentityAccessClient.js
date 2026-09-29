import { IdentityAccessAdministrationClient } from "./administration/IdentityAccessAdministrationClient.js";
import { IdentityAccessAuthenticationClient } from "./IdentityAccessAuthenticationClient.js";
import { IdentityAccessAuthorizationClient } from "./IdentityAccessAuthorizationClient.js";
import { IdentityAccessHttpTransport } from "./IdentityAccessHttpTransport.js";
import { IdentityAccessOidcClient } from "./IdentityAccessOidcClient.js";
import { IdentityAccessSystemClient } from "./IdentityAccessSystemClient.js";
/**
 * Root class-based Identity Access client.
 *
 * The root owns only composition. Transport, authentication, OIDC, authorization,
 * and administration behavior live in focused classes under `src/client/`.
 */
export class IdentityAccessClient {
    system;
    authentication;
    oidc;
    authorization;
    administration;
    constructor(options) {
        const transport = new IdentityAccessHttpTransport(options);
        this.system = new IdentityAccessSystemClient(transport);
        this.authentication = new IdentityAccessAuthenticationClient(transport);
        this.oidc = new IdentityAccessOidcClient(transport);
        this.authorization = new IdentityAccessAuthorizationClient(transport);
        this.administration = new IdentityAccessAdministrationClient(transport);
    }
}
