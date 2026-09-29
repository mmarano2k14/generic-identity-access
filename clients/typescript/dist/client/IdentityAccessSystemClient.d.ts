import type { LivenessResponse, ReadinessResponse, ServiceInfoResponse } from "../contracts.js";
import { IdentityAccessHttpTransport } from "./IdentityAccessHttpTransport.js";
/** System-health and service-descriptor operations. */
export declare class IdentityAccessSystemClient {
    #private;
    constructor(transport: IdentityAccessHttpTransport);
    liveness(signal?: AbortSignal): Promise<LivenessResponse>;
    readiness(signal?: AbortSignal): Promise<ReadinessResponse>;
    info(signal?: AbortSignal): Promise<ServiceInfoResponse>;
}
