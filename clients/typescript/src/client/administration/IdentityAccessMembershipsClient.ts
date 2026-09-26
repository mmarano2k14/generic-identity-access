import type {
  IdentityAdministrationListOptions,
  IdentityCreateTenantMembershipRequest,
  IdentityTenantAdministrationContext,
  IdentityTenantMembershipRecord,
  IdentityUpdateTenantMembershipRequest,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

export class IdentityAccessMembershipsClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async list(
    context: IdentityTenantAdministrationContext,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityTenantMembershipRecord[]> {
    const path = `${IdentityAccessPathBuilder.tenantMembershipsPath(context)}${IdentityAccessPathBuilder.administrationListQuery(options)}`;
    return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.tenantMembershipRecord), signal);
  }

  public async get(
    context: IdentityTenantAdministrationContext,
    membershipIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityTenantMembershipRecord | null> {
    const path = `${IdentityAccessPathBuilder.tenantMembershipsPath(context)}/${IdentityAccessValueCodec.uuid(membershipIdValue)}`;
    return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.tenantMembershipRecord(value), signal);
  }

  public async findByUser(
    context: IdentityTenantAdministrationContext,
    userIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityTenantMembershipRecord | null> {
    const path = `${IdentityAccessPathBuilder.tenantMembershipsPath(context)}/by-user/${IdentityAccessValueCodec.uuid(userIdValue)}`;
    return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.tenantMembershipRecord(value), signal);
  }

  public async create(
    context: IdentityTenantAdministrationContext,
    request: IdentityCreateTenantMembershipRequest,
    signal?: AbortSignal,
  ): Promise<IdentityTenantMembershipRecord> {
    const path = IdentityAccessPathBuilder.tenantMembershipsPath(context);
    return this.#admin.post(path, context, {
      membershipId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.membershipId),
      userId: IdentityAccessValueCodec.uuid(request.userId),
      status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
    }, (value) => IdentityAccessAdministrationCodec.tenantMembershipRecord(value), signal);
  }

  public async update(
    context: IdentityTenantAdministrationContext,
    membershipIdValue: string,
    request: IdentityUpdateTenantMembershipRequest,
    signal?: AbortSignal,
  ): Promise<IdentityTenantMembershipRecord> {
    const path = `${IdentityAccessPathBuilder.tenantMembershipsPath(context)}/${IdentityAccessValueCodec.uuid(membershipIdValue)}`;
    return this.#admin.put(path, context, {
      status: IdentityAccessValueCodec.lifecycleStatus(request.status),
      expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
    }, (value) => IdentityAccessAdministrationCodec.tenantMembershipRecord(value), signal);
  }
}
