import "server-only";

import type { IdentityAdministrationContext } from "@generic-identity/auth";
import type {
  IdentityMfaPolicyRecord,
  IdentityMfaProviderRecord,
  IdentityMfaUserSecurityState,
  IdentityUserAuthenticatorRecord,
  IdentityUserRecord,
} from "@generic-identity/contracts";
import { isAllowedOnServer } from "./authorization";
import { NextIdentityServerSession } from "./session";

export interface NextMfaWorkspaceQuery {
  readonly userId?: string;
}

export interface NextMfaWorkspacePermissions {
  readonly canReadPolicy: boolean;
  readonly canWritePolicy: boolean;
  readonly canReadAuthenticators: boolean;
  readonly canWriteAuthenticators: boolean;
  readonly canReadUsers: boolean;
}

export interface NextMfaWorkspace {
  readonly providers: readonly IdentityMfaProviderRecord[];
  readonly policy: IdentityMfaPolicyRecord | null;
  readonly selectedUser: IdentityUserRecord | null;
  readonly securityState: IdentityMfaUserSecurityState | null;
  readonly authenticators: readonly IdentityUserAuthenticatorRecord[];
  readonly permissions: NextMfaWorkspacePermissions;
}

/**
 * Composes the provider-neutral MFA administration workspace.
 *
 * Provider secrets never enter this surface. The workspace exposes only
 * application policy, provider capabilities, safe generic authenticator
 * lifecycle metadata and effective user MFA readiness.
 */
export async function loadNextMfaWorkspace(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  query: NextMfaWorkspaceQuery = {},
): Promise<NextMfaWorkspace> {
  const boundary = {
    identityScopeId: context.identityScopeId,
    applicationKey: context.applicationKey,
  };
  const capability = (feature: string, action: string) =>
    isAllowedOnServer(session, boundary, {
      resource: "identity-access",
      feature,
      action,
    });

  const [
    canReadPolicy,
    canWritePolicy,
    canReadAuthenticators,
    canWriteAuthenticators,
    canReadUsers,
  ] = await Promise.all([
    capability("mfa-policy", "read"),
    capability("mfa-policy", "write"),
    capability("mfa-authenticator", "read"),
    capability("mfa-authenticator", "write"),
    capability("user", "read"),
  ]);

  const permissions: NextMfaWorkspacePermissions = {
    canReadPolicy,
    canWritePolicy,
    canReadAuthenticators,
    canWriteAuthenticators,
    canReadUsers,
  };

  const [providers, policy] = await Promise.all([
    canReadPolicy
      ? session.client.security.mfa.listProviders(context)
      : Promise.resolve([] as readonly IdentityMfaProviderRecord[]),
    canReadPolicy
      ? session.client.security.mfa.getPolicy(context)
      : Promise.resolve(null),
  ]);

  const requestedUserId = query.userId?.trim();
  const selectedUser = requestedUserId && canReadUsers
    ? await session.client.directory.users.get(context, requestedUserId)
    : null;

  const [securityState, authenticators] = selectedUser && canReadAuthenticators
    ? await Promise.all([
        session.client.security.mfa.getUserSecurityState(context, selectedUser.userId),
        session.client.security.mfa.listAuthenticators(context, selectedUser.userId),
      ])
    : [null, [] as readonly IdentityUserAuthenticatorRecord[]];

  return {
    providers,
    policy,
    selectedUser,
    securityState,
    authenticators,
    permissions,
  };
}
