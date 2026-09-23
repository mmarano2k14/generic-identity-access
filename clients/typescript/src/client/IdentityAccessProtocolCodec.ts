import type { IdentityOidcTokenSet, ServiceInfoResponse } from "../contracts.js";
import { IdentityAccessClientError } from "../errors.js";
import { IdentityAccessValueCodec } from "./IdentityAccessValueCodec.js";

/** Decodes protocol-level responses and OIDC redirects/token errors. */
export class IdentityAccessProtocolCodec {
  public static serviceInfo(value: unknown): ServiceInfoResponse {
    const data = IdentityAccessValueCodec.object(value);
    if (data.service !== "identity-access" || data.apiVersion !== "v1" || data.storageProvider !== "postgresql") {
      throw new IdentityAccessClientError("protocol");
    }

    return {
      service: "identity-access",
      apiVersion: "v1",
      storageProvider: "postgresql",
      moduleVersion: IdentityAccessValueCodec.text(data.moduleVersion),
      stage: IdentityAccessValueCodec.text(data.stage),
      databaseRoutingConfigured: IdentityAccessValueCodec.flag(data.databaseRoutingConfigured),
      storageConfigured: IdentityAccessValueCodec.flag(data.storageConfigured),
      authenticationConfigured: IdentityAccessValueCodec.flag(data.authenticationConfigured),
      authorizationConfigured: IdentityAccessValueCodec.flag(data.authorizationConfigured),
    };
  }

  public static parseAuthorizationRedirect(
    location: string,
    expectedRedirectUri: string,
    expectedState: string,
  ): { readonly code: string } {
    let actual: URL;
    let expected: URL;
    try {
      actual = new URL(location);
      expected = new URL(expectedRedirectUri);
    } catch {
      throw new IdentityAccessClientError("protocol");
    }

    const protocolParameters = ["code", "state", "error"];
    if (protocolParameters.some((name) => expected.searchParams.has(name))) {
      throw new IdentityAccessClientError("configuration");
    }

    const base = new URL(actual.toString());
    for (const parameter of protocolParameters) {
      base.searchParams.delete(parameter);
    }
    if (base.toString() !== expected.toString()) {
      throw new IdentityAccessClientError("protocol");
    }

    const stateValues = actual.searchParams.getAll("state");
    if (stateValues.length !== 1 || stateValues[0] !== expectedState) {
      throw new IdentityAccessClientError("protocol");
    }

    const errorValues = actual.searchParams.getAll("error");
    const codeValues = actual.searchParams.getAll("code");
    if (errorValues.length === 1 && codeValues.length === 0) {
      throw new IdentityAccessClientError("oidc", 302, IdentityAccessProtocolCodec.oidcProtocolCode(errorValues[0] ?? ""));
    }
    if (errorValues.length !== 0 || codeValues.length !== 1) {
      throw new IdentityAccessClientError("protocol");
    }

    return { code: IdentityAccessValueCodec.opaqueToken43(codeValues[0] ?? "") };
  }

  public static tokenSet(value: unknown, status: number): IdentityOidcTokenSet {
    if (status !== 200) {
      const error = IdentityAccessProtocolCodec.oidcError(value);
      if (status === 503 || error === "temporarily_unavailable") {
        throw new IdentityAccessClientError("unavailable", status, error);
      }
      throw new IdentityAccessClientError("oidc", status, error);
    }

    const data = IdentityAccessValueCodec.object(value);
    if (data.token_type !== "Bearer" || data.scope !== "openid") {
      throw new IdentityAccessClientError("protocol");
    }

    const idToken = data.id_token === undefined
      ? undefined
      : IdentityAccessValueCodec.token(IdentityAccessValueCodec.text(data.id_token));

    return {
      accessToken: IdentityAccessValueCodec.token(IdentityAccessValueCodec.text(data.access_token)),
      tokenType: "Bearer",
      expiresIn: IdentityAccessValueCodec.positiveInteger(data.expires_in),
      ...(idToken === undefined ? {} : { idToken }),
      refreshToken: IdentityAccessValueCodec.opaqueToken43(IdentityAccessValueCodec.text(data.refresh_token)),
      scope: "openid",
    };
  }

  private static oidcError(value: unknown): string {
    const data = IdentityAccessValueCodec.object(value);
    return IdentityAccessProtocolCodec.oidcProtocolCode(IdentityAccessValueCodec.text(data.error));
  }

  private static oidcProtocolCode(value: string): string {
    const code = IdentityAccessValueCodec.nonEmpty(value);
    const allowed = new Set([
      "invalid_request",
      "invalid_client",
      "invalid_grant",
      "unsupported_grant_type",
      "unsupported_response_type",
      "invalid_scope",
      "login_required",
      "temporarily_unavailable",
    ]);
    if (!allowed.has(code)) {
      throw new IdentityAccessClientError("protocol");
    }
    return code;
  }
}
