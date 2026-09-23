import type {
  IdentityAddGroupPolicyBindingRequest,
  IdentityAddPolicyStatementRequest,
  IdentityAdministrationListOptions,
  IdentityCreatePolicyRequest,
  IdentityGroupPolicyBindingRecord,
  IdentityPolicyRecord,
  IdentityPolicyStatementRecord,
  IdentityTenantAdministrationContext,
  IdentityUpdatePolicyRequest,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

export class IdentityAccessPoliciesClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async list(
    context: IdentityTenantAdministrationContext,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityPolicyRecord[]> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/policies${IdentityAccessPathBuilder.administrationListQuery(options)}`;
    return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.policyRecord), signal);
  }

  public async get(
    context: IdentityTenantAdministrationContext,
    policyIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityPolicyRecord | null> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}`;
    return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.policyRecord(value), signal);
  }

  public async create(
    context: IdentityTenantAdministrationContext,
    request: IdentityCreatePolicyRequest,
    signal?: AbortSignal,
  ): Promise<IdentityPolicyRecord> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/policies`;
    return this.#admin.post(path, context, {
      policyId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.policyId),
      displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
    }, (value) => IdentityAccessAdministrationCodec.policyRecord(value), signal);
  }

  public async update(
    context: IdentityTenantAdministrationContext,
    policyIdValue: string,
    request: IdentityUpdatePolicyRequest,
    signal?: AbortSignal,
  ): Promise<IdentityPolicyRecord> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}`;
    return this.#admin.put(path, context, {
      displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(request.status),
      expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
    }, (value) => IdentityAccessAdministrationCodec.policyRecord(value), signal);
  }

  public async listStatements(
    context: IdentityTenantAdministrationContext,
    policyIdValue: string,
    signal?: AbortSignal,
  ): Promise<readonly IdentityPolicyStatementRecord[]> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}/statements`;
    return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.policyStatementRecord), signal);
  }

  public async addStatement(
    context: IdentityTenantAdministrationContext,
    policyIdValue: string,
    request: IdentityAddPolicyStatementRequest,
    signal?: AbortSignal,
  ): Promise<IdentityPolicyStatementRecord> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}/statements`;
    return this.#admin.post(path, context, IdentityAccessAdministrationCodec.policyStatementBody(request), (value) => IdentityAccessAdministrationCodec.policyStatementRecord(value), signal);
  }

  public async removeStatement(
    context: IdentityTenantAdministrationContext,
    policyIdValue: string,
    statementIdValue: string,
    signal?: AbortSignal,
  ): Promise<boolean> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}/statements/${IdentityAccessValueCodec.uuid(statementIdValue)}`;
    return this.#admin.delete(path, context, signal);
  }

  public async listBindings(
    context: IdentityTenantAdministrationContext,
    groupIdValue: string,
    signal?: AbortSignal,
  ): Promise<readonly IdentityGroupPolicyBindingRecord[]> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/policy-bindings`;
    return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.groupPolicyBindingRecord), signal);
  }

  public async addBinding(
    context: IdentityTenantAdministrationContext,
    groupIdValue: string,
    request: IdentityAddGroupPolicyBindingRequest,
    signal?: AbortSignal,
  ): Promise<IdentityGroupPolicyBindingRecord> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/policy-bindings`;
    return this.#admin.post(path, context, {
      policyId: IdentityAccessValueCodec.uuid(request.policyId),
      resourceScopeId: request.resourceScopeId === undefined ? null : IdentityAccessValueCodec.uuid(request.resourceScopeId),
      includeDescendants: request.includeDescendants ?? false,
    }, (value) => IdentityAccessAdministrationCodec.groupPolicyBindingRecord(value), signal);
  }

  public async removeBinding(
    context: IdentityTenantAdministrationContext,
    groupIdValue: string,
    policyIdValue: string,
    resourceScopeIdValue?: string,
    signal?: AbortSignal,
  ): Promise<boolean> {
    const query = resourceScopeIdValue === undefined
      ? ""
      : `?resourceScopeId=${encodeURIComponent(IdentityAccessValueCodec.uuid(resourceScopeIdValue))}`;
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/policy-bindings/${IdentityAccessValueCodec.uuid(policyIdValue)}${query}`;
    return this.#admin.delete(path, context, signal);
  }
}
