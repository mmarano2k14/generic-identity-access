import type { IdentityCapabilityRequirement } from "./contracts.js";
import { IdentityAccessClientError } from "./errors.js";

/** Internal class-backed metadata store used by the TypeScript decorator surface. */
export class CapabilityMetadataRegistry {
  static readonly #requirements = new WeakMap<Function, IdentityCapabilityRequirement>();

  public static register(method: Function, requirement: IdentityCapabilityRequirement): void {
    CapabilityMetadataRegistry.#requirements.set(method, Object.freeze({ ...requirement }));
  }

  public static requirementFor(target: object, methodName: string): IdentityCapabilityRequirement {
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
export function RequireCapability(
  resource: string,
  feature: string,
  action: string,
) {
  const requirement: IdentityCapabilityRequirement = Object.freeze({ resource, feature, action });

  return function <This, Args extends unknown[], Result>(
    method: (this: This, ...args: Args) => Result,
    _context: ClassMethodDecoratorContext<This, (this: This, ...args: Args) => Result>,
  ): (this: This, ...args: Args) => Result {
    CapabilityMetadataRegistry.register(method, requirement);
    return method;
  };
}
