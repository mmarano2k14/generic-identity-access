import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
export class IdentityAccessMembershipsClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, options, signal) {
        const path = `${IdentityAccessPathBuilder.tenantMembershipsPath(context)}${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.tenantMembershipRecord), signal);
    }
    async get(context, membershipIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.tenantMembershipsPath(context)}/${IdentityAccessValueCodec.uuid(membershipIdValue)}`;
        return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.tenantMembershipRecord(value), signal);
    }
    async findByUser(context, userIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.tenantMembershipsPath(context)}/by-user/${IdentityAccessValueCodec.uuid(userIdValue)}`;
        return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.tenantMembershipRecord(value), signal);
    }
    async create(context, request, signal) {
        const path = IdentityAccessPathBuilder.tenantMembershipsPath(context);
        return this.#admin.post(path, context, {
            membershipId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.membershipId),
            userId: IdentityAccessValueCodec.uuid(request.userId),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
        }, (value) => IdentityAccessAdministrationCodec.tenantMembershipRecord(value), signal);
    }
    async update(context, membershipIdValue, request, signal) {
        const path = `${IdentityAccessPathBuilder.tenantMembershipsPath(context)}/${IdentityAccessValueCodec.uuid(membershipIdValue)}`;
        return this.#admin.put(path, context, {
            status: IdentityAccessValueCodec.lifecycleStatus(request.status),
            expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
        }, (value) => IdentityAccessAdministrationCodec.tenantMembershipRecord(value), signal);
    }
}
