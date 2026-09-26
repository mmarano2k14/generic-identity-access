import type {
  IdentityAdministrationListOptions,
  IdentityCreateResourceScopeRequest,
  IdentityResourceScopeRecord,
  IdentityTenantAdministrationContext,
  IdentityUpdateResourceScopeRequest,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

export class IdentityAccessResourceScopesClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async list(
    context: IdentityTenantAdministrationContext,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityResourceScopeRecord[]> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/resource-scopes${IdentityAccessPathBuilder.administrationListQuery(options)}`;
    return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.resourceScopeRecord), signal);
  }

  public async get(
    context: IdentityTenantAdministrationContext,
    resourceScopeIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityResourceScopeRecord | null> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/resource-scopes/${IdentityAccessValueCodec.uuid(resourceScopeIdValue)}`;
    return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.resourceScopeRecord(value), signal);
  }

  public async create(
    context: IdentityTenantAdministrationContext,
    request: IdentityCreateResourceScopeRequest,
    signal?: AbortSignal,
  ): Promise<IdentityResourceScopeRecord> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/resource-scopes`;
    return this.#admin.post(path, context, IdentityAccessAdministrationCodec.resourceScopeBody(request, false), (value) => IdentityAccessAdministrationCodec.resourceScopeRecord(value), signal);
  }

  public async update(
    context: IdentityTenantAdministrationContext,
    resourceScopeIdValue: string,
    request: IdentityUpdateResourceScopeRequest,
    signal?: AbortSignal,
  ): Promise<IdentityResourceScopeRecord> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/resource-scopes/${IdentityAccessValueCodec.uuid(resourceScopeIdValue)}`;
    return this.#admin.put(path, context, IdentityAccessAdministrationCodec.resourceScopeBody(request, true), (value) => IdentityAccessAdministrationCodec.resourceScopeRecord(value), signal);
  }
}
