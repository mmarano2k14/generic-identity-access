import type { IdentityCapabilityRequirement } from "./contracts.js";
/** Internal class-backed metadata store used by the TypeScript decorator surface. */
export declare class CapabilityMetadataRegistry {
    #private;
    static register(method: Function, requirement: IdentityCapabilityRequirement): void;
    static requirementFor(target: object, methodName: string): IdentityCapabilityRequirement;
}
/**
 * Declarative TypeScript equivalent of `[RequireCapability(resource, feature, action)]`.
 * This decorator stores metadata only; it never evaluates permissions locally.
 */
export declare function RequireCapability(resource: string, feature: string, action: string): <This, Args extends unknown[], Result>(method: (this: This, ...args: Args) => Result, _context: ClassMethodDecoratorContext<This, (this: This, ...args: Args) => Result>) => (this: This, ...args: Args) => Result;
