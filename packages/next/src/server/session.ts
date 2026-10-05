import "server-only";

import { cookies } from "next/headers";
import {
  GenericIdentityClientError,
  createIdentityClient,
  signIn,
  signOut,
  validateSession,
  type GenericIdentityClient,
  type IdentityLocalSession,
  type IdentitySessionCredential,
} from "@generic-identity/auth";
import type { IdentitySessionValidationResult } from "@generic-identity/contracts";
import {
  normalizeNextIdentityServerOptions,
  type NextIdentityServerOptions,
  type NormalizedNextIdentityServerOptions,
} from "./config";

type CookieStore = Awaited<ReturnType<typeof cookies>>;

/**
 * Request-bound opaque-session coordinator for Next.js server code.
 *
 * The session id/token are stored only in HTTP-only cookies. Browser components
 * never receive the opaque session credential through this API.
 */
export class NextIdentityServerSession {
  readonly #client: GenericIdentityClient;
  readonly #cookieStore: CookieStore;
  readonly #options: NormalizedNextIdentityServerOptions;

  private constructor(
    client: GenericIdentityClient,
    cookieStore: CookieStore,
    options: NormalizedNextIdentityServerOptions,
  ) {
    this.#client = client;
    this.#cookieStore = cookieStore;
    this.#options = options;
  }

  public static async fromCurrentRequest(
    options: NextIdentityServerOptions,
  ): Promise<NextIdentityServerSession> {
    const normalized = normalizeNextIdentityServerOptions(options);
    const cookieStore = await cookies();
    return new NextIdentityServerSession(
      createIdentityClient(normalized.clientOptions),
      cookieStore,
      normalized,
    );
  }

  public get client(): GenericIdentityClient {
    return this.#client;
  }

  public credential(): IdentitySessionCredential | undefined {
    const sessionId = this.#cookieStore.get(this.#sessionIdCookieName)?.value;
    const sessionToken = this.#cookieStore.get(this.#sessionTokenCookieName)?.value;
    if (!sessionId || !sessionToken) return undefined;

    return {
      kind: "session",
      clientId: this.#options.clientId,
      sessionId,
      sessionToken,
    };
  }

  public requireCredential(): IdentitySessionCredential {
    const credential = this.credential();
    if (credential === undefined) {
      throw new GenericIdentityClientError("unauthenticated", 401);
    }
    return credential;
  }

  public async current(): Promise<IdentitySessionValidationResult | null> {
    const credential = this.credential();
    if (credential === undefined) return null;

    try {
      return await validateSession(this.#client, credential);
    } catch (error) {
      if (error instanceof GenericIdentityClientError && error.code === "unauthenticated") {
        this.clear();
        return null;
      }
      throw error;
    }
  }

  public async signIn(
    loginIdentifierValue: string,
    passwordValue: string,
    redirectUriValue: string,
  ): Promise<IdentitySessionValidationResult> {
    const loginIdentifier = requiredInput(loginIdentifierValue, "login identifier", 320);
    const password = requiredSecret(passwordValue, "password", 4096);
    const redirectUri = requiredInput(redirectUriValue, "redirect URI", 2048);

    const session = await signIn(this.#client, {
      clientId: this.#options.clientId,
      loginIdentifier,
      password,
      redirectUri,
    });

    this.persist(session);
    return {
      userId: session.userId,
      sessionId: session.sessionId,
      expiresAt: session.expiresAt,
      assurance: session.assurance,
    };
  }

  public async signOut(): Promise<void> {
    const credential = this.credential();
    try {
      if (credential !== undefined) {
        await signOut(this.#client, credential);
      }
    } finally {
      this.clear();
    }
  }

  public clear(): void {
    this.#cookieStore.delete(this.#sessionIdCookieName);
    this.#cookieStore.delete(this.#sessionTokenCookieName);
  }

  private persist(session: IdentityLocalSession): void {
    const expires = new Date(session.expiresAt);
    if (Number.isNaN(expires.getTime())) {
      throw new GenericIdentityClientError("protocol");
    }

    const options = {
      httpOnly: true,
      sameSite: "lax" as const,
      secure: this.#options.secureCookies,
      path: this.#options.cookiePath,
      expires,
    };

    this.#cookieStore.set(this.#sessionIdCookieName, session.sessionId, options);
    this.#cookieStore.set(this.#sessionTokenCookieName, session.sessionToken, options);
  }

  get #sessionIdCookieName(): string {
    return `${this.#options.cookiePrefix}_session_id`;
  }

  get #sessionTokenCookieName(): string {
    return `${this.#options.cookiePrefix}_session_token`;
  }
}

function requiredInput(value: string, label: string, maxLength: number): string {
  const normalized = value.trim();
  if (!normalized || normalized.length > maxLength) {
    throw new Error(`Invalid Generic Identity input: ${label} is required.`);
  }
  return normalized;
}

function requiredSecret(value: string, label: string, maxLength: number): string {
  if (!value || value.length > maxLength) {
    throw new Error(`Invalid Generic Identity input: ${label} is required.`);
  }
  return value;
}
