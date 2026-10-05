import {
  GenericIdentityClientError,
  RequireCapability,
  createAuthorizationContext,
  createIdentityClient,
  isAllowed,
  signIn,
  signOut,
  validateSession,
  type GenericIdentityClientOptions,
  type IdentityAccessCredential,
  type IdentityPasswordLoginRequest,
  type IdentitySessionCredential,
} from "@generic-identity/auth";
import type {
  IdentityAuthorizationBoundary,
  IdentityCapabilityRequirement,
} from "@generic-identity/contracts";

const options: GenericIdentityClientOptions = {
  baseUrl: "https://identity.example.test/",
  fetch: async () => new Response(JSON.stringify({ allowed: false }), {
    status: 200,
    headers: { "content-type": "application/json" },
  }),
};

const client = createIdentityClient(options);
const boundary: IdentityAuthorizationBoundary = {
  identityScopeId: "42111111-1111-1111-1111-111111111111",
  applicationKey: "consumer-a",
};
const requirement: IdentityCapabilityRequirement = {
  resource: "billing",
  feature: "invoice",
  action: "read",
};
const bearer: IdentityAccessCredential = {
  kind: "bearer",
  accessToken: "header.payload.signature",
};
const session: IdentitySessionCredential = {
  kind: "session",
  clientId: "consumer-a",
  sessionId: "42dddddd-dddd-dddd-dddd-dddddddddddd",
  sessionToken: "opaque-session-token",
};
const login: IdentityPasswordLoginRequest = {
  clientId: "consumer-a",
  loginIdentifier: "user@example.test",
  password: "not-a-real-password",
  redirectUri: "https://consumer.example.test/callback",
};

void isAllowed(client, boundary, requirement, bearer);
void validateSession(client, session);
void signIn(client, login);
void signOut(client, session);
void createAuthorizationContext(client, { ...boundary, credential: bearer });
void GenericIdentityClientError;
void RequireCapability;
