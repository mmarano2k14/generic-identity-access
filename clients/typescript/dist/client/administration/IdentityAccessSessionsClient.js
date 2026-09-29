import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
export class IdentityAccessSessionsClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async revokeUser(context, userIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/sessions/users/${IdentityAccessValueCodec.uuid(userIdValue)}`;
        return this.#admin.deleteJson(path, context, (value) => IdentityAccessAdministrationCodec.sessionRevocationResult(value), signal);
    }
    async revokeClient(context, clientIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/sessions/clients/${encodeURIComponent(IdentityAccessValueCodec.clientId(clientIdValue))}`;
        return this.#admin.deleteJson(path, context, (value) => IdentityAccessAdministrationCodec.sessionRevocationResult(value), signal);
    }
}
