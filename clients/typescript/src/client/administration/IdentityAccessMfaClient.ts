import type {
  IdentityAdministrationContext,
  IdentityCreateMfaPolicyRequest,
  IdentityMfaPolicyMode,
  IdentityMfaPolicyRecord,
  IdentityMfaProviderCapability,
  IdentityMfaProviderRecord,
  IdentityUpdateMfaPolicyRequest,
  IdentityUserAuthenticatorRecord,
} from "../../admin-contracts.js";
import { IdentityAccessClientError } from "../../errors.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

/** Provider-neutral MFA administration client. */
export class IdentityAccessMfaClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async listProviders(
    context: IdentityAdministrationContext,
    signal?: AbortSignal,
  ): Promise<readonly IdentityMfaProviderRecord[]> {
    return this.#admin.get(
      `${this.#base(context)}/providers`,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessMfaClient.providerRecord),
      signal,
    );
  }

  public async getPolicy(
    context: IdentityAdministrationContext,
    signal?: AbortSignal,
  ): Promise<IdentityMfaPolicyRecord | null> {
    return this.#admin.getNullable(
      `${this.#base(context)}/policy`,
      context,
      (value) => IdentityAccessMfaClient.policyRecord(value),
      signal,
    );
  }

  public async createPolicy(
    context: IdentityAdministrationContext,
    request: IdentityCreateMfaPolicyRequest,
    signal?: AbortSignal,
  ): Promise<IdentityMfaPolicyRecord> {
    return this.#admin.post(
      `${this.#base(context)}/policy`,
      context,
      {
        mode: IdentityAccessMfaClient.policyMode(request.mode),
        allowedProviders: request.allowedProviders.map(IdentityAccessValueCodec.slug),
      },
      (value) => IdentityAccessMfaClient.policyRecord(value),
      signal,
    );
  }

  public async updatePolicy(
    context: IdentityAdministrationContext,
    request: IdentityUpdateMfaPolicyRequest,
    signal?: AbortSignal,
  ): Promise<IdentityMfaPolicyRecord> {
    return this.#admin.put(
      `${this.#base(context)}/policy`,
      context,
      {
        mode: IdentityAccessMfaClient.policyMode(request.mode),
        allowedProviders: request.allowedProviders.map(IdentityAccessValueCodec.slug),
        expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
      },
      (value) => IdentityAccessMfaClient.policyRecord(value),
      signal,
    );
  }

  public async listAuthenticators(
    context: IdentityAdministrationContext,
    userIdValue: string,
    signal?: AbortSignal,
  ): Promise<readonly IdentityUserAuthenticatorRecord[]> {
    const userId = IdentityAccessValueCodec.uuid(userIdValue);
    return this.#admin.get(
      `${this.#base(context)}/users/${userId}/authenticators`,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessMfaClient.authenticatorRecord),
      signal,
    );
  }

  public async revokeAuthenticator(
    context: IdentityAdministrationContext,
    userIdValue: string,
    authenticatorIdValue: string,
    expectedVersionValue: number,
    signal?: AbortSignal,
  ): Promise<IdentityUserAuthenticatorRecord> {
    const userId = IdentityAccessValueCodec.uuid(userIdValue);
    const authenticatorId = IdentityAccessValueCodec.uuid(authenticatorIdValue);
    const expectedVersion = IdentityAccessValueCodec.version(expectedVersionValue);
    return this.#admin.deleteJson(
      `${this.#base(context)}/users/${userId}/authenticators/${authenticatorId}?expectedVersion=${expectedVersion}`,
      context,
      (value) => IdentityAccessMfaClient.authenticatorRecord(value),
      signal,
    );
  }

  #base(context: IdentityAdministrationContext): string {
    return `${IdentityAccessPathBuilder.administrationBasePath(context)}/mfa`;
  }

  private static providerRecord(value: unknown): IdentityMfaProviderRecord {
    const data = IdentityAccessValueCodec.object(value);
    if (!Array.isArray(data.capabilities)) throw new IdentityAccessClientError("protocol");
    return {
      key: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.key)),
      displayName: IdentityAccessValueCodec.text(data.displayName),
      capabilities: data.capabilities.map(IdentityAccessMfaClient.providerCapability),
    };
  }

  private static providerCapability(value: unknown): IdentityMfaProviderCapability {
    const capability = IdentityAccessValueCodec.text(value);
    if (capability !== "enrollment" && capability !== "verification" && capability !== "recovery") {
      throw new IdentityAccessClientError("protocol");
    }
    return capability;
  }

  private static policyRecord(value: unknown): IdentityMfaPolicyRecord {
    const data = IdentityAccessValueCodec.object(value);
    if (!Array.isArray(data.allowedProviders)) throw new IdentityAccessClientError("protocol");
    return {
      mode: IdentityAccessMfaClient.policyMode(data.mode),
      allowedProviders: data.allowedProviders.map((item) =>
        IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(item))),
      version: IdentityAccessValueCodec.version(data.version),
    };
  }

  private static authenticatorRecord(value: unknown): IdentityUserAuthenticatorRecord {
    const data = IdentityAccessValueCodec.object(value);
    const confirmedAt = IdentityAccessMfaClient.optionalTimestamp(data.confirmedAt);
    const lastUsedAt = IdentityAccessMfaClient.optionalTimestamp(data.lastUsedAt);
    const revokedAt = IdentityAccessMfaClient.optionalTimestamp(data.revokedAt);
    return {
      authenticatorId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.authenticatorId)),
      userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
      providerKey: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.providerKey)),
      displayName: IdentityAccessValueCodec.text(data.displayName),
      status: IdentityAccessMfaClient.authenticatorStatus(data.status),
      createdAt: IdentityAccessValueCodec.timestamp(data.createdAt),
      ...(confirmedAt === undefined ? {} : { confirmedAt }),
      ...(lastUsedAt === undefined ? {} : { lastUsedAt }),
      ...(revokedAt === undefined ? {} : { revokedAt }),
      version: IdentityAccessValueCodec.version(data.version),
    };
  }

  private static optionalTimestamp(value: unknown): string | undefined {
    return value === null || value === undefined ? undefined : IdentityAccessValueCodec.timestamp(value);
  }

  private static policyMode(value: unknown): IdentityMfaPolicyMode {
    if (value !== 1 && value !== 2 && value !== 3) throw new IdentityAccessClientError("protocol");
    return value;
  }

  private static authenticatorStatus(value: unknown): 1 | 2 | 3 {
    if (value !== 1 && value !== 2 && value !== 3) throw new IdentityAccessClientError("protocol");
    return value;
  }
}
