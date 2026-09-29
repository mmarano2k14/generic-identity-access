import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
export class IdentityAccessUsersClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, options, signal) {
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/users${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.userRecord), signal);
    }
    async get(context, userIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/users/${IdentityAccessValueCodec.uuid(userIdValue)}`;
        return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.userRecord(value), signal);
    }
    async create(context, request, signal) {
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/users`;
        return this.#admin.post(path, context, {
            userId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.userId),
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
        }, (value) => IdentityAccessAdministrationCodec.userRecord(value), signal);
    }
    async update(context, userIdValue, request, signal) {
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/users/${IdentityAccessValueCodec.uuid(userIdValue)}`;
        return this.#admin.put(path, context, {
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status),
            expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
        }, (value) => IdentityAccessAdministrationCodec.userRecord(value), signal);
    }
}
