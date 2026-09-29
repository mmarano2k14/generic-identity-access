import type {
  IdentityAdministrationListOptions,
  IdentityCreateOrganizationRequest,
  IdentityOrganizationRecord,
  IdentityOrganizationTreeNodeRecord,
  IdentityTenantAdministrationContext,
  IdentityUpdateOrganizationRequest,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

/** Tenant-local Organization Directory client hosted by the common Identity Access API. */
export class IdentityAccessOrganizationsClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async list(
    context: IdentityTenantAdministrationContext,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityOrganizationRecord[]> {
    const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}${IdentityAccessPathBuilder.administrationListQuery(options)}`;
    return this.#admin.get(
      path,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.organizationRecord),
      signal,
    );
  }

  public async get(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityOrganizationRecord | null> {
    const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${IdentityAccessValueCodec.uuid(organizationIdValue)}`;
    return this.#admin.getNullable(path, context, IdentityAccessAdministrationCodec.organizationRecord, signal);
  }

  public async tree(
    context: IdentityTenantAdministrationContext,
    signal?: AbortSignal,
  ): Promise<readonly IdentityOrganizationTreeNodeRecord[]> {
    const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/tree`;
    return this.#admin.get(
      path,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.organizationTreeNodeRecord),
      signal,
    );
  }

  public async children(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    signal?: AbortSignal,
  ): Promise<readonly IdentityOrganizationRecord[]> {
    const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${IdentityAccessValueCodec.uuid(organizationIdValue)}/children`;
    return this.#admin.get(
      path,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.organizationRecord),
      signal,
    );
  }

  public async create(
    context: IdentityTenantAdministrationContext,
    request: IdentityCreateOrganizationRequest,
    signal?: AbortSignal,
  ): Promise<IdentityOrganizationRecord> {
    const path = IdentityAccessPathBuilder.organizationDirectoryPath(context);
    return this.#admin.post(
      path,
      context,
      {
        organizationId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.organizationId),
        organizationKey: IdentityAccessValueCodec.slug(request.organizationKey),
        displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
        organizationType: IdentityAccessValueCodec.slug(request.organizationType),
        parentOrganizationId: request.parentOrganizationId === undefined
          ? null
          : IdentityAccessValueCodec.uuid(request.parentOrganizationId),
      },
      IdentityAccessAdministrationCodec.organizationRecord,
      signal,
    );
  }

  public async update(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    request: IdentityUpdateOrganizationRequest,
    signal?: AbortSignal,
  ): Promise<IdentityOrganizationRecord> {
    const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${IdentityAccessValueCodec.uuid(organizationIdValue)}`;
    return this.#admin.put(
      path,
      context,
      {
        displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
        organizationType: IdentityAccessValueCodec.slug(request.organizationType),
        parentOrganizationId: request.parentOrganizationId === undefined
          ? null
          : IdentityAccessValueCodec.uuid(request.parentOrganizationId),
        expectedRowVersion: IdentityAccessValueCodec.version(request.expectedRowVersion),
      },
      IdentityAccessAdministrationCodec.organizationRecord,
      signal,
    );
  }

  public async enable(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityOrganizationRecord> {
    return this.#lifecycle(context, organizationIdValue, "enable", expectedRowVersion, signal);
  }

  public async disable(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityOrganizationRecord> {
    return this.#lifecycle(context, organizationIdValue, "disable", expectedRowVersion, signal);
  }

  async #lifecycle(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    action: "enable" | "disable",
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityOrganizationRecord> {
    const organizationId = IdentityAccessValueCodec.uuid(organizationIdValue);
    const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${organizationId}/${action}`;
    return this.#admin.postJsonAction(
      path,
      context,
      { expectedRowVersion: IdentityAccessValueCodec.version(expectedRowVersion) },
      IdentityAccessAdministrationCodec.organizationRecord,
      signal,
    );
  }
}
