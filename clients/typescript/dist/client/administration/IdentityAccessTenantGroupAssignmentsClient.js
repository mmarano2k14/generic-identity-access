import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
/** Aggregate tenant read surface for group assignments. */
export class IdentityAccessTenantGroupAssignmentsClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, signal) {
        return this.#admin.get(`${IdentityAccessPathBuilder.tenantApplicationPath(context)}/group-memberships`, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.tenantGroupAssignmentRecord), signal);
    }
}
