import type {
  IdentityAddManagedPolicyStatementRequest,
  IdentityAdministrationContext,
  IdentityAdministrationListOptions,
  IdentityCreateManagedPolicyRequest,
  IdentityCreateManagedPolicyVersionRequest,
  IdentityManagedPolicyRecord,
  IdentityManagedPolicyStatementRecord,
  IdentityManagedPolicyVersionRecord,
  IdentityPublishManagedPolicyVersionRequest,
  IdentityUpdateManagedPolicyRequest,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

/** Administration client for the tenant-independent managed-policy catalog. */
export class IdentityAccessManagedPoliciesClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async list(
    context: IdentityAdministrationContext,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityManagedPolicyRecord[]> {
    const path = `${this.basePath(context)}${IdentityAccessPathBuilder.administrationListQuery(options)}`;
    return this.#admin.get(
      path,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.managedPolicyRecord),
      signal,
    );
  }

  public async get(
    context: IdentityAdministrationContext,
    policyIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityManagedPolicyRecord | null> {
    return this.#admin.getNullable(
      `${this.basePath(context)}/${IdentityAccessValueCodec.uuid(policyIdValue)}`,
      context,
      (value) => IdentityAccessAdministrationCodec.managedPolicyRecord(value),
      signal,
    );
  }

  public async create(
    context: IdentityAdministrationContext,
    request: IdentityCreateManagedPolicyRequest,
    signal?: AbortSignal,
  ): Promise<IdentityManagedPolicyRecord> {
    return this.#admin.post(
      this.basePath(context),
      context,
      {
        policyId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.policyId),
        policyKey: IdentityAccessValueCodec.managedPolicyKey(request.policyKey),
        displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
        status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
      },
      (value) => IdentityAccessAdministrationCodec.managedPolicyRecord(value),
      signal,
    );
  }

  public async update(
    context: IdentityAdministrationContext,
    policyIdValue: string,
    request: IdentityUpdateManagedPolicyRequest,
    signal?: AbortSignal,
  ): Promise<IdentityManagedPolicyRecord> {
    return this.#admin.put(
      `${this.basePath(context)}/${IdentityAccessValueCodec.uuid(policyIdValue)}`,
      context,
      {
        policyKey: IdentityAccessValueCodec.managedPolicyKey(request.policyKey),
        displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
        status: IdentityAccessValueCodec.lifecycleStatus(request.status),
        expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
      },
      (value) => IdentityAccessAdministrationCodec.managedPolicyRecord(value),
      signal,
    );
  }

  public async listVersions(
    context: IdentityAdministrationContext,
    policyIdValue: string,
    signal?: AbortSignal,
  ): Promise<readonly IdentityManagedPolicyVersionRecord[]> {
    return this.#admin.get(
      `${this.basePath(context)}/${IdentityAccessValueCodec.uuid(policyIdValue)}/versions`,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.managedPolicyVersionRecord),
      signal,
    );
  }

  public async getVersion(
    context: IdentityAdministrationContext,
    policyIdValue: string,
    policyVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityManagedPolicyVersionRecord | null> {
    return this.#admin.getNullable(
      `${this.basePath(context)}/${IdentityAccessValueCodec.uuid(policyIdValue)}/versions/${IdentityAccessValueCodec.positiveInteger(policyVersion)}`,
      context,
      (value) => IdentityAccessAdministrationCodec.managedPolicyVersionRecord(value),
      signal,
    );
  }

  public async createVersion(
    context: IdentityAdministrationContext,
    policyIdValue: string,
    request: IdentityCreateManagedPolicyVersionRequest,
    signal?: AbortSignal,
  ): Promise<IdentityManagedPolicyVersionRecord> {
    return this.#admin.post(
      `${this.basePath(context)}/${IdentityAccessValueCodec.uuid(policyIdValue)}/versions`,
      context,
      {
        policyVersion: IdentityAccessValueCodec.positiveInteger(request.policyVersion),
        modelVersion: IdentityAccessValueCodec.positiveInteger(request.modelVersion),
      },
      (value) => IdentityAccessAdministrationCodec.managedPolicyVersionRecord(value),
      signal,
    );
  }

  public async publishVersion(
    context: IdentityAdministrationContext,
    policyIdValue: string,
    policyVersion: number,
    request: IdentityPublishManagedPolicyVersionRequest = {},
    signal?: AbortSignal,
  ): Promise<IdentityManagedPolicyVersionRecord> {
    return this.#admin.put(
      `${this.basePath(context)}/${IdentityAccessValueCodec.uuid(policyIdValue)}/versions/${IdentityAccessValueCodec.positiveInteger(policyVersion)}/publish`,
      context,
      { makeDefault: IdentityAccessValueCodec.boolean(request.makeDefault ?? false) },
      (value) => IdentityAccessAdministrationCodec.managedPolicyVersionRecord(value),
      signal,
    );
  }

  public async listStatements(
    context: IdentityAdministrationContext,
    policyIdValue: string,
    policyVersion: number,
    signal?: AbortSignal,
  ): Promise<readonly IdentityManagedPolicyStatementRecord[]> {
    return this.#admin.get(
      `${this.versionPath(context, policyIdValue, policyVersion)}/statements`,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.managedPolicyStatementRecord),
      signal,
    );
  }

  public async addStatement(
    context: IdentityAdministrationContext,
    policyIdValue: string,
    policyVersion: number,
    request: IdentityAddManagedPolicyStatementRequest,
    signal?: AbortSignal,
  ): Promise<IdentityManagedPolicyStatementRecord> {
    return this.#admin.post(
      `${this.versionPath(context, policyIdValue, policyVersion)}/statements`,
      context,
      {
        statementId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.statementId),
        resource: IdentityAccessValueCodec.capabilityPatternSegment(request.resource),
        feature: IdentityAccessValueCodec.capabilityPatternSegment(request.feature),
        action: IdentityAccessValueCodec.capabilityPatternSegment(request.action),
      },
      (value) => IdentityAccessAdministrationCodec.managedPolicyStatementRecord(value),
      signal,
    );
  }

  public async removeStatement(
    context: IdentityAdministrationContext,
    policyIdValue: string,
    policyVersion: number,
    statementIdValue: string,
    signal?: AbortSignal,
  ): Promise<boolean> {
    return this.#admin.delete(
      `${this.versionPath(context, policyIdValue, policyVersion)}/statements/${IdentityAccessValueCodec.uuid(statementIdValue)}`,
      context,
      signal,
    );
  }

  private basePath(context: IdentityAdministrationContext): string {
    return `${IdentityAccessPathBuilder.administrationBasePath(context)}/managed-policies`;
  }

  private versionPath(
    context: IdentityAdministrationContext,
    policyIdValue: string,
    policyVersion: number,
  ): string {
    return `${this.basePath(context)}/${IdentityAccessValueCodec.uuid(policyIdValue)}/versions/${IdentityAccessValueCodec.positiveInteger(policyVersion)}`;
  }
}
