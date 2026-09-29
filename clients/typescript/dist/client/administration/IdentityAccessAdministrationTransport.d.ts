import type { IdentityAdministrationContext } from "../../admin-contracts.js";
import type { IdentityJsonObject } from "../IdentityAccessValueCodec.js";
import { IdentityAccessHttpTransport } from "../IdentityAccessHttpTransport.js";
/** Shared protected administration HTTP operations used by focused administration clients. */
export declare class IdentityAccessAdministrationTransport {
    #private;
    constructor(transport: IdentityAccessHttpTransport);
    get<T>(path: string, context: IdentityAdministrationContext, decode: (body: unknown, status: number) => T, signal?: AbortSignal): Promise<T>;
    getNullable<T>(path: string, context: IdentityAdministrationContext, decode: (body: unknown, status: number) => T, signal?: AbortSignal): Promise<T | null>;
    post<T>(path: string, context: IdentityAdministrationContext, body: IdentityJsonObject, decode: (body: unknown, status: number) => T, signal?: AbortSignal): Promise<T>;
    postAction<T>(path: string, context: IdentityAdministrationContext, decode: (body: unknown, status: number) => T, signal?: AbortSignal): Promise<T>;
    /** Executes a POST action that accepts a JSON request body and returns HTTP 200 JSON. */
    postJsonAction<T>(path: string, context: IdentityAdministrationContext, body: IdentityJsonObject, decode: (body: unknown, status: number) => T, signal?: AbortSignal): Promise<T>;
    put<T>(path: string, context: IdentityAdministrationContext, body: IdentityJsonObject, decode: (body: unknown, status: number) => T, signal?: AbortSignal): Promise<T>;
    delete(path: string, context: IdentityAdministrationContext, signal?: AbortSignal): Promise<boolean>;
    deleteJson<T>(path: string, context: IdentityAdministrationContext, decode: (body: unknown, status: number) => T, signal?: AbortSignal): Promise<T>;
    private requestOptions;
    private jsonOptions;
}
