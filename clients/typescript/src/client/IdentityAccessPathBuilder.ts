import type {
  IdentityAccessCredential,
  IdentityAuthorizationBoundary,
  IdentitySessionCredential,
} from "../contracts.js";
import type {
  IdentityAdministrationContext,
  IdentityAdministrationListOptions,
  IdentityTenantAdministrationContext,
  IdentityTenantUserListOptions,
} from "../admin-contracts.js";
import { IdentityAccessClientError } from "../errors.js";
import { IdentityAccessValueCodec } from "./IdentityAccessValueCodec.js";

/** Builds trusted API paths and transport headers without owning HTTP execution. */
export class IdentityAccessPathBuilder {
  public static authorizationPath(boundary: IdentityAuthorizationBoundary): string {
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

  public static credentialHeaders(credential: IdentityAccessCredential): Readonly<Record<string, string>> {
    if (credential.kind === "bearer") {
      return { Authorization: `Bearer ${IdentityAccessValueCodec.token(credential.accessToken)}` };
    }

    return {
      Authorization: `IdentitySession ${IdentityAccessValueCodec.token(credential.sessionToken)}`,
      "X-Identity-Access-Client": IdentityAccessValueCodec.clientId(credential.clientId),
      "X-Identity-Access-Session": IdentityAccessValueCodec.uuid(credential.sessionId),
    };
  }

  public static oidcSessionHeaders(credential: IdentitySessionCredential): Readonly<Record<string, string>> {
    return {
      Authorization: `IdentitySession ${IdentityAccessValueCodec.token(credential.sessionToken)}`,
      "X-Identity-Access-Session": IdentityAccessValueCodec.uuid(credential.sessionId),
    };
  }

  public static administrationBasePath(context: IdentityAdministrationContext): string {
    const scope = IdentityAccessValueCodec.uuid(context.identityScopeId);
    const application = IdentityAccessValueCodec.slug(context.applicationKey);
    IdentityAccessPathBuilder.credentialHeaders(context.credential);
    return `api/v1/identity-scopes/${scope}/applications/${application}`;
  }

  public static tenantApplicationPath(context: IdentityTenantAdministrationContext): string {
    const scope = IdentityAccessValueCodec.uuid(context.identityScopeId);
    const tenant = IdentityAccessValueCodec.uuid(context.tenantId);
    const application = IdentityAccessValueCodec.slug(context.applicationKey);
    IdentityAccessPathBuilder.credentialHeaders(context.credential);
    return `api/v1/identity-scopes/${scope}/tenants/${tenant}/applications/${application}`;
  }

  public static tenantMembershipsPath(context: IdentityTenantAdministrationContext): string {
    const scope = IdentityAccessValueCodec.uuid(context.identityScopeId);
    const tenant = IdentityAccessValueCodec.uuid(context.tenantId);
    const application = IdentityAccessValueCodec.slug(context.applicationKey);
    IdentityAccessPathBuilder.credentialHeaders(context.credential);
    return `api/v1/identity-scopes/${scope}/applications/${application}/tenants/${tenant}/memberships`;
  }

  public static tenantUsersPath(context: IdentityTenantAdministrationContext): string {
    const scope = IdentityAccessValueCodec.uuid(context.identityScopeId);
    const tenant = IdentityAccessValueCodec.uuid(context.tenantId);
    const application = IdentityAccessValueCodec.slug(context.applicationKey);
    IdentityAccessPathBuilder.credentialHeaders(context.credential);
    return `api/v1/identity-scopes/${scope}/tenants/${tenant}/applications/${application}/users`;
  }

  /** Builds the tenant-local Organization Directory base path hosted by IdentityAccess.Api. */
  public static organizationDirectoryPath(context: IdentityTenantAdministrationContext): string {
    const scope = IdentityAccessValueCodec.uuid(context.identityScopeId);
    const tenant = IdentityAccessValueCodec.uuid(context.tenantId);
    const application = IdentityAccessValueCodec.slug(context.applicationKey);
    IdentityAccessPathBuilder.credentialHeaders(context.credential);
    return `api/v1/identity-scopes/${scope}/applications/${application}/tenants/${tenant}/organizations`;
  }

  /** Builds the member-centric OrganizationMembership read path. */
  public static tenantMembershipOrganizationsPath(
    context: IdentityTenantAdministrationContext,
    tenantMembershipIdValue: string,
  ): string {
    const scope = IdentityAccessValueCodec.uuid(context.identityScopeId);
    const tenant = IdentityAccessValueCodec.uuid(context.tenantId);
    const application = IdentityAccessValueCodec.slug(context.applicationKey);
    const tenantMembershipId = IdentityAccessValueCodec.uuid(tenantMembershipIdValue);
    IdentityAccessPathBuilder.credentialHeaders(context.credential);
    return `api/v1/identity-scopes/${scope}/applications/${application}/tenants/${tenant}/tenant-memberships/${tenantMembershipId}/organizations`;
  }

  public static scopeAuthorityPath(context: IdentityAdministrationContext): string {
    return `${IdentityAccessPathBuilder.administrationBasePath(context)}/scope-authority`;
  }

  public static tenantUserListQuery(options: IdentityTenantUserListOptions | undefined): string {
    if (options === undefined) return "";

    const base = IdentityAccessPathBuilder.administrationListQuery(options);
    const query = new URLSearchParams(base.startsWith("?") ? base.slice(1) : base);
    if (options.activeMembershipsOnly !== undefined) {
      query.set(
        "activeMembershipsOnly",
        IdentityAccessValueCodec.boolean(options.activeMembershipsOnly) ? "true" : "false",
      );
    }

    const value = query.toString();
    return value.length === 0 ? "" : `?${value}`;
  }

  public static administrationListQuery(options: IdentityAdministrationListOptions | undefined): string {
    if (options === undefined) return "";

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
