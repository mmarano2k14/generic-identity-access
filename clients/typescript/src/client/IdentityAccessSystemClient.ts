import type { LivenessResponse, ReadinessResponse, ServiceInfoResponse } from "../contracts.js";
import { IdentityAccessClientError } from "../errors.js";
import { IdentityAccessHttpTransport } from "./IdentityAccessHttpTransport.js";
import { IdentityAccessProtocolCodec } from "./IdentityAccessProtocolCodec.js";
import { IdentityAccessValueCodec } from "./IdentityAccessValueCodec.js";

/** System-health and service-descriptor operations. */
export class IdentityAccessSystemClient {
  readonly #transport: IdentityAccessHttpTransport;

  public constructor(transport: IdentityAccessHttpTransport) {
    this.#transport = transport;
  }

  public async liveness(signal?: AbortSignal): Promise<LivenessResponse> {
    return this.#transport.requestJson(
      "health/live",
      { method: "GET", acceptedStatuses: [200], ...(signal === undefined ? {} : { signal }) },
      (value) => {
        if (IdentityAccessValueCodec.object(value).status !== "alive") {
          throw new IdentityAccessClientError("protocol");
        }
        return { status: "alive" };
      },
    );
  }

  public async readiness(signal?: AbortSignal): Promise<ReadinessResponse> {
    return this.#transport.requestJson(
      "health/ready",
      { method: "GET", acceptedStatuses: [200, 503], ...(signal === undefined ? {} : { signal }) },
      (value, status) => {
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
      },
    );
  }

  public async info(signal?: AbortSignal): Promise<ServiceInfoResponse> {
    return this.#transport.requestJson(
      "api/v1/system/info",
      { method: "GET", acceptedStatuses: [200], ...(signal === undefined ? {} : { signal }) },
      (value) => IdentityAccessProtocolCodec.serviceInfo(value),
    );
  }
}
