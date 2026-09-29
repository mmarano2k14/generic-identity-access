import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
/** Reads the server-trusted effective administration context for the current credential. */
export class IdentityAccessAdministrationContextClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async get(context, signal) {
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/administration-context`;
        return this.#admin.get(path, context, (value) => IdentityAccessAdministrationCodec.effectiveAdministrationContext(value), signal);
    }
}
