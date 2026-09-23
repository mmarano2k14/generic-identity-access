// Copy into the server-side integration directory of the consuming Next.js app.
// The consuming application must install server-only and the locally built client.
import "server-only";
import { IdentityAccessClient } from "@identity-access/client";

/** Server-only Next.js holder for the class-based Identity Access client. */
export class IdentityAccessServerConnector {
  readonly #client: IdentityAccessClient;

  public constructor() {
    // No NEXT_PUBLIC_ prefix: this is server deployment configuration.
    const baseUrl = process.env.IDENTITY_ACCESS_API_BASE_URL;
    if (!baseUrl) throw new Error("IDENTITY_ACCESS_API_BASE_URL is required.");
    this.#client = new IdentityAccessClient({ baseUrl });
  }

  public get client(): IdentityAccessClient {
    return this.#client;
  }
}

// Example server use:
// const identity = new IdentityAccessServerConnector();
// const info = await identity.client.info();
// const session = await identity.client.passwordLogin({
//   clientId: "admin-web",
//   loginIdentifier,
//   password,
//   redirectUri: "https://app.example.test/callback",
// });
// const authorization = await identity.client.authorizeOidc(session, {
//   clientId: "admin-web",
//   redirectUri: session.redirectUri,
// });
// const tokens = await identity.client.exchangeAuthorizationCode(authorization);

// const user = await identity.client.createUser({
//   identityScopeId,
//   applicationKey: "admin-app",
//   credential: { kind: "bearer", accessToken: tokens.accessToken },
// }, { displayName: "Alice" });
