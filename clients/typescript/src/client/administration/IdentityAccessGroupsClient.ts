import type {
  IdentityCreateGroupRequest,
  IdentityGroupMemberRecord,
  IdentityGroupRecord,
  IdentityTenantAdministrationContext,
  IdentityAdministrationListOptions,
  IdentityUpdateGroupRequest,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

export class IdentityAccessGroupsClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async list(
    context: IdentityTenantAdministrationContext,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityGroupRecord[]> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups${IdentityAccessPathBuilder.administrationListQuery(options)}`;
    return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.groupRecord), signal);
  }

  public async get(
    context: IdentityTenantAdministrationContext,
    groupIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityGroupRecord | null> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}`;
    return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.groupRecord(value), signal);
  }

  public async create(
    context: IdentityTenantAdministrationContext,
    request: IdentityCreateGroupRequest,
    signal?: AbortSignal,
  ): Promise<IdentityGroupRecord> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups`;
    return this.#admin.post(path, context, {
      groupId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.groupId),
      displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
    }, (value) => IdentityAccessAdministrationCodec.groupRecord(value), signal);
  }

  public async update(
    context: IdentityTenantAdministrationContext,
    groupIdValue: string,
    request: IdentityUpdateGroupRequest,
    signal?: AbortSignal,
  ): Promise<IdentityGroupRecord> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}`;
    return this.#admin.put(path, context, {
      displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(request.status),
      expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
    }, (value) => IdentityAccessAdministrationCodec.groupRecord(value), signal);
  }

  public async listMembers(
    context: IdentityTenantAdministrationContext,
    groupIdValue: string,
    signal?: AbortSignal,
  ): Promise<readonly IdentityGroupMemberRecord[]> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/members`;
    return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.groupMemberRecord), signal);
  }

  public async addMember(
    context: IdentityTenantAdministrationContext,
    groupIdValue: string,
    tenantMembershipIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityGroupMemberRecord> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/members`;
    return this.#admin.post(path, context, {
      tenantMembershipId: IdentityAccessValueCodec.uuid(tenantMembershipIdValue),
    }, (value) => IdentityAccessAdministrationCodec.groupMemberRecord(value), signal);
  }

  public async removeMember(
    context: IdentityTenantAdministrationContext,
    groupIdValue: string,
    tenantMembershipIdValue: string,
    signal?: AbortSignal,
  ): Promise<boolean> {
    const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/members/${IdentityAccessValueCodec.uuid(tenantMembershipIdValue)}`;
    return this.#admin.delete(path, context, signal);
  }
}
