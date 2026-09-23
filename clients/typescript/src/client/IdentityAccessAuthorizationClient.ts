import type {
  AuthorizationEvaluationResponse,
  IdentityAccessCredential,
  IdentityAuthorizationBoundary,
  IdentityCapabilityRequirement,
} from "../contracts.js";
import { IdentityAccessHttpTransport } from "./IdentityAccessHttpTransport.js";
import { IdentityAccessPathBuilder } from "./IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "./IdentityAccessValueCodec.js";

/** Capability evaluation against the server-side .NET/RBAC authorization boundary. */
export class IdentityAccessAuthorizationClient {
  readonly #transport: IdentityAccessHttpTransport;

  public constructor(transport: IdentityAccessHttpTransport) {
    this.#transport = transport;
  }

  public validateContext(boundary: IdentityAuthorizationBoundary, credential: IdentityAccessCredential): void {
    IdentityAccessPathBuilder.authorizationPath(boundary);
    IdentityAccessPathBuilder.credentialHeaders(credential);
  }

  public async evaluate(
    boundary: IdentityAuthorizationBoundary,
    requirement: IdentityCapabilityRequirement,
    credential: IdentityAccessCredential,
    signal?: AbortSignal,
  ): Promise<boolean> {
    const response = await this.#transport.requestJson<AuthorizationEvaluationResponse>(
      IdentityAccessPathBuilder.authorizationPath(boundary),
      {
        method: "POST",
        acceptedStatuses: [200],
        headers: {
          ...IdentityAccessPathBuilder.credentialHeaders(credential),
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          resource: IdentityAccessValueCodec.slug(requirement.resource),
          feature: IdentityAccessValueCodec.slug(requirement.feature),
          action: IdentityAccessValueCodec.slug(requirement.action),
        }),
        ...(signal === undefined ? {} : { signal }),
      },
      (value) => ({ allowed: IdentityAccessValueCodec.flag(IdentityAccessValueCodec.object(value).allowed) }),
    );

    return response.allowed;
  }
}
