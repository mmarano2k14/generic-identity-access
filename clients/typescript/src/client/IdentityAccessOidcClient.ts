import type {
  IdentityOidcAuthorizationCode,
  IdentityOidcAuthorizationOptions,
  IdentityOidcTokenSet,
  IdentitySessionCredential,
} from "../contracts.js";
import { IdentityAccessClientError } from "../errors.js";
import { IdentityAccessCrypto } from "./IdentityAccessCrypto.js";
import { IdentityAccessHttpTransport } from "./IdentityAccessHttpTransport.js";
import { IdentityAccessPathBuilder } from "./IdentityAccessPathBuilder.js";
import { IdentityAccessProtocolCodec } from "./IdentityAccessProtocolCodec.js";
import { IdentityAccessValueCodec } from "./IdentityAccessValueCodec.js";

/** OIDC Authorization Code + PKCE and refresh-token rotation. */
export class IdentityAccessOidcClient {
  readonly #transport: IdentityAccessHttpTransport;

  public constructor(transport: IdentityAccessHttpTransport) {
    this.#transport = transport;
  }

  public async authorize(
    credential: IdentitySessionCredential,
    options: IdentityOidcAuthorizationOptions,
    signal?: AbortSignal,
  ): Promise<IdentityOidcAuthorizationCode> {
    const clientId = IdentityAccessValueCodec.clientId(options.clientId);
    if (IdentityAccessValueCodec.clientId(credential.clientId) !== clientId) {
      throw new IdentityAccessClientError("configuration");
    }

    const redirectUri = IdentityAccessValueCodec.redirectUri(options.redirectUri);
    const state = options.state === undefined
      ? IdentityAccessCrypto.randomOpaque(32)
      : IdentityAccessValueCodec.opaqueState(options.state);
    const nonce = options.nonce === undefined
      ? IdentityAccessCrypto.randomOpaque(32)
      : IdentityAccessValueCodec.nonce(options.nonce);
    const codeVerifier = IdentityAccessCrypto.randomOpaque(32);
    const codeChallenge = await IdentityAccessCrypto.computeS256Challenge(codeVerifier);

    const query = new URLSearchParams({
      client_id: clientId,
      redirect_uri: redirectUri,
      response_type: "code",
      scope: "openid",
      state,
      nonce,
      code_challenge: codeChallenge,
      code_challenge_method: "S256",
    });

    const response = await this.#transport.perform(
      `connect/authorize?${query.toString()}`,
      {
        method: "GET",
        acceptedStatuses: [302],
        headers: IdentityAccessPathBuilder.oidcSessionHeaders(credential),
        redirect: "manual",
        ...(signal === undefined ? {} : { signal }),
      },
      async (result) => result,
    );

    const location = response.headers.get("location");
    if (location === null || location.trim().length === 0) {
      throw new IdentityAccessClientError("protocol");
    }

    const redirect = IdentityAccessProtocolCodec.parseAuthorizationRedirect(location, redirectUri, state);
    return { clientId, redirectUri, code: redirect.code, state, nonce, codeVerifier };
  }

  public async exchangeAuthorizationCode(
    authorization: IdentityOidcAuthorizationCode,
    signal?: AbortSignal,
  ): Promise<IdentityOidcTokenSet> {
    const result = await this.#tokenRequest(
      new URLSearchParams({
        client_id: IdentityAccessValueCodec.clientId(authorization.clientId),
        grant_type: "authorization_code",
        code: IdentityAccessValueCodec.opaqueToken43(authorization.code),
        redirect_uri: IdentityAccessValueCodec.redirectUri(authorization.redirectUri),
        code_verifier: IdentityAccessValueCodec.pkceVerifier(authorization.codeVerifier),
      }),
      signal,
    );

    if (result.idToken === undefined) {
      throw new IdentityAccessClientError("protocol");
    }
    return result;
  }

  public async refreshTokens(
    clientIdValue: string,
    refreshTokenValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityOidcTokenSet> {
    const result = await this.#tokenRequest(
      new URLSearchParams({
        client_id: IdentityAccessValueCodec.clientId(clientIdValue),
        grant_type: "refresh_token",
        refresh_token: IdentityAccessValueCodec.opaqueToken43(refreshTokenValue),
      }),
      signal,
    );

    if (result.idToken !== undefined) {
      throw new IdentityAccessClientError("protocol");
    }
    return result;
  }

  async #tokenRequest(form: URLSearchParams, signal?: AbortSignal): Promise<IdentityOidcTokenSet> {
    return this.#transport.requestJson(
      "connect/token",
      {
        method: "POST",
        acceptedStatuses: [200, 400, 503],
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: form.toString(),
        ...(signal === undefined ? {} : { signal }),
      },
      (value, status) => IdentityAccessProtocolCodec.tokenSet(value, status),
    );
  }
}
