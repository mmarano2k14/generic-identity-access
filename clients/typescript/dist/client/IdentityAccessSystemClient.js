import { IdentityAccessClientError } from "../errors.js";
import { IdentityAccessProtocolCodec } from "./IdentityAccessProtocolCodec.js";
import { IdentityAccessValueCodec } from "./IdentityAccessValueCodec.js";
/** System-health and service-descriptor operations. */
export class IdentityAccessSystemClient {
    #transport;
    constructor(transport) {
        this.#transport = transport;
    }
    async liveness(signal) {
        return this.#transport.requestJson("health/live", { method: "GET", acceptedStatuses: [200], ...(signal === undefined ? {} : { signal }) }, (value) => {
            if (IdentityAccessValueCodec.object(value).status !== "alive") {
                throw new IdentityAccessClientError("protocol");
            }
            return { status: "alive" };
        });
    }
    async readiness(signal) {
        return this.#transport.requestJson("health/ready", { method: "GET", acceptedStatuses: [200, 503], ...(signal === undefined ? {} : { signal }) }, (value, status) => {
            const data = IdentityAccessValueCodec.object(value);
            const ready = IdentityAccessValueCodec.flag(data.ready);
            if (!Array.isArray(data.blockingCapabilities) || ready !== (status === 200)) {
                throw new IdentityAccessClientError("protocol");
            }
            return {
                ready,
                stage: IdentityAccessValueCodec.text(data.stage),
                blockingCapabilities: data.blockingCapabilities.map(IdentityAccessValueCodec.text),
            };
        });
    }
    async info(signal) {
        return this.#transport.requestJson("api/v1/system/info", { method: "GET", acceptedStatuses: [200], ...(signal === undefined ? {} : { signal }) }, (value) => IdentityAccessProtocolCodec.serviceInfo(value));
    }
}
