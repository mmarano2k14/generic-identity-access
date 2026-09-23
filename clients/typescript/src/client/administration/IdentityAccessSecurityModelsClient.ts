import type {
  IdentityAddScopeTypeRequest,
  IdentityAdministrationContext,
  IdentityScopeTypeRecord,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

export class IdentityAccessSecurityModelsClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async listScopeTypes(
    context: IdentityAdministrationContext,
    modelVersionValue: number,
    signal?: AbortSignal,
  ): Promise<readonly IdentityScopeTypeRecord[]> {
    const modelVersion = IdentityAccessValueCodec.positiveInteger(modelVersionValue);
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/security-models/${modelVersion}/scope-types`;
    return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.scopeTypeRecord), signal);
  }

  public async addScopeType(
    context: IdentityAdministrationContext,
    modelVersionValue: number,
    request: IdentityAddScopeTypeRequest,
    signal?: AbortSignal,
  ): Promise<IdentityScopeTypeRecord> {
    const modelVersion = IdentityAccessValueCodec.positiveInteger(modelVersionValue);
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/security-models/${modelVersion}/scope-types`;
    return this.#admin.post(path, context, {
      key: IdentityAccessValueCodec.slug(request.key),
      displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
      parentKey: request.parentKey === undefined ? null : IdentityAccessValueCodec.slug(request.parentKey),
      canAttachToTenant: IdentityAccessValueCodec.boolean(request.canAttachToTenant),
    }, (value) => IdentityAccessAdministrationCodec.scopeTypeRecord(value), signal);
  }
}
