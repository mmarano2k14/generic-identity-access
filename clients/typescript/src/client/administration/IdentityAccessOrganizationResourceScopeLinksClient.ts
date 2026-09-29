import type {
  IdentityOrganizationResourceScopeLinkRecord,
  IdentityTenantAdministrationContext,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

/** Manages the application-aware ResourceScope associated with an Organization. */
export class IdentityAccessOrganizationResourceScopeLinksClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async get(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityOrganizationResourceScopeLinkRecord | null> {
    const path = this.#path(context, organizationIdValue);
    return this.#admin.getNullable(
      path,
      context,
      IdentityAccessAdministrationCodec.organizationResourceScopeLinkRecord,
      signal,
    );
  }

  public async create(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    resourceScopeIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityOrganizationResourceScopeLinkRecord> {
    return this.#admin.post(
      this.#path(context, organizationIdValue),
      context,
      { resourceScopeId: IdentityAccessValueCodec.uuid(resourceScopeIdValue) },
      IdentityAccessAdministrationCodec.organizationResourceScopeLinkRecord,
      signal,
    );
  }

  public async update(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    resourceScopeIdValue: string,
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityOrganizationResourceScopeLinkRecord> {
    return this.#admin.put(
      this.#path(context, organizationIdValue),
      context,
      {
        resourceScopeId: IdentityAccessValueCodec.uuid(resourceScopeIdValue),
        expectedRowVersion: IdentityAccessValueCodec.version(expectedRowVersion),
      },
      IdentityAccessAdministrationCodec.organizationResourceScopeLinkRecord,
      signal,
    );
  }

  public async remove(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<boolean> {
    const version = IdentityAccessValueCodec.version(expectedRowVersion);
    return this.#admin.delete(
      `${this.#path(context, organizationIdValue)}?expectedRowVersion=${version}`,
      context,
      signal,
    );
  }

  #path(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
  ): string {
    const organizationId = IdentityAccessValueCodec.uuid(organizationIdValue);
    return `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${organizationId}/resource-scope-link`;
  }
}
