export type FetchTransport = (url: string, init: RequestInit) => Promise<Response>;
export interface IdentityAccessClientOptions {
    /** Trusted deployment configuration, never a value from a browser request. */
    readonly baseUrl: string;
    readonly timeoutMs?: number;
    /** An injected transport must honor AbortSignal and the requested redirect mode. */
    readonly fetch?: FetchTransport;
}
export interface IdentityAccessRequestOptions {
    readonly method: "GET" | "POST" | "PUT" | "DELETE";
    readonly acceptedStatuses: readonly number[];
    readonly headers?: Readonly<Record<string, string>>;
    readonly body?: string;
    readonly signal?: AbortSignal;
    readonly redirect?: RequestRedirect;
}
/** Owns only HTTP destination, timeout, cancellation, and status handling. */
export declare class IdentityAccessHttpTransport {
    #private;
    constructor(options: IdentityAccessClientOptions);
    requestJson<T>(path: string, options: IdentityAccessRequestOptions, decode: (body: unknown, status: number) => T): Promise<T>;
    requestNullableJson<T>(path: string, options: IdentityAccessRequestOptions, decode: (body: unknown, status: number) => T): Promise<T | null>;
    requestNoContent(path: string, options: IdentityAccessRequestOptions): Promise<boolean>;
    perform<T>(path: string, options: IdentityAccessRequestOptions, decode: (response: Response) => Promise<T>): Promise<T>;
    private static parseBaseAddress;
    private static statusError;
}
