import { IdentityAccessClientError } from "../errors.js";
/** Owns only HTTP destination, timeout, cancellation, and status handling. */
export class IdentityAccessHttpTransport {
    #baseUrl;
    #timeoutMs;
    #transport;
    constructor(options) {
        if (typeof URL !== "function" || typeof AbortController !== "function") {
            throw new IdentityAccessClientError("configuration");
        }
        this.#baseUrl = IdentityAccessHttpTransport.parseBaseAddress(options.baseUrl);
        this.#timeoutMs = options.timeoutMs ?? 10_000;
        if (!Number.isSafeInteger(this.#timeoutMs) || this.#timeoutMs < 1 || this.#timeoutMs > 120_000) {
            throw new IdentityAccessClientError("configuration");
        }
        const transport = options.fetch ?? globalThis.fetch?.bind(globalThis);
        if (typeof transport !== "function") {
            throw new IdentityAccessClientError("configuration");
        }
        this.#transport = transport;
    }
    async requestJson(path, options, decode) {
        return this.perform(path, options, async (response) => {
            let body;
            try {
                body = await response.json();
            }
            catch {
                throw new IdentityAccessClientError("protocol");
            }
            return decode(body, response.status);
        });
    }
    async requestNullableJson(path, options, decode) {
        const accepted = options.acceptedStatuses.includes(404)
            ? options.acceptedStatuses
            : [...options.acceptedStatuses, 404];
        return this.perform(path, { ...options, acceptedStatuses: accepted }, async (response) => {
            if (response.status === 404)
                return null;
            let body;
            try {
                body = await response.json();
            }
            catch {
                throw new IdentityAccessClientError("protocol");
            }
            return decode(body, response.status);
        });
    }
    async requestNoContent(path, options) {
        return this.perform(path, { ...options, acceptedStatuses: [204, 404] }, async (response) => response.status === 204);
    }
    async perform(path, options, decode) {
        if (options.signal?.aborted) {
            throw new IdentityAccessClientError("cancelled");
        }
        const controller = new AbortController();
        let timedOut = false;
        const onAbort = () => controller.abort();
        options.signal?.addEventListener("abort", onAbort, { once: true });
        const timer = setTimeout(() => {
            timedOut = true;
            controller.abort();
        }, this.#timeoutMs);
        try {
            const headers = {
                Accept: "application/json",
                ...options.headers,
            };
            const response = await this.#transport(new URL(path, this.#baseUrl).toString(), {
                method: options.method,
                headers,
                ...(options.body === undefined ? {} : { body: options.body }),
                cache: "no-store",
                credentials: "omit",
                redirect: options.redirect ?? "error",
                signal: controller.signal,
            });
            if (!options.acceptedStatuses.includes(response.status)) {
                throw IdentityAccessHttpTransport.statusError(response.status);
            }
            return await decode(response);
        }
        catch (error) {
            if (error instanceof IdentityAccessClientError) {
                throw error;
            }
            if (options.signal?.aborted) {
                throw new IdentityAccessClientError("cancelled");
            }
            if (timedOut) {
                throw new IdentityAccessClientError("timeout");
            }
            throw new IdentityAccessClientError("transport");
        }
        finally {
            clearTimeout(timer);
            options.signal?.removeEventListener("abort", onAbort);
        }
    }
    static parseBaseAddress(value) {
        if (typeof value !== "string" || value !== value.trim()) {
            throw new IdentityAccessClientError("configuration");
        }
        let url;
        try {
            url = new URL(value);
        }
        catch {
            throw new IdentityAccessClientError("configuration");
        }
        const loopback = ["localhost", "127.0.0.1", "[::1]"].includes(url.hostname);
        if (url.username ||
            url.password ||
            url.search ||
            url.hash ||
            !(url.protocol === "https:" || (url.protocol === "http:" && loopback))) {
            throw new IdentityAccessClientError("configuration");
        }
        if (!url.pathname.endsWith("/")) {
            url.pathname += "/";
        }
        return url;
    }
    static statusError(status) {
        if (status === 401)
            return new IdentityAccessClientError("unauthenticated", status);
        if (status === 403)
            return new IdentityAccessClientError("forbidden", status);
        if (status === 503)
            return new IdentityAccessClientError("unavailable", status);
        return new IdentityAccessClientError("http", status);
    }
}
