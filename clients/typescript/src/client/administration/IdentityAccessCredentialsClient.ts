import type {
  IdentityAdministrationContext,
  IdentityChangePasswordCredentialRequest,
  IdentityCreatePasswordCredentialRequest,
  IdentityPasswordCredentialMetadataRecord,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

/** Password-credential administration without exposing password hashes or stored secret material. */
export class IdentityAccessCredentialsClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async get(
    context: IdentityAdministrationContext,
    userIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityPasswordCredentialMetadataRecord | null> {
    return this.#admin.getNullable(
      this.#path(context, userIdValue),
      context,
      (value) => IdentityAccessAdministrationCodec.passwordCredentialMetadataRecord(value),
      signal,
    );
  }

  public async create(
    context: IdentityAdministrationContext,
    userIdValue: string,
    request: IdentityCreatePasswordCredentialRequest,
    signal?: AbortSignal,
  ): Promise<IdentityPasswordCredentialMetadataRecord> {
    return this.#admin.post(
      this.#path(context, userIdValue),
      context,
      {
        loginIdentifier: IdentityAccessCredentialsClient.#loginIdentifier(request.loginIdentifier),
        password: IdentityAccessCredentialsClient.#password(request.password),
      },
      (value) => IdentityAccessAdministrationCodec.passwordCredentialMetadataRecord(value),
      signal,
    );
  }

  public async changePassword(
    context: IdentityAdministrationContext,
    userIdValue: string,
    request: IdentityChangePasswordCredentialRequest,
    signal?: AbortSignal,
  ): Promise<IdentityPasswordCredentialMetadataRecord> {
    return this.#admin.put(
      this.#path(context, userIdValue),
      context,
      {
        loginIdentifier: IdentityAccessCredentialsClient.#loginIdentifier(request.loginIdentifier),
        password: IdentityAccessCredentialsClient.#password(request.password),
        expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
      },
      (value) => IdentityAccessAdministrationCodec.passwordCredentialMetadataRecord(value),
      signal,
    );
  }

  #path(context: IdentityAdministrationContext, userIdValue: string): string {
    const userId = IdentityAccessValueCodec.uuid(userIdValue);
    return `${IdentityAccessPathBuilder.administrationBasePath(context)}/users/${userId}/password-credential`;
  }

  static #loginIdentifier(value: string): string {
    return IdentityAccessValueCodec.nonEmpty(value.trim());
  }

  static #password(value: string): string {
    return IdentityAccessValueCodec.nonEmptySecret(value);
  }
}
