import type { IdentityAccessCredential, IdentityAuthorizationBoundary, IdentitySessionCredential } from "../contracts.js";
import type { IdentityAdministrationContext, IdentityAdministrationListOptions, IdentityTenantAdministrationContext, IdentityTenantUserListOptions } from "../admin-contracts.js";
/** Builds trusted API paths and transport headers without owning HTTP execution. */
export declare class IdentityAccessPathBuilder {
    static authorizationPath(boundary: IdentityAuthorizationBoundary): string;
    static credentialHeaders(credential: IdentityAccessCredential): Readonly<Record<string, string>>;
    static oidcSessionHeaders(credential: IdentitySessionCredential): Readonly<Record<string, string>>;
    static administrationBasePath(context: IdentityAdministrationContext): string;
    static tenantApplicationPath(context: IdentityTenantAdministrationContext): string;
    static tenantMembershipsPath(context: IdentityTenantAdministrationContext): string;
    static tenantUsersPath(context: IdentityTenantAdministrationContext): string;
    /** Builds the tenant-local OrganisationProfile base path hosted by IdentityAccess.Api. */
    static organisationProfilesPath(context: IdentityTenantAdministrationContext): string;
    /** Builds the reusable OrganisationProfile template-catalog path. */
    static organisationProfileTemplatesPath(context: IdentityAdministrationContext): string;
    /** Builds the tenant-local Organization Directory base path hosted by IdentityAccess.Api. */
    static organizationDirectoryPath(context: IdentityTenantAdministrationContext): string;
    /** Builds the member-centric OrganizationMembership read path. */
    static tenantMembershipOrganizationsPath(context: IdentityTenantAdministrationContext, tenantMembershipIdValue: string): string;
    static scopeAuthorityPath(context: IdentityAdministrationContext): string;
    static tenantUserListQuery(options: IdentityTenantUserListOptions | undefined): string;
    static administrationListQuery(options: IdentityAdministrationListOptions | undefined): string;
}
