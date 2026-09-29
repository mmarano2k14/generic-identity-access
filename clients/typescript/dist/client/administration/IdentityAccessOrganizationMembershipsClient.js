import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
/** Explicit OrganizationMembership client. Belonging never implies authorization. */
export class IdentityAccessOrganizationMembershipsClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async listForOrganization(context, organizationIdValue, options, signal) {
        const organizationId = IdentityAccessValueCodec.uuid(organizationIdValue);
        const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${organizationId}/memberships${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.organizationMembershipRecord), signal);
    }
    async listForTenantMembership(context, tenantMembershipIdValue, options, signal) {
        const path = `${IdentityAccessPathBuilder.tenantMembershipOrganizationsPath(context, tenantMembershipIdValue)}${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.organizationMembershipRecord), signal);
    }
    async get(context, organizationIdValue, tenantMembershipIdValue, signal) {
        const organizationId = IdentityAccessValueCodec.uuid(organizationIdValue);
        const tenantMembershipId = IdentityAccessValueCodec.uuid(tenantMembershipIdValue);
        const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${organizationId}/memberships/${tenantMembershipId}`;
        return this.#admin.getNullable(path, context, IdentityAccessAdministrationCodec.organizationMembershipRecord, signal);
    }
    async add(context, organizationIdValue, tenantMembershipIdValue, signal) {
        const organizationId = IdentityAccessValueCodec.uuid(organizationIdValue);
        const tenantMembershipId = IdentityAccessValueCodec.uuid(tenantMembershipIdValue);
        const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${organizationId}/memberships`;
        return this.#admin.post(path, context, { tenantMembershipId }, IdentityAccessAdministrationCodec.organizationMembershipRecord, signal);
    }
    async activate(context, organizationIdValue, tenantMembershipIdValue, expectedRowVersion, signal) {
        return this.#lifecycle(context, organizationIdValue, tenantMembershipIdValue, "activate", expectedRowVersion, signal);
    }
    async suspend(context, organizationIdValue, tenantMembershipIdValue, expectedRowVersion, signal) {
        return this.#lifecycle(context, organizationIdValue, tenantMembershipIdValue, "suspend", expectedRowVersion, signal);
    }
    async remove(context, organizationIdValue, tenantMembershipIdValue, expectedRowVersion, signal) {
        const organizationId = IdentityAccessValueCodec.uuid(organizationIdValue);
        const tenantMembershipId = IdentityAccessValueCodec.uuid(tenantMembershipIdValue);
        const version = IdentityAccessValueCodec.version(expectedRowVersion);
        const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${organizationId}/memberships/${tenantMembershipId}?expectedRowVersion=${version}`;
        return this.#admin.delete(path, context, signal);
    }
    async #lifecycle(context, organizationIdValue, tenantMembershipIdValue, action, expectedRowVersion, signal) {
        const organizationId = IdentityAccessValueCodec.uuid(organizationIdValue);
        const tenantMembershipId = IdentityAccessValueCodec.uuid(tenantMembershipIdValue);
        const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${organizationId}/memberships/${tenantMembershipId}/${action}`;
        return this.#admin.postJsonAction(path, context, { expectedRowVersion: IdentityAccessValueCodec.version(expectedRowVersion) }, IdentityAccessAdministrationCodec.organizationMembershipRecord, signal);
    }
}
