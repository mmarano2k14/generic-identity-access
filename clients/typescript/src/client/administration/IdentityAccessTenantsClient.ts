import type {
  IdentityAdministrationContext,
  IdentityAdministrationListOptions,
  IdentityCreateTenantRequest,
  IdentityTenantRecord,
  IdentityUpdateTenantRequest,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

export class IdentityAccessTenantsClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async list(
    context: IdentityAdministrationContext,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityTenantRecord[]> {
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/tenants${IdentityAccessPathBuilder.administrationListQuery(options)}`;
    return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.tenantRecord), signal);
  }

  public async get(
    context: IdentityAdministrationContext,
    tenantIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityTenantRecord | null> {
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/tenants/${IdentityAccessValueCodec.uuid(tenantIdValue)}`;
    return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.tenantRecord(value), signal);
  }

  public async create(
    context: IdentityAdministrationContext,
    request: IdentityCreateTenantRequest,
    signal?: AbortSignal,
  ): Promise<IdentityTenantRecord> {
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/tenants`;
    return this.#admin.post(path, context, {
      tenantId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.tenantId),
      displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
    }, (value) => IdentityAccessAdministrationCodec.tenantRecord(value), signal);
  }

  public async update(
    context: IdentityAdministrationContext,
    tenantIdValue: string,
    request: IdentityUpdateTenantRequest,
    signal?: AbortSignal,
  ): Promise<IdentityTenantRecord> {
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/tenants/${IdentityAccessValueCodec.uuid(tenantIdValue)}`;
    return this.#admin.put(path, context, {
      displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(request.status),
      expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
    }, (value) => IdentityAccessAdministrationCodec.tenantRecord(value), signal);
  }
}
