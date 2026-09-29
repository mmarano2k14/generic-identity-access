import { IdentityAccessClient } from "./client.js";
import { IdentityAccessClientError } from "./errors.js";
import { CapabilityMetadataRegistry } from "./require-capability.js";
/**
 * Request/session-scoped authorization context mirroring the .NET `_auth.IsAllowed(...)` usage.
 * It delegates every decision to the server-side .NET authorization/RBAC boundary.
 */
export class IdentityAuthorizationContext {
    #client;
    #boundary;
    #credential;
    constructor(client, options) {
        if (!(client instanceof IdentityAccessClient)) {
            throw new IdentityAccessClientError("configuration");
        }
        this.#client = client;
        this.#boundary = {
            identityScopeId: options.identityScopeId,
            applicationKey: options.applicationKey,
            ...(options.tenantId === undefined ? {} : { tenantId: options.tenantId }),
            ...(options.resourceScopeId === undefined ? {} : { resourceScopeId: options.resourceScopeId }),
        };
        this.#credential = options.credential;
        // Validate all passive boundary/credential values immediately at construction.
        this.#client.authorization.validateContext(this.#boundary, this.#credential);
    }
    async isAllowed(resource, feature, action, signal) {
        return this.#client.authorization.evaluate(this.#boundary, { resource, feature, action }, this.#credential, signal);
    }
    async isAllowedRequirement(requirement, signal) {
        return this.#client.authorization.evaluate(this.#boundary, requirement, this.#credential, signal);
    }
    /** Evaluates capability metadata declared by `@RequireCapability(...)` on one method. */
    async isAllowedFor(target, methodName, signal) {
        const requirement = CapabilityMetadataRegistry.requirementFor(target, methodName);
        return this.isAllowedRequirement(requirement, signal);
    }
}
