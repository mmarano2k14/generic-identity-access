import {
  AccountPage,
  GroupDetailsPage,
  GroupsPage,
  MfaPage,
  PoliciesPage,
  PolicyDetailsPage,
  ProfilePage,
  RecoveryPage,
  SecurityPage,
  SessionsPage,
  SignInPage,
  UserDetailsPage,
  UsersPage,
} from "@generic-identity/react";
import type {
  IdentityGroupRecord,
  IdentityManagedPolicyRecord,
  IdentitySessionValidationResult,
  IdentityUserRecord,
} from "@generic-identity/contracts";

const user: IdentityUserRecord = { userId: "user-1", displayName: "Example User", status: 1, version: 1 };
const group: IdentityGroupRecord = { tenantId: "tenant-1", groupId: "group-1", displayName: "Operators", status: 1, isTemplate: false, version: 1 };
const policy: IdentityManagedPolicyRecord = { policyId: "policy-1", policyKey: "operators", displayName: "Operators", status: 1, version: 1 };
const session: IdentitySessionValidationResult = { userId: user.userId, sessionId: "session-1", expiresAt: "2026-10-01T00:00:00Z", assurance: { level: "password", methods: ["password"], verifiedAt: "2026-09-30T00:00:00Z", acr: "password" } };

export function SharedPagesConsumerProbe() {
  return <>
    <SignInPage />
    <RecoveryPage />
    <ProfilePage user={user} />
    <AccountPage user={user} session={session} />
    <UsersPage users={[user]} />
    <UserDetailsPage user={user} />
    <GroupsPage groups={[group]} />
    <GroupDetailsPage group={group} />
    <PoliciesPage policies={[policy]} />
    <PolicyDetailsPage policy={policy} />
    <SessionsPage sessions={[session]} currentSessionId={session.sessionId} />
    <MfaPage />
    <SecurityPage><span>Security content</span></SecurityPage>
  </>;
}
