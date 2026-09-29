import { IdentityAuthorizationContext } from "./authorization-context.js";
import { IdentityAccessClientError } from "./errors.js";
/**
 * Class-based builder for a reusable administration UI definition.
 * The builder never treats UI visibility as authorization; the .NET API remains authoritative.
 */
export class IdentityAccessAdminUiBuilder {
    #authorization;
    #basePath;
    #entries = new Map();
    #tenantAuthorizations = [];
    constructor(authorization, options = {}) {
        if (!(authorization instanceof IdentityAuthorizationContext)) {
            throw new IdentityAccessClientError("configuration");
        }
        this.#authorization = authorization;
        this.#basePath = IdentityAccessAdminUiBuilder.basePath(options.basePath ?? "/identity");
    }
    /** Supplies the tenant-scoped authorization context used for tenant-bound UI sections. */
    withTenantAuthorization(authorization) {
        return this.withTenantAuthorizations([authorization]);
    }
    /** Supplies all trusted active tenant contexts used for tenant-aware presentation filtering. */
    withTenantAuthorizations(authorizations) {
        if (authorizations.some((authorization) => !(authorization instanceof IdentityAuthorizationContext))) {
            throw new IdentityAccessClientError("configuration");
        }
        this.#tenantAuthorizations = Object.freeze([...authorizations]);
        return this;
    }
    withUsers() {
        return this.#with("users", "user", "Users", "Manage identities and account lifecycle.", true);
    }
    withTenants() {
        return this.#with("tenants", "tenant", "Tenants", "Manage tenant security boundaries.", false);
    }
    withMemberships() {
        return this.#with("memberships", "tenant-membership", "Memberships", "Manage user membership within a tenant.", true);
    }
    withGroups() {
        return this.#with("groups", "group", "Groups", "Manage tenant-scoped authorization groups.", true);
    }
    withPolicies() {
        return this.#with("policies", "policy", "Managed policies", "Manage shared versioned policy definitions and capability statements.", false);
    }
    withSecurityModels() {
        return this.#with("security-models", "security-model", "Security models", "Inspect registered application capabilities and RBAC context.", false);
    }
    withResourceScopes() {
        return this.#with("resource-scopes", "resource-scope", "Resource scopes", "Manage application-defined resource hierarchy.", true);
    }
    withMfa() {
        return this.#with("mfa", "mfa-policy", "Multi-factor authentication", "Manage provider-neutral MFA policy and authenticators.", false);
    }
    withSessions() {
        return this.#with("sessions", "session", "Sessions", "Revoke active user or client sessions.", false);
    }
    withSecurityAudit() {
        return this.#with("security-audit", "security-audit", "Security audit", "Inspect bounded secret-safe security events.", false);
    }
    withScopeAuthority() {
        return this.#with("scope-authority", "scope-authority-group", "Scope authority", "Manage identity-scope administration authority.", false);
    }
    /** Adds every currently supported administration section in stable navigation order. */
    withAll() {
        return this
            .withUsers()
            .withTenants()
            .withMemberships()
            .withGroups()
            .withPolicies()
            .withSecurityModels()
            .withResourceScopes()
            .withMfa()
            .withSessions()
            .withSecurityAudit()
            .withScopeAuthority();
    }
    build() {
        return Object.freeze({
            basePath: this.#basePath,
            entries: Object.freeze(Array.from(this.#entries.values())),
        });
    }
    /**
     * Returns only sections for which the current trusted context has the section's read capability.
     * This is presentation filtering only and never substitutes server-side authorization.
     */
    async buildVisible(signal) {
        const visible = [];
        for (const entry of this.#entries.values()) {
            const authorizations = entry.tenantScoped && this.#tenantAuthorizations.length > 0
                ? this.#tenantAuthorizations
                : [this.#authorization];
            for (const authorization of authorizations) {
                if (await authorization.isAllowedRequirement(entry.requirement, signal)) {
                    visible.push(entry);
                    break;
                }
            }
        }
        return Object.freeze({ basePath: this.#basePath, entries: Object.freeze(visible) });
    }
    #with(section, feature, label, description, tenantScoped) {
        const requirement = Object.freeze({
            resource: "identity-access",
            feature,
            action: "read",
        });
        this.#entries.set(section, Object.freeze({
            section,
            requirement,
            href: `${this.#basePath}/${section}`,
            label,
            description,
            tenantScoped,
        }));
        return this;
    }
    static basePath(value) {
        if (!/^\/[A-Za-z0-9/_-]*$/.test(value) || value.includes("//")) {
            throw new IdentityAccessClientError("configuration");
        }
        if (value.length > 1 && value.endsWith("/"))
            return value.slice(0, -1);
        return value;
    }
}
