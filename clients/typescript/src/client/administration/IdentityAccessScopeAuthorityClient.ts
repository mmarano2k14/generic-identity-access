import type {
  IdentityAddPolicyStatementRequest,
  IdentityAdministrationContext,
  IdentityAdministrationListOptions,
  IdentityCreateGroupRequest,
  IdentityCreatePolicyRequest,
  IdentityScopeAuthorityGroupRecord,
  IdentityScopeAuthorityMemberRecord,
  IdentityScopeAuthorityPolicyBindingRecord,
  IdentityScopeAuthorityPolicyRecord,
  IdentityScopeAuthorityPolicyStatementRecord,
  IdentityUpdateGroupRequest,
  IdentityUpdatePolicyRequest,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

export class IdentityAccessScopeAuthorityClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async listGroups(
    context: IdentityAdministrationContext,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityScopeAuthorityGroupRecord[]> {
    const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups${IdentityAccessPathBuilder.administrationListQuery(options)}`;
    return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.scopeAuthorityGroupRecord), signal);
  }

  public async getGroup(
    context: IdentityAdministrationContext,
    groupIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityScopeAuthorityGroupRecord | null> {
    const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}`;
    return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.scopeAuthorityGroupRecord(value), signal);
  }

  public async createGroup(
    context: IdentityAdministrationContext,
    request: IdentityCreateGroupRequest,
    signal?: AbortSignal,
  ): Promise<IdentityScopeAuthorityGroupRecord> {
    const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups`;
    return this.#admin.post(path, context, {
      groupId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.groupId),
      displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
    }, (value) => IdentityAccessAdministrationCodec.scopeAuthorityGroupRecord(value), signal);
  }

  public async updateGroup(
    context: IdentityAdministrationContext,
    groupIdValue: string,
    request: IdentityUpdateGroupRequest,
    signal?: AbortSignal,
  ): Promise<IdentityScopeAuthorityGroupRecord> {
    const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}`;
    return this.#admin.put(path, context, {
      displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(request.status),
      expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
    }, (value) => IdentityAccessAdministrationCodec.scopeAuthorityGroupRecord(value), signal);
  }

  public async listMembers(
    context: IdentityAdministrationContext,
    groupIdValue: string,
    signal?: AbortSignal,
  ): Promise<readonly IdentityScopeAuthorityMemberRecord[]> {
    const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/members`;
    return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.scopeAuthorityMemberRecord), signal);
  }

  public async addMember(
    context: IdentityAdministrationContext,
    groupIdValue: string,
    userIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityScopeAuthorityMemberRecord> {
    const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/members`;
    return this.#admin.post(path, context, { userId: IdentityAccessValueCodec.uuid(userIdValue) }, (value) => IdentityAccessAdministrationCodec.scopeAuthorityMemberRecord(value), signal);
  }

  public async removeMember(
    context: IdentityAdministrationContext,
    groupIdValue: string,
    userIdValue: string,
    signal?: AbortSignal,
  ): Promise<boolean> {
    const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/members/${IdentityAccessValueCodec.uuid(userIdValue)}`;
    return this.#admin.delete(path, context, signal);
  }

  public async listPolicies(
    context: IdentityAdministrationContext,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityScopeAuthorityPolicyRecord[]> {
    const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/policies${IdentityAccessPathBuilder.administrationListQuery(options)}`;
    return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.policyRecord), signal);
  }

  public async getPolicy(
    context: IdentityAdministrationContext,
    policyIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityScopeAuthorityPolicyRecord | null> {
    const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}`;
    return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.policyRecord(value), signal);
  }

  public async createPolicy(
    context: IdentityAdministrationContext,
    request: IdentityCreatePolicyRequest,
    signal?: AbortSignal,
  ): Promise<IdentityScopeAuthorityPolicyRecord> {
    const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/policies`;
    return this.#admin.post(path, context, {
      policyId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.policyId),
      displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
    }, (value) => IdentityAccessAdministrationCodec.policyRecord(value), signal);
  }

  public async updatePolicy(
    context: IdentityAdministrationContext,
    policyIdValue: string,
    request: IdentityUpdatePolicyRequest,
    signal?: AbortSignal,
  ): Promise<IdentityScopeAuthorityPolicyRecord> {
    const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}`;
    return this.#admin.put(path, context, {
      displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(request.status),
      expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
    }, (value) => IdentityAccessAdministrationCodec.policyRecord(value), signal);
  }

  public async listPolicyStatements(
    context: IdentityAdministrationContext,
    policyIdValue: string,
    signal?: AbortSignal,
  ): Promise<readonly IdentityScopeAuthorityPolicyStatementRecord[]> {
    const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}/statements`;
    return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.policyStatementRecord), signal);
  }

  public async addPolicyStatement(
    context: IdentityAdministrationContext,
    policyIdValue: string,
    request: IdentityAddPolicyStatementRequest,
    signal?: AbortSignal,
  ): Promise<IdentityScopeAuthorityPolicyStatementRecord> {
    const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}/statements`;
    return this.#admin.post(path, context, IdentityAccessAdministrationCodec.policyStatementBody(request), (value) => IdentityAccessAdministrationCodec.policyStatementRecord(value), signal);
  }

  public async removePolicyStatement(
    context: IdentityAdministrationContext,
    policyIdValue: string,
    statementIdValue: string,
    signal?: AbortSignal,
  ): Promise<boolean> {
    const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}/statements/${IdentityAccessValueCodec.uuid(statementIdValue)}`;
    return this.#admin.delete(path, context, signal);
  }

  public async listPolicyBindings(
    context: IdentityAdministrationContext,
    groupIdValue: string,
    signal?: AbortSignal,
  ): Promise<readonly IdentityScopeAuthorityPolicyBindingRecord[]> {
    const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/policy-bindings`;
    return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.scopeAuthorityPolicyBindingRecord), signal);
  }

  public async addPolicyBinding(
    context: IdentityAdministrationContext,
    groupIdValue: string,
    policyIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityScopeAuthorityPolicyBindingRecord> {
    const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/policy-bindings`;
    return this.#admin.post(path, context, { policyId: IdentityAccessValueCodec.uuid(policyIdValue) }, (value) => IdentityAccessAdministrationCodec.scopeAuthorityPolicyBindingRecord(value), signal);
  }

  public async removePolicyBinding(
    context: IdentityAdministrationContext,
    groupIdValue: string,
    policyIdValue: string,
    signal?: AbortSignal,
  ): Promise<boolean> {
    const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/policy-bindings/${IdentityAccessValueCodec.uuid(policyIdValue)}`;
    return this.#admin.delete(path, context, signal);
  }
}
