import type {
  IdentityAddScopeTypeRequest,
  IdentityAdministrationContext,
  IdentityApplicationSecurityManifestRequest,
  IdentityApplicationSecurityModelRecord,
  IdentityApplicationSecurityModelSummaryRecord,
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

  public async list(
    context: IdentityAdministrationContext,
    signal?: AbortSignal,
  ): Promise<readonly IdentityApplicationSecurityModelSummaryRecord[]> {
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/security-models`;
    return this.#admin.get(
      path,
      context,
      (value) => IdentityAccessValueCodec.array(
        value,
        (entry) => IdentityAccessAdministrationCodec.applicationSecurityModelSummaryRecord(entry),
      ),
      signal,
    );
  }

  public async get(
    context: IdentityAdministrationContext,
    modelVersionValue: number,
    signal?: AbortSignal,
  ): Promise<IdentityApplicationSecurityModelRecord | null> {
    const modelVersion = IdentityAccessValueCodec.positiveInteger(modelVersionValue);
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/security-models/${modelVersion}`;
    return this.#admin.getNullable(
      path,
      context,
      (value) => IdentityAccessAdministrationCodec.applicationSecurityModelRecord(value),
      signal,
    );
  }

  public async registerManifest(
    context: IdentityAdministrationContext,
    request: IdentityApplicationSecurityManifestRequest,
    signal?: AbortSignal,
  ): Promise<IdentityApplicationSecurityModelRecord> {
    const modelVersion = IdentityAccessValueCodec.positiveInteger(request.modelVersion);
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/security-models/${modelVersion}`;
    return this.#admin.put(
      path,
      context,
      IdentityAccessAdministrationCodec.applicationSecurityManifestBody(request),
      (value) => IdentityAccessAdministrationCodec.applicationSecurityModelRecord(value),
      signal,
    );
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
