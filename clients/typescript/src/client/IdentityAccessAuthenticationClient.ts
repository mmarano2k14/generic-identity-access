import type {
  IdentityLocalSession,
  IdentityLogoutResult,
  IdentityPasswordLoginRequest,
  IdentitySessionCredential,
  IdentitySessionValidationResult,
} from "../contracts.js";
import { IdentityAccessClientError } from "../errors.js";
import { IdentityAccessHttpTransport } from "./IdentityAccessHttpTransport.js";
import { IdentityAccessValueCodec } from "./IdentityAccessValueCodec.js";

/** Local password/session authentication lifecycle. */
export class IdentityAccessAuthenticationClient {
  readonly #transport: IdentityAccessHttpTransport;

  public constructor(transport: IdentityAccessHttpTransport) {
    this.#transport = transport;
  }

  public async passwordLogin(
    request: IdentityPasswordLoginRequest,
    signal?: AbortSignal,
  ): Promise<IdentityLocalSession> {
    const clientId = IdentityAccessValueCodec.clientId(request.clientId);
    const redirectUri = IdentityAccessValueCodec.redirectUri(request.redirectUri);
    const loginIdentifier = IdentityAccessValueCodec.nonEmpty(request.loginIdentifier);
    const password = IdentityAccessValueCodec.nonEmptySecret(request.password);

    return this.#transport.requestJson(
      `api/v1/authentication/clients/${encodeURIComponent(clientId)}/password-login`,
      {
        method: "POST",
        acceptedStatuses: [200],
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ loginIdentifier, password, redirectUri }),
        ...(signal === undefined ? {} : { signal }),
      },
      (value) => {
        const data = IdentityAccessValueCodec.object(value);
        const returnedRedirectUri = IdentityAccessValueCodec.redirectUri(IdentityAccessValueCodec.text(data.redirectUri));
        if (returnedRedirectUri !== redirectUri) {
          throw new IdentityAccessClientError("protocol");
        }

        return {
          kind: "session",
          clientId,
          userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
          sessionId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.sessionId)),
          sessionToken: IdentityAccessValueCodec.token(IdentityAccessValueCodec.text(data.sessionToken)),
          expiresAt: IdentityAccessValueCodec.timestamp(data.expiresAt),
          redirectUri: returnedRedirectUri,
        };
      },
    );
  }

  public async validateSession(
    credential: IdentitySessionCredential,
    signal?: AbortSignal,
  ): Promise<IdentitySessionValidationResult> {
    const clientId = IdentityAccessValueCodec.clientId(credential.clientId);
    const sessionId = IdentityAccessValueCodec.uuid(credential.sessionId);
    const sessionToken = IdentityAccessValueCodec.token(credential.sessionToken);

    return this.#transport.requestJson(
      `api/v1/authentication/clients/${encodeURIComponent(clientId)}/sessions/validate`,
      {
        method: "POST",
        acceptedStatuses: [200],
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ sessionId, sessionToken }),
        ...(signal === undefined ? {} : { signal }),
      },
      (value) => {
        const data = IdentityAccessValueCodec.object(value);
        const returnedSessionId = IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.sessionId));
        if (returnedSessionId !== sessionId) {
          throw new IdentityAccessClientError("protocol");
        }
        return {
          userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
          sessionId: returnedSessionId,
          expiresAt: IdentityAccessValueCodec.timestamp(data.expiresAt),
        };
      },
    );
  }

  public async logout(
    credential: IdentitySessionCredential,
    postLogoutRedirectUri?: string,
    signal?: AbortSignal,
  ): Promise<IdentityLogoutResult> {
    const clientId = IdentityAccessValueCodec.clientId(credential.clientId);
    const sessionId = IdentityAccessValueCodec.uuid(credential.sessionId);
    const sessionToken = IdentityAccessValueCodec.token(credential.sessionToken);
    const redirect = postLogoutRedirectUri === undefined
      ? undefined
      : IdentityAccessValueCodec.redirectUri(postLogoutRedirectUri);

    return this.#transport.requestJson(
      `api/v1/authentication/clients/${encodeURIComponent(clientId)}/logout`,
      {
        method: "POST",
        acceptedStatuses: [200],
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          sessionId,
          sessionToken,
          ...(redirect === undefined ? {} : { postLogoutRedirectUri: redirect }),
        }),
        ...(signal === undefined ? {} : { signal }),
      },
      (value) => {
        const data = IdentityAccessValueCodec.object(value);
        if (data.postLogoutRedirectUri === null || data.postLogoutRedirectUri === undefined) {
          return {};
        }
        const returned = IdentityAccessValueCodec.redirectUri(IdentityAccessValueCodec.text(data.postLogoutRedirectUri));
        if (redirect === undefined || returned !== redirect) {
          throw new IdentityAccessClientError("protocol");
        }
        return { postLogoutRedirectUri: returned };
      },
    );
  }
}
