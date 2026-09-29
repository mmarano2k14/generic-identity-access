import { IdentityAccessPathBuilder } from "./IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "./IdentityAccessValueCodec.js";
/** Capability evaluation against the server-side .NET/RBAC authorization boundary. */
export class IdentityAccessAuthorizationClient {
    #transport;
    constructor(transport) {
        this.#transport = transport;
    }
    validateContext(boundary, credential) {
        IdentityAccessPathBuilder.authorizationPath(boundary);
        IdentityAccessPathBuilder.credentialHeaders(credential);
    }
    async evaluate(boundary, requirement, credential, signal) {
        const response = await this.#transport.requestJson(IdentityAccessPathBuilder.authorizationPath(boundary), {
            method: "POST",
            acceptedStatuses: [200],
            headers: {
                ...IdentityAccessPathBuilder.credentialHeaders(credential),
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                resource: IdentityAccessValueCodec.slug(requirement.resource),
                feature: IdentityAccessValueCodec.slug(requirement.feature),
                action: IdentityAccessValueCodec.slug(requirement.action),
            }),
            ...(signal === undefined ? {} : { signal }),
        }, (value) => ({ allowed: IdentityAccessValueCodec.flag(IdentityAccessValueCodec.object(value).allowed) }));
        return response.allowed;
    }
}
