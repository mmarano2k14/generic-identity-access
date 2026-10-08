import { IdentityAccessClientError } from "./errors.js";
import { CapabilityMetadataRegistry } from "./require-capability.js";
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
export class IdentityAuthorizationContext {
    #authorization;
    #boundary;
    #credential;
    constructor(client, options) {
        const authorization = client?.authorization;
        if (authorization === undefined ||
            authorization === null ||
            typeof authorization.validateContext !== "function" ||
            typeof authorization.evaluate !== "function") {
            throw new IdentityAccessClientError("configuration");
        }
        this.#authorization =
            client.authorization;
        this.#boundary = {
            identityScopeId: options.identityScopeId,
            applicationKey: options.applicationKey,
            ...(options.tenantId === undefined
                ? {}
                : { tenantId: options.tenantId }),
            ...(options.resourceScopeId === undefined
                ? {}
                : { resourceScopeId: options.resourceScopeId }),
        };
        this.#credential = options.credential;
        // Validate all passive boundary/credential values immediately
        // at construction.
        this.#authorization.validateContext(this.#boundary, this.#credential);
    }
    async isAllowed(resource, feature, action, signal) {
        return this.#authorization.evaluate(this.#boundary, {
            resource,
            feature,
            action,
        }, this.#credential, signal);
    }
    async isAllowedRequirement(requirement, signal) {
        return this.#authorization.evaluate(this.#boundary, requirement, this.#credential, signal);
    }
    /**
     * Evaluates capability metadata declared by
     * `@RequireCapability(...)` on one method.
     */
    async isAllowedFor(target, methodName, signal) {
        const requirement = CapabilityMetadataRegistry.requirementFor(target, methodName);
        return this.isAllowedRequirement(requirement, signal);
    }
}
