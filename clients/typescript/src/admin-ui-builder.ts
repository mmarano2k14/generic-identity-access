import type {
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
  readonly #entries = new Map<IdentityAccessAdminUiSection, IdentityAccessAdminUiEntry>();

  public constructor(authorization: IdentityAuthorizationContext) {
    if (!(authorization instanceof IdentityAuthorizationContext)) {
      throw new IdentityAccessClientError("configuration");
    }
    this.#authorization = authorization;
  }

  public withUsers(): this {
    return this.#with("users", "user");
  }

  public withTenants(): this {
    return this.#with("tenants", "tenant");
  }

  public withMemberships(): this {
    return this.#with("memberships", "tenant-membership");
  }

  public withGroups(): this {
    return this.#with("groups", "group");
  }

  public withPolicies(): this {
    return this.#with("policies", "policy");
  }

  public withResourceScopes(): this {
    return this.#with("resource-scopes", "resource-scope");
  }

  public withSessions(): this {
    return this.#with("sessions", "session");
  }

  public withScopeAuthority(): this {
    return this.#with("scope-authority", "scope-authority-group");
  }

  public build(): IdentityAccessAdminUiDefinition {
    return Object.freeze({
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
      if (await this.#authorization.isAllowedRequirement(entry.requirement, signal)) {
        visible.push(entry);
      }
    }

    return Object.freeze({ entries: Object.freeze(visible) });
  }

  #with(section: IdentityAccessAdminUiSection, feature: string): this {
    const requirement: IdentityCapabilityRequirement = Object.freeze({
      resource: "identity-access",
      feature,
      action: "read",
    });

    this.#entries.set(section, Object.freeze({ section, requirement }));
    return this;
  }
}
