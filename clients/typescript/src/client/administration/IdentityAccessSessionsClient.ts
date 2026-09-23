import type {
  IdentityAdministrationContext,
  IdentitySessionRevocationResult,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

export class IdentityAccessSessionsClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async revokeUser(
    context: IdentityAdministrationContext,
    userIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentitySessionRevocationResult> {
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/sessions/users/${IdentityAccessValueCodec.uuid(userIdValue)}`;
    return this.#admin.deleteJson(path, context, (value) => IdentityAccessAdministrationCodec.sessionRevocationResult(value), signal);
  }

  public async revokeClient(
    context: IdentityAdministrationContext,
    clientIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentitySessionRevocationResult> {
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/sessions/clients/${encodeURIComponent(IdentityAccessValueCodec.clientId(clientIdValue))}`;
    return this.#admin.deleteJson(path, context, (value) => IdentityAccessAdministrationCodec.sessionRevocationResult(value), signal);
  }
}
