import type { IdentityAdministrationContext } from "../../admin-contracts.js";
import type { IdentityJsonObject } from "../IdentityAccessValueCodec.js";
import { IdentityAccessHttpTransport } from "../IdentityAccessHttpTransport.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";

/** Shared protected administration HTTP operations used by focused administration clients. */
export class IdentityAccessAdministrationTransport {
  readonly #transport: IdentityAccessHttpTransport;

  public constructor(transport: IdentityAccessHttpTransport) {
    this.#transport = transport;
  }

  public async get<T>(
    path: string,
    context: IdentityAdministrationContext,
    decode: (body: unknown, status: number) => T,
    signal?: AbortSignal,
  ): Promise<T> {
    return this.#transport.requestJson(path, this.requestOptions("GET", context, signal), decode);
  }

  public async getNullable<T>(
    path: string,
    context: IdentityAdministrationContext,
    decode: (body: unknown, status: number) => T,
    signal?: AbortSignal,
  ): Promise<T | null> {
    return this.#transport.requestNullableJson(path, this.requestOptions("GET", context, signal), decode);
  }

  public async post<T>(
    path: string,
    context: IdentityAdministrationContext,
    body: IdentityJsonObject,
    decode: (body: unknown, status: number) => T,
    signal?: AbortSignal,
  ): Promise<T> {
    return this.#transport.requestJson(path, this.jsonOptions("POST", context, body, signal, [201]), decode);
  }

  public async put<T>(
    path: string,
    context: IdentityAdministrationContext,
    body: IdentityJsonObject,
    decode: (body: unknown, status: number) => T,
    signal?: AbortSignal,
  ): Promise<T> {
    return this.#transport.requestJson(path, this.jsonOptions("PUT", context, body, signal, [200]), decode);
  }

  public async delete(
    path: string,
    context: IdentityAdministrationContext,
    signal?: AbortSignal,
  ): Promise<boolean> {
    return this.#transport.requestNoContent(path, this.requestOptions("DELETE", context, signal));
  }

  public async deleteJson<T>(
    path: string,
    context: IdentityAdministrationContext,
    decode: (body: unknown, status: number) => T,
    signal?: AbortSignal,
  ): Promise<T> {
    return this.#transport.requestJson(path, this.requestOptions("DELETE", context, signal), decode);
  }

  private requestOptions(
    method: "GET" | "DELETE",
    context: IdentityAdministrationContext,
    signal?: AbortSignal,
  ) {
    return {
      method,
      acceptedStatuses: [200],
      headers: IdentityAccessPathBuilder.credentialHeaders(context.credential),
      ...(signal === undefined ? {} : { signal }),
    } as const;
  }

  private jsonOptions(
    method: "POST" | "PUT",
    context: IdentityAdministrationContext,
    body: IdentityJsonObject,
    signal: AbortSignal | undefined,
    acceptedStatuses: readonly number[],
  ) {
    return {
      method,
      acceptedStatuses,
      headers: {
        ...IdentityAccessPathBuilder.credentialHeaders(context.credential),
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
      ...(signal === undefined ? {} : { signal }),
    } as const;
  }
}
