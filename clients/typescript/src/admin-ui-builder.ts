import type {
  IdentityAccessAdminUiBuilderOptions,
  IdentityAccessAdminUiDefinition,
  IdentityAccessAdminUiEntry,
  IdentityAccessAdminUiSection,
  IdentityCapabilityRequirement,
} from "./contracts.js";
import { IdentityAuthorizationContext } from "./authorization-context.js";
import { IdentityAccessClientError } from "./errors.js";

/**
 * Class-based builder for a reusable administration UI definition.
 * The builder never treats UI visibility as authorization; the .NET API remains authoritative.
 */
export class IdentityAccessAdminUiBuilder {
  readonly #authorization: IdentityAuthorizationContext;
  readonly #basePath: string;
  readonly #entries = new Map<IdentityAccessAdminUiSection, IdentityAccessAdminUiEntry>();
  #tenantAuthorizations: readonly IdentityAuthorizationContext[] = [];

  public constructor(
    authorization: IdentityAuthorizationContext,
    options: IdentityAccessAdminUiBuilderOptions = {},
  ) {
    if (!(authorization instanceof IdentityAuthorizationContext)) {
      throw new IdentityAccessClientError("configuration");
    }
    this.#authorization = authorization;
    this.#basePath = IdentityAccessAdminUiBuilder.basePath(options.basePath ?? "/identity");
  }

  /** Supplies the tenant-scoped authorization context used for tenant-bound UI sections. */
  public withTenantAuthorization(authorization: IdentityAuthorizationContext): this {
    return this.withTenantAuthorizations([authorization]);
  }

  /** Supplies all trusted active tenant contexts used for tenant-aware presentation filtering. */
  public withTenantAuthorizations(authorizations: readonly IdentityAuthorizationContext[]): this {
    if (authorizations.some((authorization) => !(authorization instanceof IdentityAuthorizationContext))) {
      throw new IdentityAccessClientError("configuration");
    }
    this.#tenantAuthorizations = Object.freeze([...authorizations]);
    return this;
  }

  public withUsers(): this {
    return this.#with("users", "user", "Users", "Manage identities and account lifecycle.", true);
  }

  public withTenants(): this {
    return this.#with("tenants", "tenant", "Tenants", "Manage tenant security boundaries.", false);
  }

  public withMemberships(): this {
    return this.#with("memberships", "tenant-membership", "Memberships", "Manage user membership within a tenant.", true);
  }

  public withGroups(): this {
    return this.#with("groups", "group", "Groups", "Manage tenant-scoped authorization groups.", true);
  }

  public withPolicies(): this {
    return this.#with("policies", "policy", "Managed policies", "Manage shared versioned policy definitions and capability statements.", false);
  }

  public withSecurityModels(): this {
    return this.#with("security-models", "security-model", "Security models", "Inspect registered application capabilities and RBAC context.", false);
  }

  public withResourceScopes(): this {
    return this.#with("resource-scopes", "resource-scope", "Resource scopes", "Manage application-defined resource hierarchy.", true);
  }

  public withMfa(): this {
    return this.#with("mfa", "mfa-policy", "Multi-factor authentication", "Manage provider-neutral MFA policy and authenticators.", false);
  }

  public withSessions(): this {
    return this.#with("sessions", "session", "Sessions", "Revoke active user or client sessions.", false);
  }

  public withSecurityAudit(): this {
    return this.#with("security-audit", "security-audit", "Security audit", "Inspect bounded secret-safe security events.", false);
  }

  public withScopeAuthority(): this {
    return this.#with("scope-authority", "scope-authority-group", "Scope authority", "Manage identity-scope administration authority.", false);
  }

  /** Adds every currently supported administration section in stable navigation order. */
  public withAll(): this {
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

  public build(): IdentityAccessAdminUiDefinition {
    return Object.freeze({
      basePath: this.#basePath,
      entries: Object.freeze(Array.from(this.#entries.values())),
    });
  }

  /**
   * Returns only sections for which the current trusted context has the section's read capability.
   * This is presentation filtering only and never substitutes server-side authorization.
   */
  public async buildVisible(signal?: AbortSignal): Promise<IdentityAccessAdminUiDefinition> {
    const visible: IdentityAccessAdminUiEntry[] = [];
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

  #with(
    section: IdentityAccessAdminUiSection,
    feature: string,
    label: string,
    description: string,
    tenantScoped: boolean,
  ): this {
    const requirement: IdentityCapabilityRequirement = Object.freeze({
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

  static basePath(value: string): string {
    if (!/^\/[A-Za-z0-9/_-]*$/.test(value) || value.includes("//")) {
      throw new IdentityAccessClientError("configuration");
    }
    if (value.length > 1 && value.endsWith("/")) return value.slice(0, -1);
    return value;
  }
}
