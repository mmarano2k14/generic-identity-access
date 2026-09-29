import type { IdentityAccessCredential, IdentityAuthorizationBoundary, IdentityCapabilityRequirement } from "../contracts.js";
import { IdentityAccessHttpTransport } from "./IdentityAccessHttpTransport.js";
/** Capability evaluation against the server-side .NET/RBAC authorization boundary. */
export declare class IdentityAccessAuthorizationClient {
    #private;
    constructor(transport: IdentityAccessHttpTransport);
    validateContext(boundary: IdentityAuthorizationBoundary, credential: IdentityAccessCredential): void;
    evaluate(boundary: IdentityAuthorizationBoundary, requirement: IdentityCapabilityRequirement, credential: IdentityAccessCredential, signal?: AbortSignal): Promise<boolean>;
}
