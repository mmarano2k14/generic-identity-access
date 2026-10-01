import { IdentityAccessClient as LegacyIdentityAccessClient } from "@identity-access/client";
import type {
  FetchTransport as LegacyFetchTransport,
  IdentityAccessClientOptions as LegacyIdentityAccessClientOptions,
} from "@identity-access/client";
import type { GenericIdentityAuthenticationClient } from "./authentication";
import type { GenericIdentityAdministrationClient } from "./administration";
import type { GenericIdentityAuthorizationClient } from "./authorization";

/** Trusted client configuration for the shared Generic Identity boundary. */
export type GenericIdentityClientOptions = LegacyIdentityAccessClientOptions;

/** Optional injected transport used by tests or controlled hosts. */
export type GenericIdentityFetchTransport = LegacyFetchTransport;

/**
 * Narrow public client surface for the shared authentication/authorization SDK.
 *
 * The proven legacy client remains the runtime implementation during this
 * additive extraction pack. Administration, system and OIDC surfaces are not
 * advertised through this package.
 */
export interface GenericIdentityClient {
  readonly authentication: GenericIdentityAuthenticationClient;
  readonly authorization: GenericIdentityAuthorizationClient;
  readonly administration: GenericIdentityAdministrationClient;
}

/**
 * Creates the shared Generic Identity client without introducing a second
 * authentication or authorization implementation.
 */
export function createIdentityClient(options: GenericIdentityClientOptions): GenericIdentityClient {
  return new LegacyIdentityAccessClient(options);
}
