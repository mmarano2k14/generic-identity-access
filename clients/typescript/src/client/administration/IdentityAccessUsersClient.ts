import type {
  IdentityAdministrationContext,
  IdentityAdministrationListOptions,
  IdentityCreateUserRequest,
  IdentityUpdateUserRequest,
  IdentityUserRecord,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

export class IdentityAccessUsersClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async list(
    context: IdentityAdministrationContext,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityUserRecord[]> {
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/users${IdentityAccessPathBuilder.administrationListQuery(options)}`;
    return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.userRecord), signal);
  }

  public async get(
    context: IdentityAdministrationContext,
    userIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityUserRecord | null> {
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/users/${IdentityAccessValueCodec.uuid(userIdValue)}`;
    return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.userRecord(value), signal);
  }

  public async create(
    context: IdentityAdministrationContext,
    request: IdentityCreateUserRequest,
    signal?: AbortSignal,
  ): Promise<IdentityUserRecord> {
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/users`;
    return this.#admin.post(path, context, {
      userId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.userId),
      displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
    }, (value) => IdentityAccessAdministrationCodec.userRecord(value), signal);
  }

  public async update(
    context: IdentityAdministrationContext,
    userIdValue: string,
    request: IdentityUpdateUserRequest,
    signal?: AbortSignal,
  ): Promise<IdentityUserRecord> {
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/users/${IdentityAccessValueCodec.uuid(userIdValue)}`;
    return this.#admin.put(path, context, {
      displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(request.status),
      expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
    }, (value) => IdentityAccessAdministrationCodec.userRecord(value), signal);
  }
}
