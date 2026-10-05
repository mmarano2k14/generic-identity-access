import type { GenericIdentitySecurityOperationsClient } from "@generic-identity/auth/security-operations";
import type { IdentityAdministrationContext } from "@identity-access/client";

export async function acceptSecurityOperations(
  security: GenericIdentitySecurityOperationsClient,
  context: IdentityAdministrationContext,
  userId: string,
): Promise<void> {
  await security.mfa.listProviders(context);
  await security.mfa.getPolicy(context);
  await security.mfa.listAuthenticators(context, userId);
  await security.mfa.getUserSecurityState(context, userId);
  await security.audit.list(context, { userId, limit: 10 });
  void security.sessions.revokeUser;
  void security.sessions.revokeClient;
  void security.mfa.createPolicy;
  void security.mfa.updatePolicy;
  void security.mfa.revokeAuthenticator;
  void security.mfa.revokeAuthenticatorForRecovery;
}
