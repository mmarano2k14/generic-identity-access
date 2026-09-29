import { IdentityAccessClientError } from "./errors.js";
/** Internal class-backed metadata store used by the TypeScript decorator surface. */
export class CapabilityMetadataRegistry {
    static #requirements = new WeakMap();
    static register(method, requirement) {
        CapabilityMetadataRegistry.#requirements.set(method, Object.freeze({ ...requirement }));
    }
    static requirementFor(target, methodName) {
        const candidate = Reflect.get(target, methodName);
        if (typeof candidate !== "function") {
            throw new IdentityAccessClientError("configuration");
        }
        const requirement = CapabilityMetadataRegistry.#requirements.get(candidate);
        if (requirement === undefined) {
            throw new IdentityAccessClientError("configuration");
        }
        return requirement;
    }
}
/**
 * Declarative TypeScript equivalent of `[RequireCapability(resource, feature, action)]`.
 * This decorator stores metadata only; it never evaluates permissions locally.
 */
export function RequireCapability(resource, feature, action) {
    const requirement = Object.freeze({ resource, feature, action });
    return function (method, _context) {
        CapabilityMetadataRegistry.register(method, requirement);
        return method;
    };
}
