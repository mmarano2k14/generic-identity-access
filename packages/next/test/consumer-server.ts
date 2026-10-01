import type {
  IdentityAuthorizationBoundary,
  IdentityCapabilityRequirement,
} from "@generic-identity/contracts";
import {
  NextIdentityServerSession,
  createNextAdministrationContext,
  createNextTenantAdministrationContext,
  requireAuthenticatedIdentitySession,
  requireServerCapability,
} from "@generic-identity/next/server";

export async function serverConsumerProbe(
  session: NextIdentityServerSession,
  boundary: IdentityAuthorizationBoundary,
  requirement: IdentityCapabilityRequirement,
): Promise<string> {
  const current = await requireAuthenticatedIdentitySession(session);
  const administration = createNextAdministrationContext(session, boundary.identityScopeId, boundary.applicationKey);
  void session.client.administration.context.get(administration);
  if (boundary.tenantId !== undefined) {
    const tenantAdministration = createNextTenantAdministrationContext(
      session,
      boundary.identityScopeId,
      boundary.applicationKey,
      boundary.tenantId,
    );
    void session.client.administration.groups.list(tenantAdministration);
  }
  await requireServerCapability(session, boundary, requirement);
  return current.userId;
}
