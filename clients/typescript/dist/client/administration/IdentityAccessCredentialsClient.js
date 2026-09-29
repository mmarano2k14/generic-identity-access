import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
/** Password-credential administration without exposing password hashes or stored secret material. */
export class IdentityAccessCredentialsClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async get(context, userIdValue, signal) {
        return this.#admin.getNullable(this.#path(context, userIdValue), context, (value) => IdentityAccessAdministrationCodec.passwordCredentialMetadataRecord(value), signal);
    }
    async create(context, userIdValue, request, signal) {
        return this.#admin.post(this.#path(context, userIdValue), context, {
            loginIdentifier: IdentityAccessCredentialsClient.#loginIdentifier(request.loginIdentifier),
            password: IdentityAccessCredentialsClient.#password(request.password),
        }, (value) => IdentityAccessAdministrationCodec.passwordCredentialMetadataRecord(value), signal);
    }
    async changePassword(context, userIdValue, request, signal) {
        return this.#admin.put(this.#path(context, userIdValue), context, {
            loginIdentifier: IdentityAccessCredentialsClient.#loginIdentifier(request.loginIdentifier),
            password: IdentityAccessCredentialsClient.#password(request.password),
            expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
        }, (value) => IdentityAccessAdministrationCodec.passwordCredentialMetadataRecord(value), signal);
    }
    #path(context, userIdValue) {
        const userId = IdentityAccessValueCodec.uuid(userIdValue);
        return `${IdentityAccessPathBuilder.administrationBasePath(context)}/users/${userId}/password-credential`;
    }
    static #loginIdentifier(value) {
        return IdentityAccessValueCodec.nonEmpty(value.trim());
    }
    static #password(value) {
        return IdentityAccessValueCodec.nonEmptySecret(value);
    }
}
