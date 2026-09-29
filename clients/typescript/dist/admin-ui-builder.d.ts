import type { IdentityAccessAdminUiBuilderOptions, IdentityAccessAdminUiDefinition } from "./contracts.js";
import { IdentityAuthorizationContext } from "./authorization-context.js";
/**
 * Class-based builder for a reusable administration UI definition.
 * The builder never treats UI visibility as authorization; the .NET API remains authoritative.
 */
export declare class IdentityAccessAdminUiBuilder {
    #private;
    constructor(authorization: IdentityAuthorizationContext, options?: IdentityAccessAdminUiBuilderOptions);
    /** Supplies the tenant-scoped authorization context used for tenant-bound UI sections. */
    withTenantAuthorization(authorization: IdentityAuthorizationContext): this;
    /** Supplies all trusted active tenant contexts used for tenant-aware presentation filtering. */
    withTenantAuthorizations(authorizations: readonly IdentityAuthorizationContext[]): this;
    withUsers(): this;
    withTenants(): this;
    withMemberships(): this;
    withGroups(): this;
    withPolicies(): this;
    withSecurityModels(): this;
    withResourceScopes(): this;
    withMfa(): this;
    withSessions(): this;
    withSecurityAudit(): this;
    withScopeAuthority(): this;
    /** Adds every currently supported administration section in stable navigation order. */
    withAll(): this;
    build(): IdentityAccessAdminUiDefinition;
    /**
     * Returns only sections for which the current trusted context has the section's read capability.
     * This is presentation filtering only and never substitutes server-side authorization.
     */
    buildVisible(signal?: AbortSignal): Promise<IdentityAccessAdminUiDefinition>;
    static basePath(value: string): string;
}
