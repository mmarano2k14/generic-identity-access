import { IdentityAccessClient } from "./client.js";
import type {
  IdentityAccessCredential,
  IdentityAuthorizationBoundary,
  IdentityCapabilityRequirement,
} from "./contracts.js";
import { IdentityAccessClientError } from "./errors.js";
import { CapabilityMetadataRegistry } from "./require-capability.js";

export interface IdentityAuthorizationContextOptions extends IdentityAuthorizationBoundary {
  readonly credential: IdentityAccessCredential;
}

/**
 * Request/session-scoped authorization context mirroring the .NET `_auth.IsAllowed(...)` usage.
 * It delegates every decision to the server-side .NET authorization/RBAC boundary.
 */
export class IdentityAuthorizationContext {
  readonly #client: IdentityAccessClient;
  readonly #boundary: IdentityAuthorizationBoundary;
  readonly #credential: IdentityAccessCredential;

  public constructor(client: IdentityAccessClient, options: IdentityAuthorizationContextOptions) {
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
    IdentityAccessClient.authorizationPath(this.#boundary);
    IdentityAccessClient.credentialHeaders(this.#credential);
  }

  public async isAllowed(
    resource: string,
    feature: string,
    action: string,
    signal?: AbortSignal,
  ): Promise<boolean> {
    return this.#client.evaluateCapability(
      this.#boundary,
      { resource, feature, action },
      this.#credential,
      signal,
    );
  }

  public async isAllowedRequirement(
    requirement: IdentityCapabilityRequirement,
    signal?: AbortSignal,
  ): Promise<boolean> {
    return this.#client.evaluateCapability(
      this.#boundary,
      requirement,
      this.#credential,
      signal,
    );
  }

  /** Evaluates capability metadata declared by `@RequireCapability(...)` on one method. */
  public async isAllowedFor(
    target: object,
    methodName: string,
    signal?: AbortSignal,
  ): Promise<boolean> {
    const requirement = CapabilityMetadataRegistry.requirementFor(target, methodName);
    return this.isAllowedRequirement(requirement, signal);
  }
}
