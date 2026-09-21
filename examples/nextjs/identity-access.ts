// Copy into the server-side integration directory of the consuming Next.js app.
// The consuming application must install server-only and the locally built client.
import "server-only";
import { createIdentityAccessClient } from "@identity-access/client";

export function identityAccessDiagnostics() {
  // No NEXT_PUBLIC_ prefix: this is server deployment configuration.
  const baseUrl = process.env.IDENTITY_ACCESS_API_BASE_URL;
  if (!baseUrl) throw new Error("IDENTITY_ACCESS_API_BASE_URL is required.");
  return createIdentityAccessClient({ baseUrl });
}

// Example server use: await identityAccessDiagnostics().info();
// No session, token handling or authorization guard is implemented in this increment.
