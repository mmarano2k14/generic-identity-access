import { IdentityAccessClient } from "./client.js";
import type { IdentityAccessCredential, IdentityAuthorizationBoundary, IdentityCapabilityRequirement } from "./contracts.js";
export interface IdentityAuthorizationContextOptions extends IdentityAuthorizationBoundary {
    readonly credential: IdentityAccessCredential;
}
/**
 * Request/session-scoped authorization context mirroring the .NET `_auth.IsAllowed(...)` usage.
 * It delegates every decision to the server-side .NET authorization/RBAC boundary.
 */
export declare class IdentityAuthorizationContext {
    #private;
    constructor(client: IdentityAccessClient, options: IdentityAuthorizationContextOptions);
    isAllowed(resource: string, feature: string, action: string, signal?: AbortSignal): Promise<boolean>;
    isAllowedRequirement(requirement: IdentityCapabilityRequirement, signal?: AbortSignal): Promise<boolean>;
    /** Evaluates capability metadata declared by `@RequireCapability(...)` on one method. */
    isAllowedFor(target: object, methodName: string, signal?: AbortSignal): Promise<boolean>;
}
