import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
/** Exact-login candidate lookup for controlled tenant membership creation. */
export class IdentityAccessMembershipCandidatesClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async findByLogin(context, loginIdentifier, signal) {
        const login = IdentityAccessValueCodec.nonEmpty(loginIdentifier);
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/tenants/${IdentityAccessValueCodec.uuid(context.tenantId)}/membership-candidates/by-login?loginIdentifier=${encodeURIComponent(login)}`;
        return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.tenantMembershipCandidateRecord(value), signal);
    }
    async createMembershipByLogin(context, loginIdentifier, status = 1, signal) {
        const login = IdentityAccessValueCodec.nonEmpty(loginIdentifier);
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/tenants/${IdentityAccessValueCodec.uuid(context.tenantId)}/membership-candidates/by-login/membership`;
        return this.#admin.post(path, context, {
            loginIdentifier: login,
            status: IdentityAccessValueCodec.lifecycleStatus(status),
        }, (value) => IdentityAccessAdministrationCodec.tenantMembershipRecord(value), signal);
    }
}
