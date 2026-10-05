import type { IdentityAccessCredential } from "@identity-access/client";
import type {
  IdentityAuthorizationBoundary,
  IdentityCapabilityRequirement,
} from "@generic-identity/contracts";

/** Secret-bearing authorization credentials stay inside the auth package. */
export type {
  IdentityAccessCredential,
  IdentityBearerCredential,
  IdentitySessionCredential,
} from "@identity-access/client";

/** Framework-neutral server-backed authorization surface. */
export interface GenericIdentityAuthorizationClient {
  validateContext(
    boundary: IdentityAuthorizationBoundary,
    credential: IdentityAccessCredential,
  ): void;

  evaluate(
    boundary: IdentityAuthorizationBoundary,
    requirement: IdentityCapabilityRequirement,
    credential: IdentityAccessCredential,
    signal?: AbortSignal,
  ): Promise<boolean>;
}

type AuthorizationClientOwner = Readonly<{
  authorization: GenericIdentityAuthorizationClient;
}>;

/**
 * Evaluates one capability through the existing server-side .NET/RBAC boundary.
 * No permission decision is calculated locally.
 */
export function isAllowed(
  client: AuthorizationClientOwner,
  boundary: IdentityAuthorizationBoundary,
  requirement: IdentityCapabilityRequirement,
  credential: IdentityAccessCredential,
  signal?: AbortSignal,
): Promise<boolean> {
  return client.authorization.evaluate(boundary, requirement, credential, signal);
}
