import "server-only";
import type { AdminEntityReferenceKind } from "../contracts/AdminEntityReferenceKind";
import type { AdminEntityReferenceOption } from "../contracts/AdminEntityReferenceOption";
import { IdentityAccessAdminEntityReferencePresentation } from "./IdentityAccessAdminEntityReferencePresentation";
import { IdentityAccessAdminManagedPolicyReferencePresentation } from "./IdentityAccessAdminManagedPolicyReferencePresentation";
import { IdentityAccessAdminTenantUserReferencePresentation } from "./IdentityAccessAdminTenantUserReferencePresentation";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";

/** Performs bounded server-side relation lookups against the protected administration API. */
export class IdentityAccessAdminEntityReferenceSearchService {
  public static readonly minimumSearchLength = 3;
  public static readonly maximumSearchLength = 128;
  public static readonly maximumResults = 20;

  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async search(
    kind: AdminEntityReferenceKind,
    queryValue: string,
    tenantId?: string,
  ): Promise<readonly AdminEntityReferenceOption[]> {
    const search = IdentityAccessAdminEntityReferenceSearchService.normalizeSearch(queryValue);
    const options = { search, limit: IdentityAccessAdminEntityReferenceSearchService.maximumResults } as const;

    switch (kind) {
      case "user":
        return IdentityAccessAdminEntityReferencePresentation.users(
          await this.#request.client.administration.users.list(this.#request.administrationContext, options),
        );
      case "tenant":
        return IdentityAccessAdminEntityReferencePresentation.tenants(
          await this.#request.client.administration.tenants.list(this.#request.administrationContext, options),
        );
      case "tenant-membership":
        return this.searchTenantMemberships(search, IdentityAccessAdminEntityReferenceSearchService.requiredTenantId(tenantId));
      case "managed-policy":
        return IdentityAccessAdminManagedPolicyReferencePresentation.policies(
          await this.#request.client.administration.managedPolicyBindings.listAvailablePolicies(
            this.#request.tenantContextFor(IdentityAccessAdminEntityReferenceSearchService.requiredTenantId(tenantId)),
            options,
          ),
        );
      case "resource-scope":
        return IdentityAccessAdminEntityReferencePresentation.resourceScopes(
          await this.#request.client.administration.resourceScopes.list(
            this.#request.tenantContextFor(IdentityAccessAdminEntityReferenceSearchService.requiredTenantId(tenantId)),
            options,
          ),
        );
      case "authority-group":
        return IdentityAccessAdminEntityReferencePresentation.scopeAuthorityGroups(
          await this.#request.client.administration.scopeAuthority.listGroups(this.#request.administrationContext, options),
        );
      case "authority-policy":
        return IdentityAccessAdminEntityReferencePresentation.scopeAuthorityPolicies(
          await this.#request.client.administration.scopeAuthority.listPolicies(this.#request.administrationContext, options),
        );
    }
  }

  private async searchTenantMemberships(
    search: string,
    tenantId: string,
  ): Promise<readonly AdminEntityReferenceOption[]> {
    const tenantContext = this.#request.tenantContextFor(tenantId);
    const records = await this.#request.client.administration.tenantUsers.list(tenantContext, {
      search,
      limit: IdentityAccessAdminEntityReferenceSearchService.maximumResults,
      activeMembershipsOnly: true,
    });

    return IdentityAccessAdminTenantUserReferencePresentation.tenantUsers(records);
  }

  private static normalizeSearch(value: string): string {
    const search = value.trim();
    if (search.length < this.minimumSearchLength || search.length > this.maximumSearchLength) {
      throw new RangeError(
        `Search must contain between ${this.minimumSearchLength} and ${this.maximumSearchLength} characters.`,
      );
    }
    return search;
  }

  private static requiredTenantId(value: string | undefined): string {
    const tenantId = value?.trim();
    if (!tenantId) throw new RangeError("A tenant is required for this reference lookup.");
    return tenantId;
  }
}
