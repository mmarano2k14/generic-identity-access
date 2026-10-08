import type { IdentityAccessCredential, IdentityAuthorizationBoundary, IdentityCapabilityRequirement } from "./contracts.js";
import type { IdentityAccessClient } from "./client.js";
export interface IdentityAuthorizationContextOptions extends IdentityAuthorizationBoundary {
    readonly credential: IdentityAccessCredential;
}
/**
 * Request/session-scoped authorization context mirroring
 * the .NET `_auth.IsAllowed(...)` usage.
 *
 * It delegates every decision to the authoritative server-side
 * .NET/RBAC authorization boundary.
 *
 * Runtime validation intentionally checks the required authorization
 * capability surface instead of relying on `instanceof IdentityAccessClient`.
 *
 * `instanceof` is not reliable across package/bundler module boundaries:
 * two runtime copies of the same constructor can represent the same client
 * implementation while failing nominal identity comparison.
 */
export declare class IdentityAuthorizationContext {
    #private;
    constructor(client: IdentityAccessClient, options: IdentityAuthorizationContextOptions);
    isAllowed(resource: string, feature: string, action: string, signal?: AbortSignal): Promise<boolean>;
    isAllowedRequirement(requirement: IdentityCapabilityRequirement, signal?: AbortSignal): Promise<boolean>;
    /**
     * Evaluates capability metadata declared by
     * `@RequireCapability(...)` on one method.
     */
    isAllowedFor(target: object, methodName: string, signal?: AbortSignal): Promise<boolean>;
}
