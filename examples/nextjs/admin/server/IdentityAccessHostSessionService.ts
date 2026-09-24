import "server-only";
import { cookies } from "next/headers";
import {
  IdentityAccessClientError,
  type IdentityBearerCredential,
  type IdentityLocalSession,
  type IdentityOidcTokenSet,
  type IdentitySessionCredential,
} from "@identity-access/client";
import { IdentityAccessServerConnector } from "./IdentityAccessServerConnector";

type CookieStore = Awaited<ReturnType<typeof cookies>>;

/**
 * Server-only session coordinator for the runnable Next.js administration host.
 * Passwords and raw protocol/session tokens remain request-local or HTTP-only cookie material.
 */
export class IdentityAccessHostSessionService {
  readonly #connector: IdentityAccessServerConnector;
  readonly #cookieStore: CookieStore;
  readonly #bearerCookieName: string;
  readonly #refreshCookieName: string;
  readonly #sessionIdCookieName: string;
  readonly #sessionTokenCookieName: string;
  readonly #clientId: string;
  readonly #redirectUri: string;

  private constructor(
    connector: IdentityAccessServerConnector,
    cookieStore: CookieStore,
    bearerCookieName: string,
    clientId: string,
    redirectUri: string,
  ) {
    this.#connector = connector;
    this.#cookieStore = cookieStore;
    this.#bearerCookieName = bearerCookieName;
    this.#refreshCookieName = `${bearerCookieName}_refresh`;
    this.#sessionIdCookieName = `${bearerCookieName}_session_id`;
    this.#sessionTokenCookieName = `${bearerCookieName}_session_token`;
    this.#clientId = clientId;
    this.#redirectUri = redirectUri;
  }

  public static async fromCurrentRequest(): Promise<IdentityAccessHostSessionService> {
    // Resolve request-bound state before reading runtime environment values so Next.js
    // excludes this host session path from build-time prerender evaluation.
    const cookieStore = await cookies();
    const bearerCookieName = IdentityAccessHostSessionService.cookieName(
      IdentityAccessHostSessionService.requiredEnvironment("IDENTITY_ACCESS_BEARER_COOKIE_NAME"),
    );
    const clientId = IdentityAccessHostSessionService.requiredEnvironment("IDENTITY_ACCESS_OIDC_CLIENT_ID");
    const redirectUri = IdentityAccessHostSessionService.requiredEnvironment("IDENTITY_ACCESS_OIDC_REDIRECT_URI");

    return new IdentityAccessHostSessionService(
      new IdentityAccessServerConnector(),
      cookieStore,
      bearerCookieName,
      clientId,
      redirectUri,
    );
  }

  public get connector(): IdentityAccessServerConnector {
    return this.#connector;
  }

  public hasBearerCredential(): boolean {
    return this.bearerCredential() !== undefined;
  }

  public bearerCredential(): IdentityBearerCredential | undefined {
    const accessToken = this.#cookieStore.get(this.#bearerCookieName)?.value;
    if (!accessToken) return undefined;
    return { kind: "bearer", accessToken };
  }

  public requireBearerCredential(): IdentityBearerCredential {
    const credential = this.bearerCredential();
    if (credential === undefined) throw new IdentityAccessClientError("unauthenticated", 401);
    return credential;
  }

  public async signIn(loginIdentifierValue: string, passwordValue: string): Promise<void> {
    const loginIdentifier = IdentityAccessHostSessionService.requiredInput(loginIdentifierValue, "login identifier", 320);
    const password = IdentityAccessHostSessionService.requiredSecret(passwordValue, "password", 4096);

    const session = await this.#connector.client.authentication.passwordLogin({
      clientId: this.#clientId,
      loginIdentifier,
      password,
      redirectUri: this.#redirectUri,
    });

    try {
      const authorization = await this.#connector.client.oidc.authorize(session, {
        clientId: this.#clientId,
        redirectUri: this.#redirectUri,
      });
      const tokens = await this.#connector.client.oidc.exchangeAuthorizationCode(authorization);
      this.#persist(session, tokens);
    } catch (error) {
      await this.#bestEffortRevoke(session);
      throw error;
    }
  }

  public async recoverPassword(loginIdentifierValue: string, recoveryCodeValue: string, newPasswordValue: string): Promise<void> {
    const loginIdentifier = IdentityAccessHostSessionService.requiredInput(loginIdentifierValue, "login identifier", 320);
    const recoveryCode = IdentityAccessHostSessionService.requiredSecret(recoveryCodeValue, "recovery code", 128);
    const newPassword = IdentityAccessHostSessionService.requiredSecret(newPasswordValue, "new password", 256);
    if (newPassword.length < 12) throw new Error("Invalid recovery input: new password must contain at least 12 characters.");

    await this.#connector.client.authentication.recoverPasswordWithCode({
      clientId: this.#clientId,
      loginIdentifier,
      recoveryCode,
      newPassword,
    });
  }

  public async signOut(): Promise<void> {
    const session = this.#sessionCredential();
    try {
      if (session !== undefined) await this.#connector.client.authentication.logout(session);
    } catch {
      // Local logout still completes when the remote revocation endpoint is temporarily unavailable.
    } finally {
      this.#clearCookies();
    }
  }

  public static publicRecoveryErrorMessage(error: unknown): string {
    if (error instanceof IdentityAccessClientError) {
      switch (error.code) {
        case "unavailable":
        case "timeout":
        case "transport":
          return "Identity Access is temporarily unavailable. Please try again.";
        case "configuration":
          return "The administration host recovery configuration is incomplete.";
        default:
          return "Account recovery was not accepted. Check the recovery proof and try again.";
      }
    }
    if (error instanceof Error && error.message.startsWith("Invalid recovery input:")) return error.message;
    return "Account recovery could not be completed.";
  }

  public static publicLoginErrorMessage(error: unknown): string {
    if (error instanceof IdentityAccessClientError) {
      switch (error.code) {
        case "unavailable":
        case "timeout":
        case "transport":
          return "Identity Access is temporarily unavailable. Please try again.";
        case "configuration":
          return "The administration host authentication configuration is incomplete.";
        default:
          return "Sign-in was not accepted. Check your credentials and try again.";
      }
    }
    if (error instanceof Error && error.message.startsWith("Invalid sign-in input:")) return error.message;
    return "Sign-in could not be completed.";
  }

  #persist(session: IdentityLocalSession, tokens: IdentityOidcTokenSet): void {
    const secure = process.env.NODE_ENV === "production";
    const base = {
      httpOnly: true,
      sameSite: "lax" as const,
      secure,
      path: "/",
    };

    this.#cookieStore.set(this.#bearerCookieName, tokens.accessToken, {
      ...base,
      maxAge: tokens.expiresIn,
    });
    this.#cookieStore.set(this.#refreshCookieName, tokens.refreshToken, base);
    this.#cookieStore.set(this.#sessionIdCookieName, session.sessionId, {
      ...base,
      expires: new Date(session.expiresAt),
    });
    this.#cookieStore.set(this.#sessionTokenCookieName, session.sessionToken, {
      ...base,
      expires: new Date(session.expiresAt),
    });
  }

  #sessionCredential(): IdentitySessionCredential | undefined {
    const sessionId = this.#cookieStore.get(this.#sessionIdCookieName)?.value;
    const sessionToken = this.#cookieStore.get(this.#sessionTokenCookieName)?.value;
    if (!sessionId || !sessionToken) return undefined;
    return {
      kind: "session",
      clientId: this.#clientId,
      sessionId,
      sessionToken,
    };
  }

  async #bestEffortRevoke(session: IdentitySessionCredential): Promise<void> {
    try {
      await this.#connector.client.authentication.logout(session);
    } catch {
      // The original sign-in failure remains authoritative. Never replace it with cleanup failure detail.
    }
  }

  #clearCookies(): void {
    for (const name of [
      this.#bearerCookieName,
      this.#refreshCookieName,
      this.#sessionIdCookieName,
      this.#sessionTokenCookieName,
    ]) {
      this.#cookieStore.delete(name);
    }
  }

  private static requiredEnvironment(name: string): string {
    const value = process.env[name]?.trim();
    if (!value) throw new IdentityAccessClientError("configuration");
    return value;
  }

  private static cookieName(value: string): string {
    if (!/^[!#$%&'*+\-.^_`|~0-9A-Za-z]+$/.test(value)) {
      throw new IdentityAccessClientError("configuration");
    }
    return value;
  }

  private static requiredInput(value: string, label: string, maxLength: number): string {
    const normalized = value.trim();
    if (!normalized || normalized.length > maxLength) {
      throw new Error(`Invalid sign-in input: ${label} is required.`);
    }
    return normalized;
  }

  private static requiredSecret(value: string, label: string, maxLength: number): string {
    if (!value || value.length > maxLength) {
      throw new Error(`Invalid sign-in input: ${label} is required.`);
    }
    return value;
  }
}
