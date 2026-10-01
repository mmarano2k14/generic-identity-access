import {
  IdentityAuthorizationContext as LegacyIdentityAuthorizationContext,
  RequireCapability,
  type IdentityAuthorizationContextOptions as LegacyIdentityAuthorizationContextOptions,
} from "@identity-access/client";
import type { IdentityCapabilityRequirement } from "@generic-identity/contracts";
import type { GenericIdentityClient } from "./client";

/** Public authorization-context options preserve the proven legacy shape. */
export type GenericIdentityAuthorizationContextOptions = LegacyIdentityAuthorizationContextOptions;

/** Public behavior exposed by a request/session-scoped authorization context. */
export interface GenericIdentityAuthorizationContext {
  isAllowed(
    resource: string,
    feature: string,
    action: string,
    signal?: AbortSignal,
  ): Promise<boolean>;

  isAllowedRequirement(
    requirement: IdentityCapabilityRequirement,
    signal?: AbortSignal,
  ): Promise<boolean>;

  isAllowedFor(
    target: object,
    methodName: string,
    signal?: AbortSignal,
  ): Promise<boolean>;
}

/**
 * Creates the proven authorization context over a client produced by
 * createIdentityClient(). The cast is internal to this extraction bridge; the
 * runtime instance is still the original IdentityAccessClient.
 */
export function createAuthorizationContext(
  client: GenericIdentityClient,
  options: GenericIdentityAuthorizationContextOptions,
): GenericIdentityAuthorizationContext {
  return new LegacyIdentityAuthorizationContext(
    client as unknown as ConstructorParameters<typeof LegacyIdentityAuthorizationContext>[0],
    options,
  );
}

/**
 * Framework-neutral declarative capability metadata. It stores metadata only;
 * server-side authorization remains authoritative.
 */
export { RequireCapability };
