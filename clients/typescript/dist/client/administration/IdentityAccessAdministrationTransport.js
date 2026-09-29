import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
/** Shared protected administration HTTP operations used by focused administration clients. */
export class IdentityAccessAdministrationTransport {
    #transport;
    constructor(transport) {
        this.#transport = transport;
    }
    async get(path, context, decode, signal) {
        return this.#transport.requestJson(path, this.requestOptions("GET", context, signal), decode);
    }
    async getNullable(path, context, decode, signal) {
        return this.#transport.requestNullableJson(path, this.requestOptions("GET", context, signal), decode);
    }
    async post(path, context, body, decode, signal) {
        return this.#transport.requestJson(path, this.jsonOptions("POST", context, body, signal, [201]), decode);
    }
    async postAction(path, context, decode, signal) {
        return this.#transport.requestJson(path, {
            method: "POST",
            acceptedStatuses: [200],
            headers: IdentityAccessPathBuilder.credentialHeaders(context.credential),
            ...(signal === undefined ? {} : { signal }),
        }, decode);
    }
    /** Executes a POST action that accepts a JSON request body and returns HTTP 200 JSON. */
    async postJsonAction(path, context, body, decode, signal) {
        return this.#transport.requestJson(path, this.jsonOptions("POST", context, body, signal, [200]), decode);
    }
    async put(path, context, body, decode, signal) {
        return this.#transport.requestJson(path, this.jsonOptions("PUT", context, body, signal, [200]), decode);
    }
    async delete(path, context, signal) {
        return this.#transport.requestNoContent(path, this.requestOptions("DELETE", context, signal));
    }
    async deleteJson(path, context, decode, signal) {
        return this.#transport.requestJson(path, this.requestOptions("DELETE", context, signal), decode);
    }
    requestOptions(method, context, signal) {
        return {
            method,
            acceptedStatuses: [200],
            headers: IdentityAccessPathBuilder.credentialHeaders(context.credential),
            ...(signal === undefined ? {} : { signal }),
        };
    }
    jsonOptions(method, context, body, signal, acceptedStatuses) {
        return {
            method,
            acceptedStatuses,
            headers: {
                ...IdentityAccessPathBuilder.credentialHeaders(context.credential),
                "Content-Type": "application/json",
            },
            body: JSON.stringify(body),
            ...(signal === undefined ? {} : { signal }),
        };
    }
}
