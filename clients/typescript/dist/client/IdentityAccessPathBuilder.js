import { IdentityAccessClientError } from "../errors.js";
import { IdentityAccessValueCodec } from "./IdentityAccessValueCodec.js";
/** Builds trusted API paths and transport headers without owning HTTP execution. */
export class IdentityAccessPathBuilder {
    static authorizationPath(boundary) {
        const identityScopeId = IdentityAccessValueCodec.uuid(boundary.identityScopeId);
        const applicationKey = IdentityAccessValueCodec.slug(boundary.applicationKey);
        const tenantId = boundary.tenantId === undefined ? undefined : IdentityAccessValueCodec.uuid(boundary.tenantId);
        const resourceScopeId = boundary.resourceScopeId === undefined
            ? undefined
            : IdentityAccessValueCodec.uuid(boundary.resourceScopeId);
        if (resourceScopeId !== undefined && tenantId === undefined) {
            throw new IdentityAccessClientError("configuration");
        }
        if (resourceScopeId !== undefined) {
            return `api/v1/identity-scopes/${identityScopeId}/tenants/${tenantId}/applications/${applicationKey}/resource-scopes/${resourceScopeId}/authorization/evaluate`;
        }
        if (tenantId !== undefined) {
            return `api/v1/identity-scopes/${identityScopeId}/tenants/${tenantId}/applications/${applicationKey}/authorization/evaluate`;
        }
        return `api/v1/identity-scopes/${identityScopeId}/applications/${applicationKey}/authorization/evaluate`;
    }
    static credentialHeaders(credential) {
        if (credential.kind === "bearer") {
            return { Authorization: `Bearer ${IdentityAccessValueCodec.token(credential.accessToken)}` };
        }
        return {
            Authorization: `IdentitySession ${IdentityAccessValueCodec.token(credential.sessionToken)}`,
            "X-Identity-Access-Client": IdentityAccessValueCodec.clientId(credential.clientId),
            "X-Identity-Access-Session": IdentityAccessValueCodec.uuid(credential.sessionId),
        };
    }
    static oidcSessionHeaders(credential) {
        return {
            Authorization: `IdentitySession ${IdentityAccessValueCodec.token(credential.sessionToken)}`,
            "X-Identity-Access-Session": IdentityAccessValueCodec.uuid(credential.sessionId),
        };
    }
    static administrationBasePath(context) {
        const scope = IdentityAccessValueCodec.uuid(context.identityScopeId);
        const application = IdentityAccessValueCodec.slug(context.applicationKey);
        IdentityAccessPathBuilder.credentialHeaders(context.credential);
        return `api/v1/identity-scopes/${scope}/applications/${application}`;
    }
    static tenantApplicationPath(context) {
        const scope = IdentityAccessValueCodec.uuid(context.identityScopeId);
        const tenant = IdentityAccessValueCodec.uuid(context.tenantId);
        const application = IdentityAccessValueCodec.slug(context.applicationKey);
        IdentityAccessPathBuilder.credentialHeaders(context.credential);
        return `api/v1/identity-scopes/${scope}/tenants/${tenant}/applications/${application}`;
    }
    static tenantMembershipsPath(context) {
        const scope = IdentityAccessValueCodec.uuid(context.identityScopeId);
        const tenant = IdentityAccessValueCodec.uuid(context.tenantId);
        const application = IdentityAccessValueCodec.slug(context.applicationKey);
        IdentityAccessPathBuilder.credentialHeaders(context.credential);
        return `api/v1/identity-scopes/${scope}/applications/${application}/tenants/${tenant}/memberships`;
    }
    static tenantUsersPath(context) {
        const scope = IdentityAccessValueCodec.uuid(context.identityScopeId);
        const tenant = IdentityAccessValueCodec.uuid(context.tenantId);
        const application = IdentityAccessValueCodec.slug(context.applicationKey);
        IdentityAccessPathBuilder.credentialHeaders(context.credential);
        return `api/v1/identity-scopes/${scope}/tenants/${tenant}/applications/${application}/users`;
    }
    /** Builds the tenant-local OrganisationProfile base path hosted by IdentityAccess.Api. */
    static organisationProfilesPath(context) {
        const scope = IdentityAccessValueCodec.uuid(context.identityScopeId);
        const tenant = IdentityAccessValueCodec.uuid(context.tenantId);
        const application = IdentityAccessValueCodec.slug(context.applicationKey);
        IdentityAccessPathBuilder.credentialHeaders(context.credential);
        return `api/v1/identity-scopes/${scope}/applications/${application}/tenants/${tenant}/organisation-profiles`;
    }
    /** Builds the reusable OrganisationProfile template-catalog path. */
    static organisationProfileTemplatesPath(context) {
        const scope = IdentityAccessValueCodec.uuid(context.identityScopeId);
        const application = IdentityAccessValueCodec.slug(context.applicationKey);
        IdentityAccessPathBuilder.credentialHeaders(context.credential);
        return `api/v1/identity-scopes/${scope}/applications/${application}/organisation-profile-templates`;
    }
    /** Builds the tenant-local Organization Directory base path hosted by IdentityAccess.Api. */
    static organizationDirectoryPath(context) {
        const scope = IdentityAccessValueCodec.uuid(context.identityScopeId);
        const tenant = IdentityAccessValueCodec.uuid(context.tenantId);
        const application = IdentityAccessValueCodec.slug(context.applicationKey);
        IdentityAccessPathBuilder.credentialHeaders(context.credential);
        return `api/v1/identity-scopes/${scope}/applications/${application}/tenants/${tenant}/organizations`;
    }
    /** Builds the member-centric OrganizationMembership read path. */
    static tenantMembershipOrganizationsPath(context, tenantMembershipIdValue) {
        const scope = IdentityAccessValueCodec.uuid(context.identityScopeId);
        const tenant = IdentityAccessValueCodec.uuid(context.tenantId);
        const application = IdentityAccessValueCodec.slug(context.applicationKey);
        const tenantMembershipId = IdentityAccessValueCodec.uuid(tenantMembershipIdValue);
        IdentityAccessPathBuilder.credentialHeaders(context.credential);
        return `api/v1/identity-scopes/${scope}/applications/${application}/tenants/${tenant}/tenant-memberships/${tenantMembershipId}/organizations`;
    }
    static scopeAuthorityPath(context) {
        return `${IdentityAccessPathBuilder.administrationBasePath(context)}/scope-authority`;
    }
    static tenantUserListQuery(options) {
        if (options === undefined)
            return "";
        const base = IdentityAccessPathBuilder.administrationListQuery(options);
        const query = new URLSearchParams(base.startsWith("?") ? base.slice(1) : base);
        if (options.activeMembershipsOnly !== undefined) {
            query.set("activeMembershipsOnly", IdentityAccessValueCodec.boolean(options.activeMembershipsOnly) ? "true" : "false");
        }
        const value = query.toString();
        return value.length === 0 ? "" : `?${value}`;
    }
    static administrationListQuery(options) {
        if (options === undefined)
            return "";
        const query = new URLSearchParams();
        if (options.offset !== undefined) {
            if (!Number.isSafeInteger(options.offset) || options.offset < 0) {
                throw new IdentityAccessClientError("configuration");
            }
            query.set("offset", String(options.offset));
        }
        if (options.limit !== undefined) {
            if (!Number.isSafeInteger(options.limit) || options.limit < 1 || options.limit > 200) {
                throw new IdentityAccessClientError("configuration");
            }
            query.set("limit", String(options.limit));
        }
        if (options.search !== undefined) {
            const search = options.search.trim();
            if (search.length < 3 || search.length > 128) {
                throw new IdentityAccessClientError("configuration");
            }
            query.set("search", search);
        }
        const value = query.toString();
        return value.length === 0 ? "" : `?${value}`;
    }
}
