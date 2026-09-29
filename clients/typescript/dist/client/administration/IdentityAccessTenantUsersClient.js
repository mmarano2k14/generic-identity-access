import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
/** Reads tenant-constrained user projections without exposing the scope-wide directory. */
export class IdentityAccessTenantUsersClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, options, signal) {
        const path = `${IdentityAccessPathBuilder.tenantUsersPath(context)}${IdentityAccessPathBuilder.tenantUserListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.tenantUserRecord), signal);
    }
}
